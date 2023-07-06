// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Elastic.AzureFunctionApp.Isolated;

public static class HttpTriggers
{
	[Function("SampleHttpTrigger")]
	public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("SampleHttpTrigger");
		logger.LogInformation("C# HTTP trigger function processed a request.");

		var response = req.CreateResponse(HttpStatusCode.OK);
		response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

		response.WriteString("Hello Azure Functions!\n");
		response.WriteString("======================\n");
		foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
			response.WriteString($"{e.Key} = {e.Value}\n");
		response.WriteString("======================\n");

		return response;
	}

	[Function("HttpTriggerWithInternalServerError")]
	public static HttpResponseData InternalServerError(
		[HttpTrigger(AuthorizationLevel.Function, "get")]
		HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("HttpTriggerWithInternalServerError");
		logger.LogInformation("C# HTTP trigger function processed a request.");

		return req.CreateResponse(HttpStatusCode.InternalServerError);
	}

	[Function("HttpTriggerWithNotFound")]
	public static HttpResponseData NotFound([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("HttpTriggerWithNotFound");
		logger.LogInformation("C# HTTP trigger function processed a request.");

		return req.CreateResponse(HttpStatusCode.NotFound);
	}

	[Function("HttpTriggerWithException")]
	public static HttpResponseData Exception([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
		FunctionContext executionContext)
	{
		var logger = executionContext.GetLogger("HttpTriggerWithException");
		logger.LogInformation("C# HTTP trigger function processed a request.");

		throw new Exception("whoops!");
	}
}
