using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Http;
using Elastic.Apm.AspNetCore.Extensions;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	internal class AspNetCoreDiagnosticListener : IDiagnosticListener
	{
		private readonly ScopedLogger _logger;

		public AspNetCoreDiagnosticListener(IApmAgent agent) => _logger = agent.Logger?.Scoped(nameof(AspNetCoreDiagnosticListener));

		public string Name => "Microsoft.AspNetCore";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Key != "Microsoft.AspNetCore.Diagnostics.UnhandledException"
				&& kv.Key != "Microsoft.AspNetCore.Diagnostics.HandledException") return;

			var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("exception").GetValue(kv.Value) as Exception;

			var transaction = Agent.TransactionContainer.Transactions?.Value;

			transaction?.CaptureException(exception, "ASP.NET Core Unhandled Exception",
				kv.Key == "Microsoft.AspNetCore.Diagnostics.HandledException");


			var httpContext = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("httpContext").GetValue(kv.Value) as HttpContext;

			CollectRequestInfo(httpContext, transaction);
		}

		// Collect necessary fields based on config & request state
		// Checks if according to the configuration and existance of request body - the Apm agent should
		// extract the request body for logging.
		private bool ShouldExtractRequestBody() => (Agent.Config.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyAll) ||
				Agent.Config.CaptureBody.Equals(ConfigConsts.SupportedValues.CaptureBodyErrors));

		private void CollectRequestInfo(HttpContext httpContext, Model.Transaction transaction)
		{
			var body = Consts.BodyRedacted; // According to the documentation - the default value of 'body' is '[Redacted]'
			if (httpContext?.Request != null && ShouldExtractRequestBody())
			{
				body = httpContext.Request.extractRequestBody(_logger);
			}
			transaction.Context.Request.Body = string.IsNullOrEmpty(body) ? Consts.BodyRedacted : body;
		}
	}
}
