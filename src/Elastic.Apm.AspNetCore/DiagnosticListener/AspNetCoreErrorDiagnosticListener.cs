// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCoreErrorDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _agent;

		public AspNetCoreErrorDiagnosticListener(IApmAgent agent) => _agent = agent;

		public string Name => "Microsoft.AspNetCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Key != "Microsoft.AspNetCore.Diagnostics.UnhandledException"
				&& kv.Key != "Microsoft.AspNetCore.Diagnostics.HandledException") return;

			var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("exception")?.GetValue(kv.Value) as Exception;
			var httpContextUnhandledException =
				kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("httpContext")?.GetValue(kv.Value) as DefaultHttpContext;

			var transaction = _agent.Tracer.CurrentTransaction as Transaction;
			transaction?.CollectRequestBody(true, httpContextUnhandledException?.Request, _agent.Logger, transaction.ConfigSnapshot);
			transaction?.CaptureException(exception, "ASP.NET Core Unhandled Exception",
				kv.Key == "Microsoft.AspNetCore.Diagnostics.HandledException");
		}
	}
}
