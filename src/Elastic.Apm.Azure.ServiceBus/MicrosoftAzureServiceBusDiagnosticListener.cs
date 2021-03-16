// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Reflection;

namespace Elastic.Apm.Azure.Messaging.ServiceBus
{
	/// <summary>
	/// Creates spans for diagnostic events from Microsoft.Azure.ServiceBus
	/// </summary>
	public class MicrosoftAzureServiceBusDiagnosticListener: IDiagnosticListener
	{
		private readonly IApmAgent _agent;
		private readonly ApmAgent _realAgent;
		private readonly ConcurrentDictionary<string, IExecutionSegment> _processingSegments =
			new ConcurrentDictionary<string, IExecutionSegment>();

		private readonly ConcurrentDictionary<string, Func<object, object>> _sendProperties =
			new ConcurrentDictionary<string, Func<object, object>>();

		private readonly ConcurrentDictionary<string, Func<object, object>> _scheduleProperties =
			new ConcurrentDictionary<string, Func<object, object>>();

		private readonly ConcurrentDictionary<string, Func<object, object>> _receiveProperties =
			new ConcurrentDictionary<string, Func<object, object>>();

		private readonly ConcurrentDictionary<string, Func<object, object>> _receiveDeferredProperties =
			new ConcurrentDictionary<string, Func<object, object>>();

		private readonly ConcurrentDictionary<string, Func<object, object>> _exceptionProperties =
			new ConcurrentDictionary<string, Func<object, object>>();

		internal IApmLogger Logger { get; }

		public string Name { get; } = "Microsoft.Azure.ServiceBus";

		public MicrosoftAzureServiceBusDiagnosticListener(IApmAgent agent)
		{
			_agent = agent;
			_realAgent = agent as ApmAgent;
			Logger = _agent.Logger.Scoped(nameof(AzureMessagingServiceBusDiagnosticListener));
		}

		public void OnCompleted() => Logger.Trace()?.Log("Completed");

		public void OnError(Exception error) => Logger.Error()?.LogExceptionWithCaller(error, nameof(OnError));

		public void OnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);

			if (string.IsNullOrEmpty(kv.Key))
			{
				Logger.Trace()?.Log($"Key is {(kv.Key == null ? "null" : "an empty string")} - exiting");
				return;
			}

