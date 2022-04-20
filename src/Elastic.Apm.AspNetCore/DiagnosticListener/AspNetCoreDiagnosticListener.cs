// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Reflection;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCoreDiagnosticListener : DiagnosticListenerBase
	{
		private readonly PropertyFetcher _defaultHttpContextFetcher = new PropertyFetcher("HttpContext");
		private readonly PropertyFetcher _exceptionContextPropertyFetcher = new PropertyFetcher("Exception");
		private readonly PropertyFetcher _httpContextPropertyFetcher = new PropertyFetcher("HttpContext");

		private readonly PropertyFetcher _hostDefaultHttpContextFetcher = new PropertyFetcher("HttpContext");
		private readonly PropertyFetcher _hostExceptionContextPropertyFetcher = new PropertyFetcher("Exception");

		/// <summary>
		/// Keeps track of ongoing transactions
		/// </summary>
		internal readonly ConditionalWeakTable<HttpContext, ITransaction> ProcessingRequests = new ConditionalWeakTable<HttpContext, ITransaction>();

		public AspNetCoreDiagnosticListener(ApmAgent agent) : base(agent) { }

		public override string Name => "Microsoft.AspNetCore";

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);

			if (kv.Key == $"{KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn}.Start")
			{
				if (_httpContextPropertyFetcher.Fetch(kv.Value) is HttpContext httpContextStart)
				{
					var createdTransaction = WebRequestTransactionCreator.StartTransactionAsync(httpContextStart, Logger, ApmAgent.Tracer,
						(ApmAgent as ApmAgent)?.ConfigurationStore.CurrentSnapshot);


					Transaction transaction = null;
					if (createdTransaction is Transaction t)
						transaction = t;

					if (transaction != null)
						WebRequestTransactionCreator.FillSampledTransactionContextRequest(transaction, httpContextStart, Logger);

					if (createdTransaction != null)
						ProcessingRequests.Add(httpContextStart, createdTransaction);
				}
			}
			else if (kv.Key == $"{KnownListeners.MicrosoftAspNetCoreHostingHttpRequestIn}.Stop")
			{
				if (_httpContextPropertyFetcher.Fetch(kv.Value) is HttpContext httpContextStop)
				{
					if (ProcessingRequests.TryGetValue(httpContextStop, out var createdTransaction))
					{
						if (createdTransaction is Transaction transaction)
							WebRequestTransactionCreator.StopTransaction(transaction, httpContextStop, Logger);
						else
							createdTransaction?.End();

						ProcessingRequests.Remove(httpContextStop);
					}
				}
			}
			switch (kv.Key)
			{
				case "Microsoft.AspNetCore.Diagnostics.UnhandledException": //Called when exception handler is registered
				case "Microsoft.AspNetCore.Diagnostics.HandledException":
					if (!(_defaultHttpContextFetcher.Fetch(kv.Value) is DefaultHttpContext httpContextDiagnosticsUnhandledException)) return;
					if (!(_exceptionContextPropertyFetcher.Fetch(kv.Value) is Exception diagnosticsException)) return;
					if (!ProcessingRequests.TryGetValue(httpContextDiagnosticsUnhandledException, out var iDiagnosticsTransaction)) return;

					if (iDiagnosticsTransaction is Transaction diagnosticsTransaction)
					{
						diagnosticsTransaction.CollectRequestBody(true, httpContextDiagnosticsUnhandledException.Request, Logger);
						diagnosticsTransaction.CaptureException(diagnosticsException);
					}

					break;
				case "Microsoft.AspNetCore.Hosting.UnhandledException": // Not called when exception handler registered
					if (!(_hostDefaultHttpContextFetcher.Fetch(kv.Value) is DefaultHttpContext httpContextUnhandledException)) return;
					if (!(_hostExceptionContextPropertyFetcher.Fetch(kv.Value) is Exception exception)) return;
					if (!ProcessingRequests.TryGetValue(httpContextUnhandledException, out var iCurrentTransaction)) return;

					if (iCurrentTransaction is Transaction currentTransaction)
					{
						currentTransaction.CollectRequestBody(true, httpContextUnhandledException.Request, Logger);
						currentTransaction.CaptureException(exception);
					}
					break;
			}
		}
	}
}
