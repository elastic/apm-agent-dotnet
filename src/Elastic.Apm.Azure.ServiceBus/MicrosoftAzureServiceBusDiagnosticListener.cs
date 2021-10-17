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
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Reflection;

namespace Elastic.Apm.Azure.ServiceBus
{
	/// <summary>
	/// Creates spans for diagnostic events from Microsoft.Azure.ServiceBus
	/// </summary>
	internal class MicrosoftAzureServiceBusDiagnosticListener: DiagnosticListenerBase
	{
		private readonly ApmAgent _realAgent;
		private readonly ConcurrentDictionary<string, IExecutionSegment> _processingSegments = new ConcurrentDictionary<string, IExecutionSegment>();
		private readonly PropertyFetcherCollection _sendProperties = new PropertyFetcherCollection { "Entity", "Endpoint", "Status" };
		private readonly PropertyFetcherCollection _scheduleProperties = new PropertyFetcherCollection { "Entity", "Endpoint", "Status" };
		private readonly PropertyFetcherCollection _receiveProperties = new PropertyFetcherCollection { "Entity", "Endpoint", "Status" };
		private readonly PropertyFetcherCollection _receiveDeferredProperties = new PropertyFetcherCollection { "Entity", "Endpoint", "Status" };
		private readonly PropertyFetcherCollection _processProperties = new PropertyFetcherCollection { "Entity", "Endpoint", "Status" };
		private readonly PropertyFetcherCollection _processSessionProperties = new PropertyFetcherCollection { "Entity", "Endpoint", "Status" };
		private readonly PropertyFetcher _exceptionProperty = new PropertyFetcher("Exception");
		private readonly Framework _framework;

		public override string Name { get; } = "Microsoft.Azure.ServiceBus";

		public MicrosoftAzureServiceBusDiagnosticListener(IApmAgent agent) : base(agent)
		{
			_realAgent = agent as ApmAgent;
			_framework = new Framework { Name = ServiceBus.SegmentName };
		}

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
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
				case "Microsoft.Azure.ServiceBus.Process.Start":
					OnProcessStart(kv, "PROCESS", _processProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Process.Stop":
					OnStop(kv, _processProperties);
					break;
				case "Microsoft.Azure.ServiceBus.ProcessSession.Start":
					OnProcessStart(kv, "PROCESS", _processSessionProperties);
					break;
				case "Microsoft.Azure.ServiceBus.ProcessSession.Stop":
					OnStop(kv, _processSessionProperties);
					break;
				case "Microsoft.Azure.ServiceBus.Exception":
					OnException(kv);
					break;
				default:
					Logger.Trace()?.Log("`{DiagnosticEventKey}' key is not a traced diagnostic event", kv.Key);
					break;
			}
		}