			switch (kv.Key)
			{
				case "Microsoft.Azure.ServiceBus.Send.Start":
					OnSendStart(kv, "SEND", _sendProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Send.Stop":
					OnStop(kv, _sendProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Schedule.Start":
					OnSendStart(kv, "SCHEDULE", _scheduleProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Schedule.Stop":
					OnStop(kv, _scheduleProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Receive.Start":
					OnReceiveStart(kv, "RECEIVE", _receiveProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Receive.Stop":
					OnStop(kv, _receiveProperties);
					break;
				case "Microsoft.Azure.ServiceBus.ReceiveDeferred.Start":
					OnReceiveStart(kv, "RECEIVEDEFERRED", _receiveDeferredProperties);
					break;
				case "Microsoft.Azure.ServiceBus.ReceiveDeferred.Stop":
					OnStop(kv, _receiveDeferredProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Exception":
					OnException(kv, _exceptionProperties);
					break;
				default:
					Logger.Trace()?.Log("`{DiagnosticEventKey}' key is not a traced diagnostic event", kv.Key);
					break;
			}
		}

		private void OnReceiveStart(
			KeyValuePair<string, object> kv,
			string action,
			ConcurrentDictionary<string, Func<object, object>> cachedProperties)
		{
			if (kv.Value is null)
			{
				Logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			var activity = Activity.Current;
			var entityGetter = cachedProperties.GetOrAdd(
				"Entity",
				k => ExpressionBuilder.BuildPropertyGetter(kv.Value.GetType(), k));
			var endpointGetter = cachedProperties.GetOrAdd(
				"Endpoint",
				k => ExpressionBuilder.BuildPropertyGetter(kv.Value.GetType(), k));

			var queueName = entityGetter(kv.Value) as string;
			var destinationAddress = endpointGetter(kv.Value) as Uri;

			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var transactionName = queueName is null
				? $"AzureServiceBus {action}"
				: $"AzureServiceBus {action} from {queueName}";

			DistributedTracingData tracingData = null;

			var transaction = _agent.Tracer.StartTransaction(transactionName, "messaging", tracingData);

			// transaction creation will create an activity, so use this as the key.
			// TODO: change when existing activity is used.
			var activityId = Activity.Current.Id;

			transaction.Context.Service = Service.GetDefaultService(_agent.ConfigurationReader, _agent.Logger);
			transaction.Context.Service.Framework = new Framework { Name = "AzureServiceBus" };

			if (!_processingSegments.TryAdd(activityId, transaction))
			{
				Logger.Error()?.Log(
					"Could not add {Action} transaction {TransactionId} for activity {ActivityId} to tracked segments",
					action,
					transaction.Id,
					activity.Id);
			}
		}

		private bool MatchesIgnoreMessageQueues(string name)
		{
			if (name != null && _realAgent != null)
			{
				var matcher = WildcardMatcher.AnyMatch(_realAgent.ConfigStore.CurrentSnapshot.IgnoreMessageQueues, name);
				if (matcher != null)
				{
					Logger.Debug()?.Log(
						"Not tracing message from {QueueName} because it matched IgnoreMessageQueues pattern {Matcher}",
						name,
						matcher.GetMatcher());
					return true;
				}
			}

			return false;
		}

		private void OnSendStart(
			KeyValuePair<string, object> kv,
			string action,
			ConcurrentDictionary<string, Func<object, object>> cachedProperties
		)
		{
			var currentSegment = _agent.GetCurrentExecutionSegment();
			if (currentSegment is null)
			{
				Logger.Trace()?.Log("No current transaction or span - exiting");
				return;
			}

			if (kv.Value is null)
			{
				Logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			var activity = Activity.Current;

			var entityGetter = cachedProperties.GetOrAdd(
				"Entity",
				k => ExpressionBuilder.BuildPropertyGetter(kv.Value.GetType(), k));
			var endpointGetter = cachedProperties.GetOrAdd(
				"Endpoint",
				k => ExpressionBuilder.BuildPropertyGetter(kv.Value.GetType(), k));

			var queueName = entityGetter(kv.Value) as string;
			var destinationAddress = endpointGetter(kv.Value) as Uri;

			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var spanName = queueName is null
				? $"AzureServiceBus {action}"
				: $"AzureServiceBus {action} to {queueName}";

			var span = currentSegment.StartSpan(spanName, "messaging", "azureservicebus", action.ToLowerInvariant());

			span.Context.Destination = new Destination
			{
				Address = destinationAddress?.AbsoluteUri,
				Service = new Destination.DestinationService
				{
					Name = "azureservicebus",
					Resource = queueName is null ? "azureservicebus" : $"azureservicebus/{queueName}",
					Type = "messaging"
				}
			};

			if (!_processingSegments.TryAdd(activity.Id, span))
			{
				Logger.Error()?.Log(
					"Could not add {Action} span {SpanId} for activity {ActivityId} to tracked spans",
					action,
					span.Id,
					activity.Id);
			}
		}

		private void OnStop(
			KeyValuePair<string, object> kv,
			ConcurrentDictionary<string, Func<object, object>> cachedProperties)
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_processingSegments.TryRemove(activity.Id, out var segment))
				return;

			var statusGetter = cachedProperties.GetOrAdd(
				"Status",
				k => ExpressionBuilder.BuildPropertyGetter(kv.Value.GetType(), k));

			var status = statusGetter(kv.Value) as TaskStatus?;
			var outcome = status switch
			{
				TaskStatus.RanToCompletion => Outcome.Success,
				TaskStatus.Canceled => Outcome.Failure,
				TaskStatus.Faulted => Outcome.Failure,
				_ => Outcome.Unknown
			};

			segment.Outcome = outcome;
			segment.End();
		}

		private void OnException(
			KeyValuePair<string, object> kv,
			ConcurrentDictionary<string, Func<object, object>> cachedProperties)
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_processingSegments.TryRemove(activity.Id, out var segment))
				return;

			var exceptionGetter = cachedProperties.GetOrAdd(
				"Exception",
				k => ExpressionBuilder.BuildPropertyGetter(kv.Value.GetType(), k));

			if (exceptionGetter(kv.Value) is Exception exception)
				segment.CaptureException(exception);

			segment.Outcome = Outcome.Failure;
			segment.End();
		}
	}
}
