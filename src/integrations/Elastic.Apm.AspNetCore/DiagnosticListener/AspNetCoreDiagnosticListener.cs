// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Extensions;
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
				case "Microsoft.AspNetCore.Diagnostics.UnhandledException":
					HandleException(_defaultHttpContextFetcher, _exceptionContextPropertyFetcher, kv, false);
					break;
				case "Microsoft.AspNetCore.Diagnostics.HandledException":
					HandleException(_defaultHttpContextFetcher, _exceptionContextPropertyFetcher, kv, true);
					break;
				case "Microsoft.AspNetCore.Hosting.UnhandledException": // Not called when exception handler registered
					HandleException(_hostDefaultHttpContextFetcher, _hostExceptionContextPropertyFetcher, kv, false);
					break;
			}
		}

		private bool HandleException(PropertyFetcher propertyFetcher, PropertyFetcher exceptionPropertyFetcher, KeyValuePair<string, object> kv, bool isHandled)
		{
			if (propertyFetcher.Fetch(kv.Value) is not DefaultHttpContext exception)
				return false;
			if (exceptionPropertyFetcher.Fetch(kv.Value) is not Exception httpContextException)
				return false;
			if (!ProcessingRequests.TryGetValue(exception, out var iTransaction))
				return false;
			if (iTransaction is Transaction transaction)
			{
				transaction.CollectRequestBody(true, new AspNetCoreHttpRequest(exception.Request), Logger);
				transaction.CaptureException(httpContextException, isHandled: isHandled);
			}

			return true;
		}
	}
}
