using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	public class AspNetCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly AbstractLogger _logger;

		public AspNetCoreDiagnosticListener()
			=> _logger = Agent.CreateLogger(Name);

		public string Name => "Microsoft.AspNetCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Key != "Microsoft.AspNetCore.Diagnostics.UnhandledException"
				&& kv.Key != "Microsoft.AspNetCore.Diagnostics.HandledException") return;

			var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("exception").GetValue(kv.Value) as Exception;

			var transaction = TransactionContainer.Transactions?.Value;

			transaction?.CaptureException(exception, "ASP.NET Core Unhandled Exception",
				kv.Key == "Microsoft.AspNetCore.Diagnostics.HandledException");
		}
	}
}
