using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNetFullFramework
{
	public class ElasticApmModule : IHttpModule
	{
		private static readonly Lazy<ApmAgent> Agent = new Lazy<ApmAgent>(SetupElasticApmAgent);
		private static bool _isCaptureHeadersEnabled;

		private Transaction _currentTransaction;
		private IApmLogger _logger;

		/// <summary>
		/// You will need to configure this module in the Web.config file of your
		/// web and register it with IIS before being able to use it. For more information
		/// see the following link: https://go.microsoft.com/?linkid=8101007
		/// </summary>
		public void Dispose()
		{
			// TODO: There can be multiple IHttpModule instances
			// Agent.Instance.Dispose();
		}

		public void Init(HttpApplication context)
		{
			_logger = Agent.Value.Logger.Scoped(nameof(ElasticApmModule));

			context.BeginRequest += OnBeginRequest;
			context.EndRequest += OnEndRequest;
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
				_logger.Error()?.Log("Processing BeginRequest event failed", ex);
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
				_logger.Error()?.Log("Processing EndRequest event failed", ex);
			}
		}

		private void ProcessBeginRequest(object eventSender, EventArgs eventArgs)
		{
			var httpApp = (HttpApplication)eventSender;
			var httpRequest = httpApp.Context.Request;

			_currentTransaction = Apm.Agent.Instance.TracerInternal.StartTransactionInternal(
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

		private static ApmAgent SetupElasticApmAgent()
		{
//			var configReader = configuration == null
//				? new EnvironmentConfigurationReader(logger)
//				: new MicrosoftExtensionsConfig(configuration, logger) as IConfigurationReader;
//
			var configReader = new EnvironmentConfigurationReader(ConsoleLogger.Instance);

//			UpdateServiceInformation(config.Service);

			Apm.Agent.Setup(new AgentComponents(configurationReader: configReader));

//			Agent.Instance.Subscribe(new HttpDiagnosticsSubscriber());

			_isCaptureHeadersEnabled = Apm.Agent.Instance.ConfigurationReader.CaptureHeaders;

			return Apm.Agent.Instance;
		}
	}
}
