// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Text;
using System.Web.Http;
using Elastic.Apm.AzureFunctionApp.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Elastic.AzureFunctionApp.InProcess;

public static class Triggers
{
	[FunctionName(FunctionName.SampleHttpTrigger)]
	public static IActionResult Run(
		[HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
		ILogger log)
	{
		log.LogInformation("C# HTTP trigger function (dotnet in-process) processed a request.");

		var responseMessage = new StringBuilder();
		responseMessage.AppendLine("Hello Azure Functions (in-process)!");
		responseMessage.AppendLine("======================");
		foreach (DictionaryEntry e in Environment.GetEnvironmentVariables())
		{
			log.LogInformation($"{e.Key} = {e.Value}");
			responseMessage.AppendLine($"{e.Key} = {e.Value}");
		}
		responseMessage.AppendLine("======================");

		return new OkObjectResult(responseMessage);
	}

	[FunctionName(FunctionName.HttpTriggerWithInternalServerError)]
	public static IActionResult InternalServerError(
		[HttpTrigger(AuthorizationLevel.Function, "get")]
		HttpRequest req,
		ILogger log)
	{
		log.LogInformation("C# HTTP trigger function processed a request.");

		return new InternalServerErrorResult();
	}

	[FunctionName(FunctionName.HttpTriggerWithNotFound)]
	public static IActionResult NotFound([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
		ILogger log)
	{
		log.LogInformation("C# HTTP trigger function processed a request.");
		return new NotFoundResult();
	}

	[FunctionName(FunctionName.HttpTriggerWithException)]
	public static IActionResult Exception([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
	{
		log.LogInformation("C# HTTP trigger function processed a request.");
		throw new Exception("whoops!");
	}
}
