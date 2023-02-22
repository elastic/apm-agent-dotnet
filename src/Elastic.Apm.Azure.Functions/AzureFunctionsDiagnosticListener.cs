// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Extensions;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Elastic.Apm.Azure.Functions
{
	internal class AzureFunctionsDiagnosticListener : DiagnosticListenerBase
	{
		private static readonly AzureFunctionsContext Context = new(nameof(AzureFunctionsDiagnosticListener));

		public AzureFunctionsDiagnosticListener(ApmAgent agent) : base(agent) { }

		public override string Name => "Microsoft.AspNetCore";

		public override bool AllowDuplicates => true;

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			Context.Logger.Trace()?.Log($"'{nameof(AzureFunctionsDiagnosticListener)}.{nameof(HandleOnNext)}': {kv.Key}");
			if (kv.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start")
				HandleRequestInStart(kv.Value as DefaultHttpContext);
			else if (kv.Key == "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop")
				HandleRequestInStop(kv.Value as DefaultHttpContext);
		}

		private void HandleRequestInStart(DefaultHttpContext? httpContext)
		{
			if (httpContext is null)
			{
				Logger.Warning()?.Log($"${nameof(HandleRequestInStart)}: no ${nameof(DefaultHttpContext)} provided.");
			}
			else
			{
				var data = GetHttpRequestData(httpContext.Request);
				var functionName = httpContext.Request.Path.ToUriComponent().Split('/').Last();
				var transaction = ApmAgent.Tracer.StartTransaction(data.Name, ApiConstants.TypeRequest);
				transaction.FaaS = new Faas
				{
					Name = $"{Context.MetaData.WebsiteSiteName}/{functionName}",
					Id = $"{Context.FaasIdPrefix}{functionName}",
					Trigger = new Trigger { Type = data.TriggerType },
					ColdStart = AzureFunctionsContext.IsColdStart()
				};
				transaction.Context.Request = data.Request;
			}
		}

		private void HandleRequestInStop(DefaultHttpContext? httpContext)
		{
			if (httpContext is null)
			{
				Logger.Warning()?.Log($"${nameof(HandleRequestInStop)}: no ${nameof(DefaultHttpContext)} provided.");
			}
			else
			{
				var transaction = ApmAgent.Tracer.CurrentTransaction;
				if (transaction == null)
				{
					Logger.Warning()?.Log($"No current transaction.");
				}
				else
				{
					SetHttpResponseData(transaction, httpContext.Response);
					transaction.End();
				}
			}
		}

		private static TriggerSpecificData GetHttpRequestData(HttpRequest httpRequest)
		{
			var traceparent = httpRequest.Headers["traceparent"];
			var tracestate = httpRequest.Headers["tracestate"];

			return new($"{httpRequest.Method} {httpRequest.Path}", Trigger.TypeHttp,
				TraceContext.TryExtractTracingData(traceparent, tracestate))
			{
				Request = new Request(httpRequest.Method, new() { Full = UriHelper.GetDisplayUrl(httpRequest) })
				{
					Headers = CreateHeadersDictionary(httpRequest.Headers)
				}
			};
		}

		private static void SetHttpResponseData(ITransaction transaction, HttpResponse httpResponse)
		{
			transaction.Result = Transaction.StatusCodeToResult("HTTP", httpResponse.StatusCode);
			transaction.SetOutcomeForHttpResult(httpResponse.StatusCode);

			transaction.Context.Response = new Response
			{
				Finished = true,
				StatusCode = httpResponse.StatusCode,
				Headers = CreateHeadersDictionary(httpResponse.Headers)
			};
		}

		private static Dictionary<string, string> CreateHeadersDictionary(IHeaderDictionary headers) =>
			headers.ToDictionary(entry => entry.Key, entry => entry.Value.ToString());
	}
}
