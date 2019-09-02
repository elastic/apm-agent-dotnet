using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.AspNetCore.Extensions;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly ScopedLogger _logger;
		private readonly IConfigurationReader _confgurationReader;
		private readonly IApmAgent _agent;

		public AspNetCoreDiagnosticListener(IApmAgent agent)
		{
			_agent = agent;
			_logger = agent.Logger?.Scoped(nameof(AspNetCoreDiagnosticListener));
			_confgurationReader = agent.ConfigurationReader;
		}

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

			var httpContext = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("httpContext").GetValue(kv.Value) as HttpContext;

			if (_confgurationReader.ShouldExtractRequestBodyOnError())
			{
				transaction.CollectRequestInfo(httpContext, _confgurationReader, _logger);
			}
		}
	}
}
