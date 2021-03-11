// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCoreErrorDiagnosticListener : DiagnosticListenerBase
	{
		public AspNetCoreErrorDiagnosticListener(IApmAgent agent) : base(agent) { }

		public override string Name => "Microsoft.AspNetCore";

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Key != "Microsoft.AspNetCore.Diagnostics.UnhandledException"
				&& kv.Key != "Microsoft.AspNetCore.Diagnostics.HandledException") return;

			var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("exception")?.GetValue(kv.Value) as Exception;
			var httpContextUnhandledException =
				kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("httpContext")?.GetValue(kv.Value) as DefaultHttpContext;

			var transaction = ApmAgent.Tracer.CurrentTransaction as Transaction;
			transaction?.CollectRequestBody(true, httpContextUnhandledException?.Request, Logger);
			transaction?.CaptureException(exception, "ASP.NET Core Unhandled Exception",
				kv.Key == "Microsoft.AspNetCore.Diagnostics.HandledException");
		}
	}
}
