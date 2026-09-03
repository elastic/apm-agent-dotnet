// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Elastic.Apm.DiagnosticListeners
{
	internal static class KnownListeners
	{
		// Known activity names
		public const string MicrosoftAspNetCoreHostingHttpRequestIn = "Microsoft.AspNetCore.Hosting.HttpRequestIn";
		public const string SystemNetHttpHttpRequestOut = "System.Net.Http.HttpRequestOut";
		public const string SystemNetHttpDesktopHttpRequestOut = "System.Net.Http.Desktop.HttpRequestOut";
		public const string ApmTransactionActivityName = "ElasticApm.Transaction";

		// ActivitySource name emitted by Microsoft.Azure.Cosmos >= 3.36.0 for operation-level spans
		public const string AzureCosmosOperationActivitySource = "Azure.Cosmos.Operation";

		public static readonly HashSet<string> SkippedActivityNamesSet =
		[
			MicrosoftAspNetCoreHostingHttpRequestIn,
			SystemNetHttpHttpRequestOut,
			SystemNetHttpDesktopHttpRequestOut,
			ApmTransactionActivityName
		];
	}
}
