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
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Azure.Storage
{
	internal static class AzureFileStorage
	{
		internal const string SpanName = "AzureFile";
		internal const string SubType = "azurefile";
	}

	/// <summary>
	/// Creates transactions and spans for Azure File Share Storage diagnostic events from Azure.Storage.Files.Shares
	/// </summary>
	internal class AzureFileShareStorageDiagnosticListener : DiagnosticListenerBase
	{
		private readonly ConcurrentDictionary<string, ISpan> _processingSegments =
			new ConcurrentDictionary<string, ISpan>();

		public AzureFileShareStorageDiagnosticListener(IApmAgent agent) : base(agent) { }

		public override string Name { get; } = "Azure.Storage.Files.Shares";

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
				case "ShareClient.Create.Start":
				case "ShareDirectoryClient.Create.Start":
				case "ShareFileClient.Create.Start":
					OnStart(kv, "Create");
					break;
				case "ShareClient.Delete.Start":
				case "ShareDirectoryClient.Delete.Start":
				case "ShareFileClient.Delete.Start":
					OnStart(kv, "Delete");
					break;
				case "ShareFileClient.UploadRange.Start":
				case "ShareFileClient.Upload.Start":
					OnStart(kv, "Upload");
					break;
				case "ShareClient.Create.Stop":
				case "ShareClient.Delete.Stop":
				case "ShareDirectoryClient.Create.Stop":
				case "ShareDirectoryClient.Delete.Stop":
				case "ShareFileClient.Create.Stop":
				case "ShareFileClient.Delete.Stop":
				case "ShareFileClient.UploadRange.Stop":
					OnStop();
					break;
				case "ShareClient.Create.Exception":
				case "ShareClient.Delete.Exception":
				case "ShareDirectoryClient.Create.Exception":
				case "ShareDirectoryClient.Delete.Exception":
				case "ShareFileClient.Create.Exception":
				case "ShareFileClient.Delete.Exception":
				case "ShareFileClient.UploadRange.Exception":
					OnException(kv);
					break;
				default:
					Logger.Trace()?.Log("`{DiagnosticEventKey}' key is not a traced diagnostic event", kv.Key);
					break;
			}
		}

		private void OnStart(KeyValuePair<string, object> kv, string action)
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

			var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
			var spanName = FileShareUrl.TryCreate(urlTag, out var fileShareUrl)
				? $"{AzureFileStorage.SpanName} {action} {fileShareUrl.ResourceName}"
				: $"{AzureFileStorage.SpanName} {action}";

			var span = currentSegment.StartSpan(spanName, ApiConstants.TypeStorage, AzureFileStorage.SubType, action);
			if (span is Span realSpan)
				realSpan.InstrumentationFlag = InstrumentationFlag.Azure;

			if (fileShareUrl != null)
				SetDestination(span, fileShareUrl);

			if (!_processingSegments.TryAdd(activity.Id, span))
			{
				Logger.Trace()
					?.Log(
						"Could not add {Action} span {SpanId} for activity {ActivityId} to tracked spans",
						action,
						span.Id,
						activity.Id);
			}
		}

		private static void SetDestination(ISpan span, FileShareUrl fileShareUrl) =>
			span.Context.Destination = new Destination
			{
				Address = fileShareUrl.FullyQualifiedNamespace,
				Service = new Destination.DestinationService
				{
					Name = AzureFileStorage.SubType,
					Resource = $"{AzureFileStorage.SubType}/{fileShareUrl.StorageAccountName}",
					Type = ApiConstants.TypeStorage
				}
			};

		private void OnStop()
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_processingSegments.TryRemove(activity.Id, out var span))
			{
				Logger.Trace()
					?.Log(
						"Could not find segment for activity {ActivityId} in tracked segments",
						activity.Id);
				return;
			}

			if (span.Context.Destination is null)
			{
				var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
				if (FileShareUrl.TryCreate(urlTag, out var fileShareUrl))
				{
					span.Name += $" {fileShareUrl.ResourceName}";
					SetDestination(span, fileShareUrl);
				}
			}

			span.Outcome = Outcome.Success;
			span.End();
		}

		private void OnException(KeyValuePair<string, object> kv)
		{
			var activity = Activity.Current;
			if (activity is null)
			{
				Logger.Trace()?.Log("Current activity is null - exiting");
				return;
			}

			if (!_processingSegments.TryRemove(activity.Id, out var span))
			{
				Logger.Trace()
					?.Log(
						"Could not find segment for activity {ActivityId} in tracked segments",
						activity.Id);
				return;
			}

			if (span.Context.Destination is null)
			{
				var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
				if (FileShareUrl.TryCreate(urlTag, out var fileShareUrl))
				{
					span.Name += $" {fileShareUrl.ResourceName}";
					SetDestination(span, fileShareUrl);
				}
			}

			if (kv.Value is Exception e)
				span.CaptureException(e);

			span.Outcome = Outcome.Failure;
			span.End();
		}
	}
}
