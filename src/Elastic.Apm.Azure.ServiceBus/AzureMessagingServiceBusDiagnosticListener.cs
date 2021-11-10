// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Azure.ServiceBus
{
	/// <summary>
	/// Creates spans for diagnostic events from Azure.Messaging.ServiceBus
	/// </summary>
	internal class AzureMessagingServiceBusDiagnosticListener: DiagnosticListenerBase
	{
		private readonly ApmAgent _realAgent;
		private readonly ConcurrentDictionary<string, IExecutionSegment> _processingSegments = new ConcurrentDictionary<string, IExecutionSegment>();
		private readonly Framework _framework;

		public override string Name { get; } = "Azure.Messaging.ServiceBus";

		public AzureMessagingServiceBusDiagnosticListener(IApmAgent agent) : base(agent)
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
				case "ServiceBusSender.Send.Start":
					OnSendStart(kv, "SEND");
					break;
				case "ServiceBusSender.Schedule.Start":
					OnSendStart(kv, "SCHEDULE");
					break;
				case "ServiceBusReceiver.Receive.Start":
					OnReceiveStart(kv, "RECEIVE");
					break;
				case "ServiceBusReceiver.ReceiveDeferred.Start":
					OnReceiveStart(kv, "RECEIVEDEFERRED");
					break;
				case "ServiceBusProcessor.ProcessMessage.Start":
				case "ServiceBusSessionProcessor.ProcessSessionMessage.Start":
					OnProcessStart(kv, "PROCESS");
					break;
				case "ServiceBusSender.Send.Stop":
				case "ServiceBusSender.Schedule.Stop":
				case "ServiceBusReceiver.Receive.Stop":
				case "ServiceBusReceiver.ReceiveDeferred.Stop":
				case "ServiceBusProcessor.ProcessMessage.Stop":
				case "ServiceBusSessionProcessor.ProcessSessionMessage.Stop":
					OnStop();
					break;
				case "ServiceBusSender.Send.Exception":
				case "ServiceBusSender.Schedule.Exception":
				case "ServiceBusReceiver.Receive.Exception":
				case "ServiceBusReceiver.ReceiveDeferred.Exception":
				case "ServiceBusProcessor.ProcessMessage.Exception":
				case "ServiceBusSessionProcessor.ProcessSessionMessage.Exception":
					OnException(kv);
					break;
				default:
					Logger.Trace()?.Log("`{DiagnosticEventKey}' key is not a traced diagnostic event", kv.Key);
					break;
			}
		}

		private void OnProcessStart(KeyValuePair<string, object> kv, string action)
		{
			if (!(kv.Value is Activity activity))
			{
				Logger.Trace()?.Log("Value is not an activity - exiting");
				return;
			}

			string queueName = null;
			foreach (var tag in activity.Tags)
			{
				switch (tag.Key)
				{
					case "message_bus.destination":
						queueName = tag.Value;
						break;
					default:
						continue;
				}
			}

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

		private void OnReceiveStart(KeyValuePair<string, object> kv, string action)
		{
			if (!(kv.Value is Activity activity))
			{
				Logger.Trace()?.Log("Value is not an activity - exiting");
				return;
			}

			string queueName = null;
			foreach (var tag in activity.Tags)
			{
				switch (tag.Key)
				{
					case "message_bus.destination":
						queueName = tag.Value;
						break;
					default:
						continue;
				}
			}

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
				var span = ApmAgent.GetCurrentExecutionSegment().StartSpan(transactionName, ApiConstants.TypeMessaging, ServiceBus.SubType, action, isExitSpan: true);

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

		private void OnSendStart(KeyValuePair<string, object> kv, string action)
		{
			var currentSegment = ApmAgent.GetCurrentExecutionSegment();
			if (currentSegment is null)
			{
				Logger.Trace()?.Log("No current transaction or span - exiting");
				return;
			}

			if (!(kv.Value is Activity activity))
			{
				Logger.Trace()?.Log("Value is not an activity - exiting");
				return;
			}

			string queueName = null;
			string destinationAddress = null;
			foreach (var tag in activity.Tags)
			{
				switch (tag.Key)
				{
					case "message_bus.destination":
						queueName = tag.Value;
						break;
					case "peer.address":
						destinationAddress = tag.Value;
						break;
					default:
						continue;
				}
			}

			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var spanName = queueName is null
				? $"{ServiceBus.SegmentName} {action}"
				: $"{ServiceBus.SegmentName} {action} to {queueName}";

			var span = currentSegment.StartSpan(spanName, ApiConstants.TypeMessaging, ServiceBus.SubType, action.ToLowerInvariant());
			span.Context.Destination = new Destination
			{
				Address = destinationAddress,
				Service = new Destination.DestinationService
				{
					Resource = queueName is null ? ServiceBus.SubType : $"{ServiceBus.SubType}/{queueName}"
				}
			};

			if (queueName != null)
				span.Context.Message = new Message { Queue = new Queue { Name = queueName } };

			if (!_processingSegments.TryAdd(activity.Id, span))
			{
				Logger.Trace()?.Log(
					"Could not add {Action} span {SpanId} for activity {ActivityId} to tracked spans",
					action,
					span.Id,
					activity.Id);
			}
		}

		private void OnStop()
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

			segment.Outcome = Outcome.Success;
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

			if (kv.Value is Exception e)
				segment.CaptureException(e);

			segment.Outcome = Outcome.Failure;
			segment.End();
		}
	}
}
