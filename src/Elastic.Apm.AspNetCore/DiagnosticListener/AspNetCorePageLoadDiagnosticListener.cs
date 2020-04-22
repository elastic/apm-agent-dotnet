using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCorePageLoadDiagnosticListener : IDiagnosticListener
	{
		private readonly ApmAgent _agent;
		private readonly PropertyFetcher _exceptionContextPropertyFetcher = new PropertyFetcher("Exception");
		private readonly PropertyFetcher _httpContextPropertyFetcher = new PropertyFetcher("HttpContext");
		private readonly PropertyFetcher _defaultHttpContextFetcher = new PropertyFetcher("HttpContext");
		private readonly IApmLogger _logger;

		/// <summary>
		/// Keeps track of ongoing transactions
		/// </summary>
		private readonly ConcurrentDictionary<HttpContext, Transaction> _processingRequests = new ConcurrentDictionary<HttpContext, Transaction>();

		public AspNetCorePageLoadDiagnosticListener(ApmAgent agent) =>
			(_agent, _logger) = (agent, agent.Logger.Scoped(nameof(AspNetCorePageLoadDiagnosticListener)));

		public string Name => "Microsoft.AspNetCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			_logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);

			switch (kv.Key)
			{
				case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
					if (_httpContextPropertyFetcher.Fetch(kv.Value) is HttpContext httpContextStart)
					{
						var transaction = WebRequestTransactionCreator.StartTransactionAsync(httpContextStart, _logger, _agent.TracerInternal);
						WebRequestTransactionCreator.FillSampledTransactionContextRequest(transaction, httpContextStart, _logger);
						_processingRequests[httpContextStart] = transaction;
					}
					break;
				case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
					if (_httpContextPropertyFetcher.Fetch(kv.Value) is HttpContext httpContextStop)
					{
						if (_processingRequests.TryRemove(httpContextStop, out var transaction))
						{
							if (transaction != null && transaction.IsContextCreated && httpContextStop.Response.StatusCode >= 400
								&& transaction.Context?.Request?.Body is string body
								&& (string.IsNullOrEmpty(body) || body == Apm.Consts.Redacted))
								transaction.CollectRequestBody(true, httpContextStop.Request, _logger, transaction.ConfigSnapshot);

							WebRequestTransactionCreator.StopTransaction(transaction, httpContextStop, _logger);
						}
					}
					break;
				case "Microsoft.AspNetCore.Hosting.UnhandledException":
					if (!(_defaultHttpContextFetcher.Fetch(kv.Value) is DefaultHttpContext httpContextUnhandledException)) return;
					if (!(_exceptionContextPropertyFetcher.Fetch(kv.Value) is Exception exception)) return;
					if (!_processingRequests.TryGetValue(httpContextUnhandledException, out var currentTransaction)) return;

					currentTransaction.CaptureException(exception);
					currentTransaction.CollectRequestBody(true, httpContextUnhandledException.Request, _logger, currentTransaction.ConfigSnapshot);
					break;
			}
		}
	}
}
