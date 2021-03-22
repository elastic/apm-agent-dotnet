// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Azure.Storage
{
	/// <summary>
	/// Creates transactions and spans for diagnostic events from Azure.Storage.Queues
	/// </summary>
	public class AzureQueueStorageDiagnosticListener : DiagnosticListenerBase
	{
		private readonly ApmAgent _realAgent;
		private readonly Service _service;
		private readonly ConcurrentDictionary<string, IExecutionSegment> _processingSegments =
			new ConcurrentDictionary<string, IExecutionSegment>();

		public AzureQueueStorageDiagnosticListener(IApmAgent agent) : base(agent)
		{
			_realAgent = agent as ApmAgent;
			_service = Service.GetDefaultService(agent.ConfigurationReader, agent.Logger);
			_service.Framework = new Framework { Name = "AzureQueue" };
		}

		public override string Name { get; } = "Azure.Storage.Queues";

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
				case "QueueClient.ReceiveMessage.Start":
				case "QueueClient.ReceiveMessages.Start":
					OnReceiveStart(kv);
					break;
				case "QueueClient.SendMessage.Start":
					OnSendStart(kv);
					break;
				case "QueueClient.ReceiveMessage.Stop":
				case "QueueClient.ReceiveMessages.Stop":
				case "QueueClient.SendMessage.Stop":
					OnStop();
					break;
				case "QueueClient.ReceiveMessage.Exception":
				case "QueueClient.ReceiveMessages.Exception":
				case "QueueClient.SendMessage.Exception":
					OnException(kv);
					break;

				default:
					Logger.Trace()?.Log("`{DiagnosticEventKey}' key is not a traced diagnostic event", kv.Key);
					break;
			}
		}

		private void OnSendStart(KeyValuePair<string, object> kv)
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

			var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
			if (!string.IsNullOrEmpty(urlTag))
			{
				var queueUrl = new QueueUrl(urlTag);
				queueName = queueUrl.QueueName;
				destinationAddress = queueUrl.FullyQualifiedNamespace;
			}

			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var spanName = queueName is null
				? $"AzureQueue SEND"
				: $"AzureQueue SEND to {queueName}";

			var span = currentSegment.StartSpan(spanName, "messaging", "azurequeue", "send");
			span.Context.Destination = new Destination
			{
				Address = destinationAddress,
				Service = new Destination.DestinationService
				{
					Name = "azurequeue",
					Resource = queueName is null ? "azurequeue" : $"azurequeue/{queueName}",
					Type = "messaging"
				}
			};

			if (!_processingSegments.TryAdd(activity.Id, span))
			{
				Logger.Trace()?.Log(
					"Could not add {Action} span {SpanId} for activity {ActivityId} to tracked spans",
					"SEND",
					span.Id,
					activity.Id);
			}
		}

		private void OnReceiveStart(KeyValuePair<string, object> kv)
		{
			if (!(kv.Value is Activity activity))
			{
				Logger.Trace()?.Log("Value is not an activity - exiting");
				return;
			}

			var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
			var queueName = !string.IsNullOrEmpty(urlTag)
				? new QueueUrl(urlTag).QueueName
				: null;

			if (MatchesIgnoreMessageQueues(queueName))
				return;

			var transactionName = queueName is null
				? $"AzureQueue RECEIVE"
				: $"AzureQueue RECEIVE from {queueName}";

			var transaction = ApmAgent.Tracer.StartTransaction(transactionName, "messaging");
			transaction.Context.Service = _service;

			// transaction creation will create an activity, so use this as the key.
			// TODO: change when existing activity is used.
			var activityId = Activity.Current.Id;

			if (!_processingSegments.TryAdd(activityId, transaction))
			{
				Logger.Error()?.Log(
					"Could not add {Action} transaction {TransactionId} for activity {ActivityId} to tracked segments",
					"RECEIVE",
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

		/// <summary>
		/// Working with a queue url to extract the queue name and address.
		/// </summary>
		private class QueueUrl
		{
			private readonly UriBuilder _builder;

			public QueueUrl(string url) => _builder = new UriBuilder(url);

			public string QueueName => _builder.Uri.Segments.Length > 2
				? _builder.Uri.Segments[1].TrimEnd('/')
				: null;

			public string FullyQualifiedNamespace => _builder.Uri.GetLeftPart(UriPartial.Authority) + "/";
		}
	}


}
