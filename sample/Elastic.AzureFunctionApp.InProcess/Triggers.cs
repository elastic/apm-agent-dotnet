// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text;

namespace Elastic.AzureFunctionApp.InProcess
{
    public static class Triggers
    {
        [FunctionName("SampleHttpTrigger")]
        public static  IActionResult Run(
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
    }
}
