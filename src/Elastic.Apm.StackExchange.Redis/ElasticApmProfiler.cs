// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Reflection;
using StackExchange.Redis.Profiling;

namespace Elastic.Apm.StackExchange.Redis
{
	/// <summary>
	/// Captures redis commands sent with StackExchange.Redis client
	/// </summary>
	public class ElasticApmProfiler
	{
		private readonly ConcurrentDictionary<string, ProfilingSession> _executionSegmentSessions =
			new ConcurrentDictionary<string, ProfilingSession>();

		private readonly Lazy<IApmLogger> _logger;
		private readonly Lazy<IApmAgent> _agent;
		private static readonly Func<object,object> MessageFetcher;
		private static readonly Func<object,object> CommandAndKeyFetcher;
		private static readonly Type _profiledCommandType;

		static ElasticApmProfiler()
		{
			var messageType = Type.GetType("StackExchange.Redis.Message,StackExchange.Redis", false);
			_profiledCommandType = Type.GetType("StackExchange.Redis.Profiling.ProfiledCommand,StackExchange.Redis", false);
			if (messageType != null && _profiledCommandType != null)
			{
				var commandAndKey = messageType.GetProperty("CommandAndKey", BindingFlags.Public | BindingFlags.Instance);
				var messageProperty = _profiledCommandType.GetField("Message", BindingFlags.NonPublic | BindingFlags.Instance);
				if (commandAndKey != null && messageProperty != null)
				{
					MessageFetcher = ExpressionBuilder.BuildFieldGetter(_profiledCommandType, messageProperty);
					CommandAndKeyFetcher = ExpressionBuilder.BuildPropertyGetter(messageType, commandAndKey);
				}
			}


		}

		public ElasticApmProfiler(Func<IApmAgent> agentGetter)
		{
			_agent = new Lazy<IApmAgent>(agentGetter);
			_logger = new Lazy<IApmLogger>(() => _agent.Value.Logger.Scoped(nameof(ElasticApmProfiler)));
		}

		/// <summary>
		/// Gets a profiling session for StackExchange.Redis to add redis commands to.
		/// Creates a profiling session per span or transaction
		/// </summary>
		/// <remarks>
		/// See https://stackexchange.github.io/StackExchange.Redis/Profiling_v2.html
		/// </remarks>
		/// <returns>A profiling session for the current span or transaction, or null if the agent is not enabled or not recording</returns>
		public ProfilingSession GetProfilingSession()
		{
			if (!Agent.Config.Enabled || !Agent.Config.Recording)
				return null;

			var executionSegment = _agent.Value.GetCurrentExecutionSegment();
			var realSpan = executionSegment as Span;
			Transaction realTransaction = null;

			// don't profile when there's no real span or transaction
			if (realSpan is null)
			{
				realTransaction = executionSegment as Transaction;
				if (realTransaction is null)
					return null;
			}

			var isSpan = realSpan != null;
			if (!_executionSegmentSessions.TryGetValue(executionSegment.Id, out var session))
			{
				_logger.Value.Trace()?.Log("Creating profiling session for {ExecutionSegment} {Id}",
					isSpan ? "span" : "transaction",
					executionSegment.Id);

				session = new ProfilingSession();

				if (!_executionSegmentSessions.TryAdd(executionSegment.Id, session))
				{
					_logger.Value.Debug()?.Log("could not add profiling session to tracked sessions for {ExecutionSegment} {Id}",
						isSpan ? "span" : "transaction",
						executionSegment.Id);
				}

				if (isSpan)
					realSpan.Ended += (sender, _) => EndProfilingSession(sender, session);
				else
					realTransaction.Ended += (sender, _) => EndProfilingSession(sender, session);
			}

			return session;
		}

		private void EndProfilingSession(object sender, ProfilingSession session)
		{
			IExecutionSegment executionSegment = sender as Span;
			string segmentType;
			if (executionSegment is null)
			{
				executionSegment = sender as Transaction;
				if (executionSegment is null)
					return;

				segmentType = "transaction";
			}
			else
				segmentType = "span";

			try
			{
				// Remove the session. Use session passed to EndProfilingSession rather than the removed session in the event
				// there was an issue in adding or removing the session
				if (!_executionSegmentSessions.TryRemove(executionSegment.Id, out _))
				{
					_logger.Value.Debug()?.Log(
						"could not remove profiling session from tracked sessions for {ExecutionSegment} {Id}",
						segmentType, executionSegment.Id);
				}

				var profiledCommands = session.FinishProfiling();
				_logger.Value.Trace()?.Log(
					"Finished profiling session for {ExecutionSegment}. Collected {ProfiledCommandCount} commands",
					executionSegment, profiledCommands.Count());

				foreach (var profiledCommand in profiledCommands) ProcessCommand(profiledCommand, executionSegment);

				_logger.Value.Trace()?.Log(
					"End profiling session for {ExecutionSegment} {Id}",
					segmentType, executionSegment.Id);
			}
			catch (Exception e)
			{
				_logger.Value.Error()?.LogException(e,
					"Exception ending profiling session for {ExecutionSegment} {Id}",
					segmentType, executionSegment.Id);
			}
		}
		private static void ProcessCommand(IProfiledCommand profiledCommand, IExecutionSegment executionSegment)
		{
			var name = GetCommand(profiledCommand);
			if (profiledCommand.RetransmissionOf != null)
			{
				var retransmissionName = GetCommand(profiledCommand.RetransmissionOf);
				name += $" (Retransmission of {retransmissionName}: {profiledCommand.RetransmissionReason})";
			}

			executionSegment.CaptureSpan(name, ApiConstants.TypeDb, span =>
			{
				span.Context.Db = new Database
				{
					Instance = profiledCommand.Db.ToString(CultureInfo.InvariantCulture),
					Statement = GetCommandAndKey(profiledCommand) ?? name,
					Type = ApiConstants.SubTypeRedis
				};

				string address = null;
				int? port = null;
				switch (profiledCommand.EndPoint)
				{
					case IPEndPoint ipEndPoint:
						address = ipEndPoint.Address.ToString();
						port = ipEndPoint.Port;
						break;
					case DnsEndPoint dnsEndPoint:
						address = dnsEndPoint.Host;
						port = dnsEndPoint.Port;
						break;
				}

				if (address != null)
					span.Context.Destination = new Destination { Address = address, Port = port };

				// update the timestamp to reflect the point at which the command was created
				if (span is Span realSpan)
					realSpan.Timestamp = TimeUtils.ToTimestamp(profiledCommand.CommandCreated);

				span.Duration = profiledCommand.ElapsedTime.TotalMilliseconds;

				// profiled commands are always successful
				span.Outcome = Outcome.Success;

				// TODO: clear the raw stacktrace as it won't be representative of the call stack at
				// the point at which the call to redis happens, and therefore misleading to include
			}, ApiConstants.SubTypeRedis, "query");
		}

		private static string GetCommand(IProfiledCommand profiledCommand) =>
			!string.IsNullOrEmpty(profiledCommand.Command)
				? profiledCommand.Command
				: "UNKNOWN";

		private static string GetCommandAndKey(IProfiledCommand profiledCommand)
		{
			if (profiledCommand.GetType() != _profiledCommandType || MessageFetcher == null)
				return null;

			var message = MessageFetcher.Invoke(profiledCommand);
			return CommandAndKeyFetcher.Invoke(message) as string;
		}
	}
}
