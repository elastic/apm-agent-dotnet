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
		private readonly ConcurrentDictionary<string, ISpan> _processingSegments =
			new ConcurrentDictionary<string, ISpan>();

		public AzureBlobStorageDiagnosticListener(IApmAgent agent) : base(agent) { }

		public override string Name { get; } = "Azure.Storage.Blobs";

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);

			if (string.IsNullOrEmpty(kv.Key))
			{
				Logger.Trace()?.Log($"Key is {(kv.Key is null ? "null" : "an empty string")} - exiting");
				return;
			}

			if (kv.Key.EndsWith(".Stop", StringComparison.Ordinal))
				OnStop();
			else if (kv.Key.EndsWith(".Exception", StringComparison.Ordinal))
				OnException(kv);
			else
			{
				switch (kv.Key)
				{
					case "BlobContainerClient.Create.Start":
					case "PageBlobClient.Create.Start":
						OnStart(kv, "Create");
						break;
					case "BlobContainerClient.Delete.Start":
					case "BlobBaseClient.Delete.Start":
					case "BlobBaseClient.DeleteIfExists.Start":
						OnStart(kv, "Delete");
						break;
					case "BlobContainerClient.GetBlobs.Start":
					case "BlobContainerClient.GetBlobsByHierarchy.Start":
					case "ContainerClient.ListBlobsFlatSegment.Start":
					case "ContainerClient.ListBlobsHierarchySegment":
						OnStart(kv, "ListBlobs");
						break;
					case "BlockBlobClient.Upload.Start":
					case "BlockBlobClient.StageBlock.Start":
					case "BlockBlobClient.CommitBlockList.Start":
					case "PageBlobClient.UploadPages.Start":
					case "AppendBlobClient.AppendBlock.Start":
					case "AppendBlobClient.Create.Start":
					case "AppendBlobClient.CreateIfNotExists.Start":
					case "AppendBlobClient.OpenWrite.Start":
					case "BlockBlobClient.OpenWrite.Start":
					case "PageBlobClient.OpenWrite.Start":
					case "BlobClient.Upload.Start":
						OnStart(kv, "Upload");
						break;
					case "BlobBaseClient.Download.Start":
					case "BlobBaseClient.DownloadContent.Start":
					case "BlobBaseClient.DownloadStreaming.Start":
					case "BlobBaseClient.DownloadTo.Start":
					case "BlobBaseClient.OpenRead.Start":
					case "BlockBlobClient.GetBlockList.Start":
						OnStart(kv, "Download");
						break;
					case "BlobBaseClient.StartCopyFromUri.Start":
					case "AppendBlobClient.AppendBlockFromUri.Start":
					case "BlobBaseClient.SyncCopyFromUri.Start":
					case "BlockBlobClient.StageBlockFromUri.Start":
					case "BlockBlobClient.SyncUploadFromUri.Start":
					case "PageBlobClient.StartCopyIncremental.Start":
					case "PageBlobClient.UploadPagesFromUri.Start":
						OnStart(kv, "Copy");
						break;
					case "BlobBatchClient.SubmitBatch.Start":
						OnStart(kv, "Batch");
						break;
					case "BlobLeaseClient.Acquire.Start":
					case "BlobLeaseClient.Renew.Start":
					case "BlobLeaseClient.Release.Start":
					case "BlobLeaseClient.Change.Start":
					case "BlobLeaseClient.Break.Start":
						OnStart(kv, "Lease");
						break;
					case "AppendBlobClient.Seal.Start":
						OnStart(kv, "Seal");
						break;
					case "BlobBaseClient.GetProperties.Start":
					case "BlobContainerClient.GetProperties.Start":
					case "BlobServiceClient.GetAccountInfo.Start":
					case "BlobServiceClient.GetProperties.Start":
						OnStart(kv, "GetProperties");
						break;
					case "BlobBaseClient.AbortCopyFromUri.Start":
						OnStart(kv, "Abort");
						break;
					case "BlobBaseClient.Undelete.Start":
					case "BlobServiceClient.UndeleteBlobContainer.Start":
						OnStart(kv, "Undelete");
						break;
					case "BlobBaseClient.SetHttpHeaders.Start":
					case "BlobServiceClient.SetProperties.Start":
					case "PageBlobClient.Resize.Start":
					case "PageBlobClient.UpdateSequenceNumber.Start":
						OnStart(kv, "SetProperties");
						break;
					case "BlobBaseClient.SetMetadata.Start":
					case "BlobContainerClient.SetMetadata.Start":
						OnStart(kv, "SetMetadata");
						break;
					case "BlobBaseClient.CreateSnapshot.Start":
						OnStart(kv, "Snapshot");
						break;
					case "BlobBaseClient.SetAccessTier.Start":
						OnStart(kv, "SetTier");
						break;
					case "BlobBaseClient.GetTags.Start":
						OnStart(kv, "GetTags");
						break;
					case "BlobBaseClient.SetTags.Start":
						OnStart(kv, "SetTags");
						break;
					case "BlobContainerClient.GetAccessPolicy.Start":
						OnStart(kv, "GetAcl");
						break;
					case "BlobContainerClient.SetAccessPolicy.Start":
						OnStart(kv, "SetAcl");
						break;
					case "BlobContainerClient.Rename.Start":
					case "BlobServiceClient.RenameBlobContainer.Start":
						OnStart(kv, "Rename");
						break;
					case "BlobServiceClient.GetBlobContainers.Start":
						OnStart(kv, "ListContainers");
						break;
					case "BlobServiceClient.GetStatistics.Start":
						OnStart(kv, "Stats");
						break;
					case "BlobServiceClient.GetUserDelegationKey.Start":
						OnStart(kv, "GetUserDelegationKey");
						break;
					case "BlobServiceClient.FindBlobsByTags.Start":
						OnStart(kv, "FilterBlobs");
						break;
					case "BlockBlobClient.Query.Start":
						OnStart(kv, "Query");
						break;
					case "PageBlobClient.ClearPages.Start":
						OnStart(kv, "Clear");
						break;
					case "PageBlobClient.GetPageRanges.Start":
					case "PageBlobClient.GetPageRangesDiff.Start":
					case "PageBlobClient.GetManagedDiskPageRangesDiff.Start":
						OnStart(kv, "GetPageRanges");
						break;
					default:
						Logger.Trace()?.Log("`{DiagnosticEventKey}' key is not a traced diagnostic event", kv.Key);
						break;
				}
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
			var spanName = BlobUrl.TryCreate(urlTag, out var blobUrl)
				? $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}"
				: $"{AzureBlobStorage.SpanName} {action}";

			var span = currentSegment.StartSpan(spanName, ApiConstants.TypeStorage, AzureBlobStorage.SubType, action, true);
			if (span is Span realSpan)
				realSpan.InstrumentationFlag = InstrumentationFlag.Azure;

			if (blobUrl != null) SetDestination(span, blobUrl);

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

		private static void SetDestination(ISpan span, BlobUrl blobUrl) =>
			span.Context.Destination = new Destination
			{
				Address = blobUrl.FullyQualifiedNamespace,
				Service = new Destination.DestinationService
				{
					Resource = $"{AzureBlobStorage.SubType}/{blobUrl.StorageAccountName}"
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
				Logger.Trace()?.Log(
					"Could not find segment for activity {ActivityId} in tracked segments",
					activity.Id);
				return;
			}

			if (span.Context.Destination is null)
			{
				var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
				if (BlobUrl.TryCreate(urlTag, out var blobUrl))
				{
					span.Name += $" {blobUrl.ResourceName}";
					SetDestination(span, blobUrl);
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
				Logger.Trace()?.Log(
					"Could not find segment for activity {ActivityId} in tracked segments",
					activity.Id);
				return;
			}

			if (span.Context.Destination is null)
			{
				var urlTag = activity.Tags.FirstOrDefault(t => t.Key == "url").Value;
				if (BlobUrl.TryCreate(urlTag, out var blobUrl))
				{
					span.Name += $" {blobUrl.ResourceName}";
					SetDestination(span, blobUrl);
				}
			}

			if (kv.Value is Exception e)
				span.CaptureException(e);

			span.Outcome = Outcome.Failure;
			span.End();
		}
	}
}
