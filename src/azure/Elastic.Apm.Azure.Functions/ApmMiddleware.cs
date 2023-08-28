// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Extensions;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using TraceContext = Elastic.Apm.DistributedTracing.TraceContext;

namespace Elastic.Apm.Azure.Functions;

public class ApmMiddleware : IFunctionsWorkerMiddleware
{
	private static readonly AzureFunctionsContext Context = new(nameof(ApmMiddleware));

	public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		Context.Logger.Trace()?.Log($"{nameof(Invoke)} - {context.FunctionDefinition.Name}");

		var data = GetTriggerSpecificData(context);
		await Agent.Tracer.CaptureTransaction(data.Name, ApiConstants.TypeRequest, async t =>
		{
			var success = true;
			t.FaaS = new Faas
			{
				Name = $"{Context.MetaData.WebsiteSiteName}/{context.FunctionDefinition.Name}",
				Id = $"{Context.FaasIdPrefix}{context.FunctionDefinition.Name}",
				Trigger = new Trigger { Type = data.TriggerType },
				Execution = context.InvocationId,
				ColdStart = AzureFunctionsContext.IsColdStart()
			};
			t.Context.Request = data.Request;
			try
			{
				await next(context);
			}
			catch (Exception ex)
			{
				success = false;
				t.CaptureException(ex);
				Context.Logger.Error()?.LogException(ex, $"Exception was thrown during '{nameof(Invoke)}'");
				throw;
			}
			finally
			{
				SetTriggerSpecificResult(t, success, context);
			}
		}, data.TracingData);
	}

	private static TriggerSpecificData GetTriggerSpecificData(FunctionContext context)
	{
		var httpRequestData = GetHttpRequestData(context);
		if (httpRequestData != null) // HTTP Trigger
		{
			Context.Logger.Trace()?.Log("HTTP Trigger type detected.");

			httpRequestData.Headers.TryGetValues("traceparent", out var traceparent);
			httpRequestData.Headers.TryGetValues("tracestate", out var tracestate);

			return new($"{httpRequestData.Method} {httpRequestData.Url.AbsolutePath}", Trigger.TypeHttp,
				TraceContext.TryExtractTracingData(traceparent?.FirstOrDefault(), tracestate?.FirstOrDefault()))
			{
				Request = new Request(httpRequestData.Method, Url.FromUri(httpRequestData.Url))
				{
					Headers = CreateHeadersDictionary(httpRequestData.Headers),
				}
			};
		}

		// Generic
		return new TriggerSpecificData(context.FunctionDefinition.Name, Trigger.TypeOther, null);
	}

	private static void SetTriggerSpecificResult(ITransaction transaction, bool success, FunctionContext context)
	{
		var httpResponseData = GetProperty<HttpResponseData>(context, "InvocationResult");
		if (httpResponseData != null) // HTTP Trigger
		{
			var httpStatusCode = (int)httpResponseData.StatusCode;
			transaction.Result = Transaction.StatusCodeToResult("HTTP", httpStatusCode);
			transaction.SetOutcomeForHttpResult(httpStatusCode);

			transaction.Context.Response = new Response
			{
				Finished = true,
				StatusCode = httpStatusCode,
				Headers = CreateHeadersDictionary(httpResponseData.Headers)
			};
		}
		else // Generic
		{
			if (success)
			{
				transaction.Result = "success";
				transaction.Outcome = Outcome.Success;
			}
			else
			{
				transaction.Result = "failure";
				transaction.Outcome = Outcome.Failure;
			}
		}
	}

	private static Dictionary<string, string> CreateHeadersDictionary(HttpHeadersCollection httpHeadersCollection) =>
		httpHeadersCollection.ToDictionary(h => h.Key, h => string.Join(",", h.Value));

	private static HttpRequestData? GetHttpRequestData(FunctionContext functionContext)
	{
		var inputData = GetProperty<IReadOnlyDictionary<string, object>>(functionContext, "InputData");
		return inputData?.Values.SingleOrDefault(o => o is HttpRequestData) as HttpRequestData;
	}

	private static T? GetProperty<T>(FunctionContext functionContext, string name) where T : class
	{
		var feature = functionContext.Features.SingleOrDefault(f => f.Key.Name == "IFunctionBindingsFeature").Value;
		return feature?.GetType().GetProperties().SingleOrDefault(p => p.Name == name)?.GetValue(feature) as T;
	}
}

internal struct TriggerSpecificData
{
	internal TriggerSpecificData(string name, string triggerType, DistributedTracingData? tracingData)
	{
		Name = name;
		TriggerType = triggerType;
		TracingData = tracingData;
	}

	internal DistributedTracingData? TracingData { get; }

	internal string TriggerType { get; }

	internal string Name { get; }

	internal Request? Request { get; set; }
}
