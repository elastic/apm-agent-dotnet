// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Model;

namespace Elastic.Apm.Azure.Storage
{
	/// <summary>
	/// Creates HTTP spans wth Azure Blob storage details from Microsoft.Azure.Storage.Blob
	/// </summary>
	internal class MicrosoftAzureBlobStorageTracer : IHttpSpanTracer
	{
		public bool IsMatch(string method, Uri requestUrl, Func<string, string> headerGetter) =>
			requestUrl.Host.EndsWith(".blob.core.windows.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.usgovcloudapi.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.chinacloudapi.cn", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.cloudapi.de", StringComparison.Ordinal);

		public ISpan StartSpan(IApmAgent agent, string method, Uri requestUrl, Func<string, string> headerGetter)
		{
			var blobUrl = new BlobUrl(requestUrl);
			string action = null;

			switch (method)
			{
				case "DELETE":
					action = "Delete";
					break;
				case "GET":
					if (requestUrl.Query.Contains("restype=container"))
					{
						if (requestUrl.Query.Contains("comp=list"))
							action = "ListBlobs";
						else if (requestUrl.Query.Contains("comp=acl"))
							action = "GetAcl";
						else
							action = "GetProperties";
					}
					else
					{
						if (requestUrl.Query.Contains("comp=metadata"))
							action = "GetMetadata";
						else if (requestUrl.Query.Contains("comp=list"))
							action = "ListContainers";
						else if (requestUrl.Query.Contains("comp=tags"))
							action = requestUrl.Query.Contains("where=") ? "FindTags" : "GetTags";
						else
							action = "Download";
					}
					break;
				case "HEAD":
					if (requestUrl.Query.Contains("comp=metadata"))
						action = "GetMetadata";
					else if (requestUrl.Query.Contains("comp=acl"))
						action = "GetAcl";
					else
						action = "GetProperties";
					break;
				case "POST":
					if (requestUrl.Query.Contains("comp=batch"))
						action = "Batch";
					else if (requestUrl.Query.Contains("comp=query"))
						action = "Query";
					break;
				case "PUT":
					if (!string.IsNullOrEmpty(headerGetter("x-ms-copy-source")))
						action = "Copy";
					else if (requestUrl.Query.Contains("comp=copy"))
						action = "Abort";
					else if (!string.IsNullOrEmpty(headerGetter("x-ms-blob-type")) ||
						requestUrl.Query.Contains("comp=block") ||
						requestUrl.Query.Contains("comp=blocklist") ||
						requestUrl.Query.Contains("comp=page") ||
						requestUrl.Query.Contains("comp=appendblock"))
						action = "Upload";
					else if (requestUrl.Query.Contains("comp=metadata"))
						action = "SetMetadata";
					else if (requestUrl.Query.Contains("comp=acl"))
						action = "SetAcl";
					else if (requestUrl.Query.Contains("comp=properties"))
						action = "SetProperties";
					else if (requestUrl.Query.Contains("comp=lease"))
						action = "Lease";
					else if (requestUrl.Query.Contains("comp=snapshot"))
						action = "Snapshot";
					else if (requestUrl.Query.Contains("comp=undelete"))
						action = "Undelete";
					else if (requestUrl.Query.Contains("comp=tags"))
						action = "SetTags";
					else if (requestUrl.Query.Contains("comp=tier"))
						action = "SetTier";
					else if (requestUrl.Query.Contains("comp=expiry"))
						action = "SetExpiry";
					else if (requestUrl.Query.Contains("comp=seal"))
						action = "Seal";
					else
						action = "Create";

					break;
			}

			if (action is null)
				return null;

			var name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
			var span = ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(agent, name,
				ApiConstants.TypeStorage, AzureBlobStorage.SubType, InstrumentationFlag.Azure, true, true);
			span.Action = action;
			span.Context.Destination = new Destination
			{
				Address = blobUrl.FullyQualifiedNamespace,
				Service = new Destination.DestinationService
				{
					Resource = $"{AzureBlobStorage.SubType}/{blobUrl.StorageAccountName}",
				}
			};

			if (span is Span realSpan)
				realSpan.InstrumentationFlag = InstrumentationFlag.Azure;

			return span;
		}

		public bool ShouldSuppressSpanCreation() => false;
	}
}
