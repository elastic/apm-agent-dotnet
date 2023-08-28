// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.AzureFunctionApp.Core;

public static class FunctionName
{
	public const string SampleHttpTrigger = nameof(SampleHttpTrigger);
	public const string HttpTriggerWithInternalServerError = nameof(HttpTriggerWithInternalServerError);
	public const string HttpTriggerWithNotFound = nameof(HttpTriggerWithNotFound);
	public const string HttpTriggerWithException = nameof(HttpTriggerWithException);
}
