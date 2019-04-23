using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNetFullFramework
{
	public class ElasticApmModule : IHttpModule
	{
		private static readonly bool _isCaptureHeadersEnabled;
		private static readonly IApmLogger _logger;

		static ElasticApmModule()
		{
			var configReader = new FullFrameworkConfigReader(ConsoleLogger.Instance);
			var agentComponents = new AgentComponents(configurationReader: configReader);
			UpdateServiceInformation(agentComponents.Service);
			Agent.Setup(agentComponents);
			_logger = Agent.Instance.Logger.Scoped(nameof(ElasticApmModule));
			_isCaptureHeadersEnabled = Agent.Instance.ConfigurationReader.CaptureHeaders;
		}

		// We can store current transaction because each IHttpModule is used for at most one request at a time
		// For example See https://bytes.com/topic/asp-net/answers/324305-httpmodule-multithreading-request-response-corelation
		private Transaction _currentTransaction;

		private static void UpdateServiceInformation(Service service)
		{
			service.Framework = new Framework { Name = "ASP.NET", Version = Environment.Version.ToString() };
			service.Language = new Language { Name = "C#" }; //TODO
		}

		public void Init(HttpApplication httpApp)
		{
			httpApp.BeginRequest += OnBeginRequest;
			httpApp.EndRequest += OnEndRequest;
		}

		public void Dispose()
		{
			// There's nothing to dispose because there's no disposable state kept per instance
		}

		private void OnBeginRequest(object eventSender, EventArgs eventArgs)
		{
			_logger.Debug()?.Log("Incoming request processing started - starting trace");

			try
			{
				ProcessBeginRequest(eventSender, eventArgs);
			}
			catch (Exception ex)
			{
				_logger.Error()?.Log("Processing BeginRequest event failed. Exception: {Exception}", ex);
			}
		}

		private void OnEndRequest(object eventSender, EventArgs eventArgs)
		{
			_logger.Debug()?.Log("Incoming request processing finished - ending trace");

			try
			{
				ProcessEndRequest(eventSender, eventArgs);
			}
			catch (Exception ex)
			{
				_logger.Error()?.Log("Processing EndRequest event failed. Exception: {Exception}", ex);
			}
		}

		private void ProcessBeginRequest(object eventSender, EventArgs eventArgs)
		{
			var httpApp = (HttpApplication)eventSender;
			var httpRequest = httpApp.Context.Request;

			_currentTransaction = Agent.Instance.TracerInternal.StartTransactionInternal(
				$"{httpRequest.HttpMethod} {httpRequest.Path}",
				ApiConstants.TypeRequest);

			if (_currentTransaction.IsSampled) FillSampledTransactionContextRequest(httpRequest, _currentTransaction);
		}

		private static void FillSampledTransactionContextRequest(HttpRequest httpRequest, ITransaction transaction)
		{
			var httpRequestUrl = httpRequest.Url;
			var url = new Url
			{
				Full = httpRequestUrl.AbsoluteUri,
				HostName = httpRequestUrl.Host,
				Protocol = "HTTP",
				Raw = httpRequestUrl.OriginalString
			};

			transaction.Context.Request = new Request(httpRequest.HttpMethod, url)
			{
				Socket = new Socket
				{
					Encrypted = httpRequest.IsSecureConnection,
					RemoteAddress = httpRequest.UserHostAddress
				},
				HttpVersion = GetHttpVersion(httpRequest.ServerVariables["SERVER_PROTOCOL"]),
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(httpRequest.Headers) : null
			};
		}

		private static string GetHttpVersion(string protocolString)
		{
			switch (protocolString)
			{
				case "HTTP/1.0":
					return "1.0";
				case "HTTP/1.1":
					return "1.1";
				case "HTTP/2.0":
					return "2.0";
				default:
					return protocolString.Replace("HTTP/", string.Empty);
			}
		}

		private static Dictionary<string, string> ConvertHeaders(NameValueCollection httpHeaders)
		{
			var convertedHeaders = new Dictionary<string, string>();
			foreach (var headerName in httpHeaders.AllKeys)
				convertedHeaders.Add(headerName, string.Join(",", httpHeaders.GetValues(headerName)));
			return convertedHeaders;
		}

		private void ProcessEndRequest(object eventSender, EventArgs eventArgs)
		{
			var httpApp = (HttpApplication)eventSender;
			var httpCtx = httpApp.Context;
			var httpResponse = httpCtx.Response;

			_currentTransaction.Result = Transaction.StatusCodeToResult("HTTP", httpResponse.StatusCode);

			if (_currentTransaction.IsSampled)
			{
				FillSampledTransactionContextResponse(httpResponse, _currentTransaction);
				FillSampledTransactionContextUser(httpCtx, _currentTransaction);
			}

			_currentTransaction?.End();
			_currentTransaction = null;
		}

		private void FillSampledTransactionContextResponse(HttpResponse httpResponse, ITransaction transaction) =>
			transaction.Context.Response = new Response
			{
				Finished = true,
				StatusCode = httpResponse.StatusCode,
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(httpResponse.Headers) : null
			};

		private void FillSampledTransactionContextUser(HttpContext httpCtx, Transaction transaction)
		{
			var userIdentity = httpCtx.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated) return;

			transaction.Context.User = new User
			{
				UserName = userIdentity.Name
			};

			_logger.Debug()?.Log("Captured user - {CapturedUser}", transaction.Context.User);
		}
	}
}
