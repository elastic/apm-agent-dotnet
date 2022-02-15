// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Elastic.Apm.DiagnosticListeners
{
	internal static class KnownListeners
	{
		public const string MicrosoftAspNetCoreHostingHttpRequestIn = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
		public const string SystemNetHttpHttpRequestOut = "System.Net.Http.HttpRequestOut";
		public const string SystemNetHttpDesktopHttpRequestOut = "System.Net.Http.Desktop.HttpRequestOut";
		public const string ApmTransactionActivityName = "ElasticApm.Transaction";


		public static HashSet<string> KnownListenersList => new()
		{
			MicrosoftAspNetCoreHostingHttpRequestIn, SystemNetHttpHttpRequestOut, SystemNetHttpDesktopHttpRequestOut,
			ApmTransactionActivityName
		};
	}
}
