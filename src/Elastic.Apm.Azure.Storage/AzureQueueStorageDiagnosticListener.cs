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
	internal static class AzureQueueStorage
	{
		internal const string SpanName = "AzureQueue";
		internal const string SubType = "azurequeue";
	}

	/// <summary>
	/// Creates transactions and spans for Azure Queue Storage diagnostic events from Azure.Storage.Queues
	/// </summary>
	internal class AzureQueueStorageDiagnosticListener : DiagnosticListenerBase
	{
		private readonly Framework _framework;

		private readonly ConcurrentDictionary<string, IExecutionSegment> _processingSegments =
			new ConcurrentDictionary<string, IExecutionSegment>();

		private readonly ApmAgent _realAgent;

		public AzureQueueStorageDiagnosticListener(IApmAgent agent) : base(agent)
		{
			_realAgent = agent as ApmAgent;
			_framework = new Framework { Name = AzureQueueStorage.SpanName };
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
				? $"{AzureQueueStorage.SpanName} SEND"
				: $"{AzureQueueStorage.SpanName} SEND to {queueName}";

			var span = currentSegment.StartSpan(spanName, ApiConstants.TypeMessaging, AzureQueueStorage.SubType, "send");
			span.Context.Destination = new Destination
			{
				Address = destinationAddress,
				Service = new Destination.DestinationService
				{
					Name = AzureQueueStorage.SubType,
					Resource = queueName is null ? AzureQueueStorage.SubType : $"{AzureQueueStorage.SubType}/{queueName}",
					Type = ApiConstants.TypeMessaging
				}
			};

			if (!_processingSegments.TryAdd(activity.Id, span))
			{
				Logger.Trace()
					?.Log(
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
				? $"{AzureQueueStorage.SpanName} RECEIVE"
				: $"{AzureQueueStorage.SpanName} RECEIVE from {queueName}";

			var transaction = ApmAgent.Tracer.StartTransaction(transactionName, ApiConstants.TypeMessaging);
			transaction.Context.Service = new Service(null, null) { Framework = _framework };

			// transaction creation will create an activity, so use this as the key.
			var activityId = Activity.Current.Id;

			if (!_processingSegments.TryAdd(activityId, transaction))
			{
				Logger.Error()
					?.Log(
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
					Logger.Debug()
						?.Log(
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
				Logger.Trace()
					?.Log(
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
				Logger.Trace()
					?.Log(
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
			public QueueUrl(string url)
			{
				var builder = new UriBuilder(url);

				FullyQualifiedNamespace = builder.Uri.GetLeftPart(UriPartial.Authority) + "/";

				QueueName = builder.Uri.Segments.Length > 1
					? builder.Uri.Segments[1].TrimEnd('/')
					: null;
			}

			public string FullyQualifiedNamespace { get; }

			public string QueueName { get; }
		}
	}
}
