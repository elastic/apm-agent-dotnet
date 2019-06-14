using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetFullFramework.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.AspNetFullFramework
{
	public class ElasticApmModule : IHttpModule
	{
		private static readonly bool IsCaptureHeadersEnabled;
		private static readonly IApmLogger Logger;

		static ElasticApmModule()
		{
			var configReader = new FullFrameworkConfigReader(ConsoleLogger.Instance);
			var agentComponents = new AgentComponents(configurationReader: configReader);
			SetServiceInformation(agentComponents.Service);
			Agent.Setup(agentComponents);
			Logger = Agent.Instance.Logger.Scoped(nameof(ElasticApmModule));

			Logger.Debug()
				?.Log($"Entered {nameof(ElasticApmModule)} static ctor: ASP.NET: {AspNetVersion}, CLR: {ClrDescription}, IIS: {IisVersion}");

			IsCaptureHeadersEnabled = Agent.Instance.ConfigurationReader.CaptureHeaders;

			Agent.Instance.Subscribe(new HttpDiagnosticsSubscriber());
		}

		// We can store current transaction because each IHttpModule is used for at most one request at a time
		// For example see https://bytes.com/topic/asp-net/answers/324305-httpmodule-multithreading-request-response-corelation
		private Transaction _currentTransaction;

		private HttpApplication _httpApp;

		private static Version AspNetVersion => typeof(HttpRuntime).Assembly.GetName().Version;
		private static string ClrDescription => PlatformDetection.FrameworkDescription;
		private static Version IisVersion => HttpRuntime.IISVersion;

		private static void SetServiceInformation(Service service)
		{
			service.Framework = new Framework { Name = "ASP.NET", Version = AspNetVersion.ToString() };
			service.Language = new Language { Name = "C#" }; //TODO
		}

		public void Init(HttpApplication httpApp)
		{
			_httpApp = httpApp;
			_httpApp.BeginRequest += OnBeginRequest;
			_httpApp.EndRequest += OnEndRequest;
		}

		public void Dispose()
		{
			if (_httpApp != null)
			{
				_httpApp.BeginRequest -= OnBeginRequest;
				_httpApp.EndRequest -= OnEndRequest;
				_httpApp = null;
			}
		}

		private void OnBeginRequest(object eventSender, EventArgs eventArgs)
		{
			Logger.Debug()?.Log("Incoming request processing started - starting trace");

			try
			{
				ProcessBeginRequest(eventSender);
			}
			catch (Exception ex)
			{
				Logger.Error()?.Log("Processing BeginRequest event failed. Exception: {Exception}", ex);
			}
		}

		private void OnEndRequest(object eventSender, EventArgs eventArgs)
		{
			Logger.Debug()?.Log("Incoming request processing finished - ending trace");

			try
			{
				ProcessEndRequest(eventSender);
			}
			catch (Exception ex)
			{
				Logger.Error()?.Log("Processing EndRequest event failed. Exception: {Exception}", ex);
			}
		}

		private void ProcessBeginRequest(object eventSender)
		{
			var httpApp = (HttpApplication)eventSender;
			var httpRequest = httpApp.Context.Request;

			var distributedTracingData = ExtractIncomingDistributedTracingData(httpRequest);
			if (distributedTracingData != null)
			{
				Logger.Debug()
					?.Log(
						"Incoming request with {TraceParentHeaderName} header. DistributedTracingData: {DistributedTracingData} - continuing trace",
						TraceParent.TraceParentHeaderName, distributedTracingData);

				_currentTransaction = Agent.Instance.TracerInternal.StartTransactionInternal(
					$"{httpRequest.HttpMethod} {httpRequest.Path}",
					ApiConstants.TypeRequest,
					distributedTracingData);
			}
			else
			{
				Logger.Debug()?.Log("Incoming request doesn't have valid incoming distributed tracing data - starting trace with new trace id.");
				_currentTransaction = Agent.Instance.TracerInternal.StartTransactionInternal(
					$"{httpRequest.HttpMethod} {httpRequest.Path}",
					ApiConstants.TypeRequest);
			}

			if (_currentTransaction.IsSampled) FillSampledTransactionContextRequest(httpRequest, _currentTransaction);
		}

		private static DistributedTracingData ExtractIncomingDistributedTracingData(HttpRequest httpRequest)
		{
			var headerValue = httpRequest.Headers.Get(TraceParent.TraceParentHeaderName);
			if (headerValue == null)
			{
				Logger.Debug()
					?.Log("Incoming request doesn't {TraceParentHeaderName} header - " +
						"it means request doesn't have incoming distributed tracing data", TraceParent.TraceParentHeaderName);
				return null;
			}
			return TraceParent.TryExtractTraceparent(headerValue);
		}

		private static void FillSampledTransactionContextRequest(HttpRequest httpRequest, ITransaction transaction)
		{
			var httpRequestUrl = httpRequest.Url;
			var url = new Url
			{
				Full = httpRequestUrl.AbsoluteUri, HostName = httpRequestUrl.Host, Protocol = "HTTP", Raw = httpRequestUrl.OriginalString
			};

			transaction.Context.Request = new Request(httpRequest.HttpMethod, url)
			{
				Socket = new Socket { Encrypted = httpRequest.IsSecureConnection, RemoteAddress = httpRequest.UserHostAddress },
				HttpVersion = GetHttpVersion(httpRequest.ServerVariables["SERVER_PROTOCOL"]),
				Headers = IsCaptureHeadersEnabled ? ConvertHeaders(httpRequest.Headers) : null
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
			{
				var headerValue = httpHeaders.Get(headerName);
				if (headerValue != null) convertedHeaders.Add(headerName, headerValue);
			}
			return convertedHeaders;
		}

		private void ProcessEndRequest(object eventSender)
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
				Headers = IsCaptureHeadersEnabled ? ConvertHeaders(httpResponse.Headers) : null
			};

		private void FillSampledTransactionContextUser(HttpContext httpCtx, Transaction transaction)
		{
			var userIdentity = httpCtx.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated) return;

			transaction.Context.User = new User { UserName = userIdentity.Name };

			Logger.Debug()?.Log("Captured user - {CapturedUser}", transaction.Context.User);
		}
	}
}
