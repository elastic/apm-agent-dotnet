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
	internal class FileShareStorageTracer : IHttpSpanTracer
	{
		public bool IsMatch(string method, Uri requestUrl, Func<string, string> headerGetter) =>
			requestUrl.Host.EndsWith(".file.core.windows.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".file.core.usgovcloudapi.net", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".file.core.chinacloudapi.cn", StringComparison.Ordinal) ||
			requestUrl.Host.EndsWith(".file.core.cloudapi.de", StringComparison.Ordinal);

		public ISpan StartSpan(IApmAgent agent, string method, Uri requestUrl, Func<string, string> headerGetter)
		{
			var fileShareUrl = new FileShareUrl(requestUrl);
			string action = null;

			switch (method)
			{
				case "DELETE":
					action = "Delete";
					break;
				case "GET":
					if (requestUrl.Query.Contains("comp=metadata"))
						action = "GetMetadata";
					else if (requestUrl.Query.Contains("comp=list"))
						action = "List";
					else if (requestUrl.Query.Contains("comp=properties") || requestUrl.Query.Contains("restype=share"))
						action = "GetProperties";
					else if (requestUrl.Query.Contains("comp=acl"))
						action = "GetAcl";
					else if (requestUrl.Query.Contains("comp=stats"))
						action = "Stats";
					else if (requestUrl.Query.Contains("comp=filepermission"))
						action = "GetPermission";
					else if (requestUrl.Query.Contains("comp=listhandles"))
						action = "ListHandles";
					else if (requestUrl.Query.Contains("comp=rangelist"))
						action = "ListRanges";
					else
						action = "Download";
					break;
				case "HEAD":
					if (requestUrl.Query.Contains("comp=metadata"))
						action = "GetMetadata";
					else if (requestUrl.Query.Contains("comp=acl"))
						action = "GetAcl";
					else
						action = "GetProperties";
					break;
				case "PUT":
					if (!string.IsNullOrEmpty(headerGetter("x-ms-copy-source")))
						action = "Copy";
					else if (!string.IsNullOrEmpty(headerGetter("x-ms-copy-action:abort")) && requestUrl.Query.Contains("comp=copy"))
						action = "Abort";
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
					else if (requestUrl.Query.Contains("comp=filepermission"))
						action = "SetPermission";
					else if (requestUrl.Query.Contains("comp=forceclosehandles"))
						action = "CloseHandles";
					else if (requestUrl.Query.Contains("comp=range"))
						action = "Upload";
					else
						action = "Create";

					break;
				case "OPTIONS":
					action = "Preflight";
					break;
			}

			if (action is null)
				return null;

			var name = $"{AzureFileStorage.SpanName} {action} {fileShareUrl.ResourceName}";
			var span = ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(agent, name,
				ApiConstants.TypeStorage, AzureFileStorage.SubType, InstrumentationFlag.Azure, true);
			span.Action = action;
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

			if (span is Span realSpan)
				realSpan.InstrumentationFlag = InstrumentationFlag.Azure;

			return span;
		}
	}
}
