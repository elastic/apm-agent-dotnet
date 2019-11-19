using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _agent;

		public AspNetCoreDiagnosticListener(IApmAgent agent) => _agent = agent;

		public string Name => "Microsoft.AspNetCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Key != "Microsoft.AspNetCore.Diagnostics.UnhandledException"
				&& kv.Key != "Microsoft.AspNetCore.Diagnostics.HandledException") return;

			var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("exception").GetValue(kv.Value) as Exception;

			var transaction = _agent.Tracer.CurrentTransaction;

			transaction?.CaptureException(exception, "ASP.NET Core Unhandled Exception",
				kv.Key == "Microsoft.AspNetCore.Diagnostics.HandledException");

			//Depending on config, request body may also be captured on errors. Since we do this async, this happens in the ApmMiddleware
		}
	}
}
