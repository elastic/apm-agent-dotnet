using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCorePageLoadDiagnosticListener: IDiagnosticListener
	{
		private readonly ApmAgent _agent;
		private readonly PropertyFetcher _propertyFetcher = new PropertyFetcher("HttpContext");

		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		private readonly ConcurrentDictionary<HttpContext, Transaction> _processingRequests = new ConcurrentDictionary<HttpContext, Transaction>();

		public AspNetCorePageLoadDiagnosticListener(ApmAgent agent) => _agent = agent;

		public string Name => "Microsoft.AspNetCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			switch (kv.Key)
			{
				case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Start":
					if (_propertyFetcher.Fetch(kv.Value) is HttpContext httpContextStart)
					{
						var transaction = WebRequestTransactionCreator.StartTransactionAsync(httpContextStart, _agent.Logger, _agent.TracerInternal);
						_processingRequests[httpContextStart] = transaction;
					}
					break;

				case "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop":
					if (_propertyFetcher.Fetch(kv.Value) is HttpContext httpContextStop)
					{
						if (_processingRequests.TryRemove(httpContextStop, out var transaction)) WebRequestTransactionCreator.StopTransaction(transaction, httpContextStop, _agent.Logger);
					}
					break;
			}
		}
	}
}
