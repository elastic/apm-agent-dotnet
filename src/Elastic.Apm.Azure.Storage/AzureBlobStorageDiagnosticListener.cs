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

namespace Elastic.Apm.Azure.Storage
{
	internal static class AzureBlobStorage
	{
		internal const string SpanName = "AzureBlob";
		internal const string SubType = "azureblob";
	}

	/// <summary>
	/// Creates transactions and spans for Azure Blob Storage diagnostic events from Azure.Storage.Blobs
	/// </summary>
	internal class AzureBlobStorageDiagnosticListener : DiagnosticListenerBase
	{
		private readonly ConcurrentDictionary<string, IExecutionSegment> _processingSegments =
			new ConcurrentDictionary<string, IExecutionSegment>();

		public AzureBlobStorageDiagnosticListener(IApmAgent agent) : base(agent) { }

		public override string Name { get; } = "Azure.Storage.Blobs";

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
				case "BlobContainerClient.Create.Start":
				case "PageBlobClient.Create.Start":
					OnStart(kv, "Create");
					break;
				case "BlobContainerClient.Delete.Start":
				case "BlobBaseClient.Delete.Start":
					OnStart(kv, "Delete");
					break;
				case "BlobContainerClient.GetBlobs.Start":
					OnStart(kv, "GetBlobs");
					break;
				case "BlockBlobClient.Upload.Start":
				case "PageBlobClient.UploadPages.Start":
					OnStart(kv, "Upload");
					break;
				case "BlobBaseClient.Download.Start":
				case "BlobBaseClient.DownloadContent.Start":
				case "BlobBaseClient.DownloadStreaming.Start":
					OnStart(kv, "Download");
					break;
				case "BlobBaseClient.StartCopyFromUri.Start":
					OnStart(kv, "CopyFromUri");
					break;
				case "BlobContainerClient.Create.Stop":
				case "BlobContainerClient.Delete.Stop":
				case "BlobBaseClient.Delete.Stop":
				case "PageBlobClient.Create.Stop":
				case "BlockBlobClient.Upload.Stop":
				case "BlobBaseClient.Download.Stop":
				case "BlobBaseClient.DownloadContent.Stop":
				case "BlobBaseClient.DownloadStreaming.Stop":
				case "PageBlobClient.UploadPages.Stop":
				case "BlobContainerClient.GetBlobs.Stop":
				case "BlobBaseClient.StartCopyFromUri.Stop":
					OnStop();
					break;
				case "BlobContainerClient.Create.Exception":
				case "BlobContainerClient.Delete.Exception":
				case "BlobBaseClient.Delete.Exception":
				case "PageBlobClient.Create.Exception":
				case "BlockBlobClient.Upload.Exception":
				case "BlobBaseClient.Download.Exception":
				case "BlobBaseClient.DownloadContent.Exception":
				case "BlobBaseClient.DownloadStreaming.Exception":
				case "PageBlobClient.UploadPages.Exception":
				case "BlobContainerClient.GetBlobs.Exception":
				case "BlobBaseClient.StartCopyFromUri.Exception":
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
			var blobUrl = new BlobUrl(urlTag);

			var spanName = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";

			var span = currentSegment.StartSpan(spanName, ApiConstants.TypeStorage, AzureBlobStorage.SubType, action);
			span.Context.Destination = new Destination
			{
				Address = blobUrl.FullyQualifiedNamespace,
				Service = new Destination.DestinationService
				{
					Name = AzureBlobStorage.SubType,
					Resource = $"{AzureBlobStorage.SubType}/{blobUrl.ResourceName}",
					Type = ApiConstants.TypeStorage
				}
			};

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