		private void OnProcessStart(KeyValuePair<string, object> kv, string action, PropertyFetcherCollection cachedProperties)
		{
			if (kv.Value is null)
			{
				Logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			var queueName = cachedProperties.Fetch(kv.Value,"Entity") as string;
			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var transactionName = queueName is null
				? $"{ServiceBus.SegmentName} {action}"
				: $"{ServiceBus.SegmentName} {action} from {queueName}";

			var transaction = ApmAgent.Tracer.StartTransaction(transactionName, ApiConstants.TypeMessaging);
			transaction.Context.Service = new Service(null, null) { Framework = _framework };

			if (queueName != null)
				transaction.Context.Message = new Message { Queue = new Queue { Name = queueName } };

			// transaction creation will create an activity, so use this as the key.
			var activityId = Activity.Current.Id;

			if (!_processingSegments.TryAdd(activityId, transaction))
			{
				Logger.Trace()?.Log(
					"Could not add {Action} transaction {TransactionId} for activity {ActivityId} to tracked segments",
					action,
					transaction.Id,
					activityId);
			}
		}

		private void OnReceiveStart(KeyValuePair<string, object> kv, string action, PropertyFetcherCollection cachedProperties)
		{
			if (kv.Value is null)
			{
				Logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			var queueName = cachedProperties.Fetch(kv.Value,"Entity") as string;
			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var transactionName = queueName is null
				? $"{ServiceBus.SegmentName} {action}"
				: $"{ServiceBus.SegmentName} {action} from {queueName}";

			IExecutionSegment segment;
			if (ApmAgent.Tracer.CurrentTransaction is null)
			{
				var transaction = ApmAgent.Tracer.StartTransaction(transactionName, ApiConstants.TypeMessaging);
				transaction.Context.Service = new Service(null, null) { Framework = _framework };
				if (queueName != null)
					transaction.Context.Message = new Message { Queue = new Queue { Name = queueName } };
				segment = transaction;
			}
			else
			{
				var span = ApmAgent.GetCurrentExecutionSegment().StartSpan(transactionName, ApiConstants.TypeMessaging, ServiceBus.SubType, action);
				if (queueName != null)
					span.Context.Message = new Message { Queue = new Queue { Name = queueName } };
				segment = span;
			}

			// transaction creation will create an activity, so use this as the key.
			var activityId = Activity.Current.Id;

			if (!_processingSegments.TryAdd(activityId, segment))
			{
				Logger.Trace()?.Log(
					"Could not add {Action} {SegmentName} {TransactionId} for activity {ActivityId} to tracked segments",
					action,
					segment is ITransaction ? "transaction" : "span",
					segment.Id,
					activityId);
			}
		}

		private bool MatchesIgnoreMessageQueues(string name)
		{
			if (name != null && _realAgent != null)
			{
				var matcher = WildcardMatcher.AnyMatch(_realAgent.ConfigurationStore.CurrentSnapshot.IgnoreMessageQueues, name);
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

		private void OnSendStart(KeyValuePair<string, object> kv, string action, PropertyFetcherCollection cachedProperties)
		{
			var currentSegment = ApmAgent.GetCurrentExecutionSegment();
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
			var queueName = cachedProperties.Fetch(kv.Value,"Entity") as string;
			var destinationAddress = cachedProperties.Fetch(kv.Value, "Endpoint") as Uri;

			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var spanName = queueName is null
				? $"{ServiceBus.SegmentName} {action}"
				: $"{ServiceBus.SegmentName} {action} to {queueName}";

			var span = currentSegment.StartSpan(spanName, ApiConstants.TypeMessaging, ServiceBus.SubType, action.ToLowerInvariant());

			span.Context.Destination = new Destination
			{
				Address = destinationAddress?.AbsoluteUri,
				Service = new Destination.DestinationService
				{
					Name = ServiceBus.SubType,
					Resource = queueName is null ? ServiceBus.SubType : $"{ServiceBus.SubType}/{queueName}",
					Type = ApiConstants.TypeMessaging
				}
			};

			if (queueName != null)
				span.Context.Message = new Message { Queue = new Queue { Name = queueName } };

			if (!_processingSegments.TryAdd(activity.Id, span))
			{
				Logger.Trace()?.Log(
					"Could not add {Action} span {SpanId} for activity {ActivityId} to tracked segments",
					action,
					span.Id,
					activity.Id);
			}
		}

		private void OnStop(KeyValuePair<string, object> kv, PropertyFetcherCollection cachedProperties)
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_processingSegments.TryRemove(activity.Id, out var segment))
			{
				Logger.Trace()?.Log(
					"Could not find segment for activity {ActivityId} in tracked segments",
					activity.Id);
				return;
			}

			var status = cachedProperties.Fetch(kv.Value, "Status") as TaskStatus?;
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

		private void OnException(KeyValuePair<string, object> kv)
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_processingSegments.TryRemove(activity.Id, out var segment))
			{
				Logger.Trace()?.Log(
					"Could not find segment for activity {ActivityId} in tracked segments",
					activity.Id);
				return;
			}

			if (_exceptionProperty.Fetch(kv.Value) is Exception exception)
				segment.CaptureException(exception);

			segment.Outcome = Outcome.Failure;
			segment.End();
		}
	}
}
