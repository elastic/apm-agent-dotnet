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
	/// Enriches Http spans wth Azure Blob storage details from Microsoft.Azure.Storage.Blob
	/// </summary>
	public class MicrosoftAzureBlobStorageEnricher : IHttpSpanEnricher
	{
		public bool IsMatch(string method, Uri requestUrl) =>
			requestUrl.Host.EndsWith(".blob.core.windows.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.usgovcloudapi.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".blob.core.chinacloudapi.cn", StringComparison.Ordinal);

		public void Enrich(string method, Uri requestUrl, Func<string, string> headerGetter, ISpan span)
		{
			var blobUrl = new BlobUrl(requestUrl);

			if (span is Span realSpan)
				realSpan.InstrumentationFlag = InstrumentationFlag.Azure;

			span.Type = AzureBlobStorage.Type;
			span.Subtype = AzureBlobStorage.SubType;
			span.Context.Destination = new Destination
			{
				Address = blobUrl.FullyQualifiedNamespace,
				Service = new Destination.DestinationService
				{
					Name = AzureBlobStorage.SubType,
					Resource = $"{AzureBlobStorage.SubType}/{blobUrl.ResourceName}",
					Type = AzureBlobStorage.Type
				}
			};

			switch (method)
			{
				case "PUT":
				{
					var blobType = headerGetter("x-ms-blob-type");
					var action = !string.IsNullOrEmpty(blobType) && blobType == "BlockBlob" ? "Upload" : "Create";
					span.Name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
					span.Action = action;
				}
					break;
				case "DELETE":
				{
					var action = "Delete";
					span.Name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
					span.Action = action;
				}
					break;
				case "GET":
				{
					var action = requestUrl.Query.Contains("restype=container") && requestUrl.Query.Contains("comp=list")
						? "GetBlobs"
						: "Download";
					span.Name = $"{AzureBlobStorage.SpanName} {action} {blobUrl.ResourceName}";
					span.Action = action;
				}
					break;
			}
		}
	}
}
