// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text.RegularExpressions;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Model;

namespace Elastic.Apm.Azure.Storage
{
	/// <summary>
	/// Creates HTTP spans wth Azure Blob storage details from Microsoft.Azure.Storage.Blob
	/// </summary>
	public class MicrosoftAzureBlobStorageCreator : IHttpSpanCreator
	{
		public bool IsMatch(string method, Uri requestUrl, Func<string, string> headerGetter) =>
			requestUrl.Host.EndsWith(".blob.core.windows.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.usgovcloudapi.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.chinacloudapi.cn", StringComparison.Ordinal);

		public ISpan Create(IApmAgent agent, string method, Uri requestUrl, Func<string, string> headerGetter)
		{
			var blobUrl = new BlobUrl(requestUrl);
			string name = null;
			string action = null;

			switch (method)
			{
				case "PUT":
					var blobType = headerGetter("x-ms-blob-type");
					action = !string.IsNullOrEmpty(blobType) && blobType == "BlockBlob" ? "Upload" : "Create";
					name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
					break;
				case "DELETE":
					action = "Delete";
					name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
					break;
				case "GET":
					action = requestUrl.Query.Contains("restype=container") && requestUrl.Query.Contains("comp=list")
						? "GetBlobs"
						: "Download";
					name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
					break;
			}

			// if this isn't a storage operation that we capture, don't create a span for it
			if (name == null)
				return null;

			var span = ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(agent, name,
				ApiConstants.TypeStorage, AzureBlobStorage.SubType, InstrumentationFlag.Azure, true);
			span.Action = action;
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

			if (span is Span realSpan)
				realSpan.InstrumentationFlag = InstrumentationFlag.Azure;

			return span;
		}
	}
}
