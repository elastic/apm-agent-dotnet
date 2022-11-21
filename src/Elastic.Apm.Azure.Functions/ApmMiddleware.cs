// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Cloud;
using Elastic.Apm.Extensions;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using TraceContext = Elastic.Apm.DistributedTracing.TraceContext;

namespace Elastic.Apm.Azure.Functions;

public class ApmMiddleware : IFunctionsWorkerMiddleware
{
	private static readonly IApmLogger Logger;
	private static readonly string FaasIdPrefix;
	private static int ColdStart = 1;

	static ApmMiddleware()
	{
		Logger = Agent.Instance.Logger.Scoped(nameof(ApmMiddleware));
		var metaData = new AzureFunctionsMetadataProvider(Logger,
			new EnvironmentVariables(Logger).GetEnvironmentVariables()).GetMetadata();
		FaasIdPrefix =
			$"/subscriptions/{metaData?.Account?.Id}/resourceGroups/{metaData?.Project}/providers/Microsoft.Web/sites/{metaData?.Instance}/functions/";
		Logger.Trace()?.Log($"FaasIdPrefix: {FaasIdPrefix}");
	}

	private static bool IsColdStart() => Interlocked.Exchange(ref ColdStart, 0) == 1;

	public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
	{
		Logger.Trace()?.Log($"{nameof(Invoke)} - {context.FunctionDefinition.Name}");

		var data = GetTriggerSpecificData(context);
		await Agent.Tracer.CaptureTransaction(data.Name, ApiConstants.TypeRequest, async t =>
		{
			var success = true;
			t.FaaS = new Faas
			{
				Name = context.FunctionDefinition.Name,
				Id = $"{FaasIdPrefix}{context.FunctionDefinition.Name}",
				Trigger = new Trigger { Type = data.TriggerType },
				Execution = context.InvocationId,
				ColdStart = IsColdStart()
			};

			try
			{
				await next(context);
			}
			catch (Exception ex)
			{
				success = false;
				Logger.Log(LogLevel.Error, $"Exception was thrown during '{nameof(Invoke)}'", ex, null);
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
		if (httpRequestData != null)
		{
			Logger.Trace()?.Log("HTTP Trigger type detected.");

			httpRequestData.Headers.TryGetValues("traceparent", out var traceparent);
			httpRequestData.Headers.TryGetValues("tracestate", out var tracestate);
			var name = $"{httpRequestData.Method} {httpRequestData.Url.AbsolutePath}";
			return new TriggerSpecificData(name, Trigger.TypeHttp,
				TraceContext.TryExtractTracingData(traceparent?.FirstOrDefault(), tracestate?.FirstOrDefault()));
		}

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
}
