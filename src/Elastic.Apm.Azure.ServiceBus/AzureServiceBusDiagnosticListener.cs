// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Azure.ServiceBus
{
	/// <summary>
	/// Creates spans for diagnostic events from Azure.Messaging.ServiceBus
	/// </summary>
	public class AzureServiceBusDiagnosticListener: IDiagnosticListener
	{
		private readonly IApmAgent _agent;
		private readonly ConcurrentDictionary<string, ISpan> _sendSpans = new ConcurrentDictionary<string, ISpan>();

		internal IApmLogger Logger { get; }

		public AzureServiceBusDiagnosticListener(IApmAgent agent)
		{
			_agent = agent;
			Logger = _agent.Logger.Scoped(nameof(AzureServiceBusDiagnosticListener));
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
				case "ServiceBusSender.Send.Start":
					OnSendStart(kv);
					break;
				case "ServiceBusSender.Send.Stop":
					OnSendStop();
					break;
				case "ServiceBusSender.Send.Exception":
					OnSendException(kv);
					break;
				case "ServiceBusSender.Schedule.Start":
					break;
				case "ServiceBusSender.Schedule.Stop":
					break;
				case "ServiceBusSender.Schedule.Exception":
					break;
				case "ServiceBusReceiver.Receive.Start":
					break;
				case "ServiceBusReceiver.Receive.Stop":
					break;
				case "ServiceBusReceiver.Receive.Exception":
					break;
				case "ServiceBusReceiver.ReceiveDeferred.Start":
					break;
				case "ServiceBusReceiver.ReceiveDeferred.Stop":
					break;
				case "ServiceBusReceiver.ReceiveDeferred.Exception":
					break;
				default:
					Logger.Trace()?.Log("Unrecognized key `{DiagnosticEventKey}'", kv.Key);
					break;
			}
		}

		private void OnSendStart(KeyValuePair<string, object> kv)
		{
			var currentSegment = _agent.GetCurrentExecutionSegment();
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

			var spanName = queueName is null
				? "AzureServiceBus SEND"
				: $"AzureServiceBus SEND to {queueName}";

			var span = currentSegment.StartSpan(spanName, "messaging", "azureservicebus", "send");

			span.Context.Destination = new Destination
			{
				Address = destinationAddress,
				Service = new Destination.DestinationService
				{
					Name = "azureservicebus",
					Resource = queueName is null ? "azureservicebus" : $"azureservicebus/{queueName}",
					Type = "messaging"
				}
			};

			if (!_sendSpans.TryAdd(activity.Id, span))
			{
				Logger.Error()?
					.Log("Could not add send span {SpanId} for activity {ActivityId} to tracked spans", span.Id, activity.Id);
			}
		}

		private void OnSendStop()
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_sendSpans.TryRemove(activity.Id, out var span))
			{
				Logger.Error()?
					.Log("Could not get span for activity {ActivityId} from tracked spans", activity.Id);
				return;
			}

			span.Outcome = Outcome.Success;
			span.End();
		}

		private void OnSendException(KeyValuePair<string,object> kv)
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_sendSpans.TryRemove(activity.Id, out var span))
			{
				Logger.Error()?
					.Log("Could not get span for activity {ActivityId} from tracked spans", activity.Id);
				return;
			}

			if (kv.Value is Exception e)
				span.CaptureException(e);

			span.Outcome = Outcome.Failure;
			span.End();
		}

		public string Name { get; } = "Azure.Messaging.ServiceBus";
	}
}
