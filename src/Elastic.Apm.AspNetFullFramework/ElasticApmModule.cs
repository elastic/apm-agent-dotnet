using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNetFullFramework
{
	public class ElasticApmModule : IHttpModule
	{
		private static bool _isCaptureHeadersEnabled;
		private static readonly DbgInstanceNameGenerator DbgInstanceNameGenerator = new DbgInstanceNameGenerator();

		private static readonly LazyContextualInit InitOnceHelper = new LazyContextualInit();

		// ReSharper disable once ImpureMethodCallOnReadonlyValueField
		public ElasticApmModule() => _dbgInstanceName = DbgInstanceNameGenerator.Generate($"{nameof(ElasticApmModule)}.#");

		// We can store current transaction because each IHttpModule is used for at most one request at a time
		// For example see https://bytes.com/topic/asp-net/answers/324305-httpmodule-multithreading-request-response-corelation
		private ITransaction _currentTransaction;

		private HttpApplication _httpApp;

		private IApmLogger _logger;

		private readonly string _dbgInstanceName;
		private static Version IisVersion => HttpRuntime.IISVersion;

		public void Init(HttpApplication httpApp)
		{
			var isInitedByThisCall = InitOnceForAllInstancesUnderLock(_dbgInstanceName);

			_logger = Agent.Instance.Logger.Scoped(_dbgInstanceName);

			if (isInitedByThisCall)
			{
				_logger.Debug()
					?.Log("Initialized Agent singleton. .NET runtime: {DotNetRuntimeDescription}; IIS: {IisVersion}",
						PlatformDetection.DotNetRuntimeDescription, IisVersion);
			}

			_httpApp = httpApp;
			_httpApp.BeginRequest += OnBeginRequest;
			_httpApp.EndRequest += OnEndRequest;
		}

		public void Dispose() => _httpApp = null;

		private void OnBeginRequest(object eventSender, EventArgs eventArgs)
		{
			_logger.Debug()?.Log("Incoming request processing started - starting trace...");

			try
			{
				ProcessBeginRequest(eventSender);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Processing BeginRequest event failed");
			}
		}

		private void OnEndRequest(object eventSender, EventArgs eventArgs)
		{
			_logger.Debug()?.Log("Incoming request processing finished - ending trace...");

			try
			{
				ProcessEndRequest(eventSender);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Processing EndRequest event failed");
			}
		}

		private void ProcessBeginRequest(object eventSender)
		{
			var httpApp = (HttpApplication)eventSender;
			var httpRequest = httpApp.Context.Request;

			var distributedTracingData = ExtractIncomingDistributedTracingData(httpRequest);
			if (distributedTracingData != null)
			{
				_logger.Debug()
					?.Log(
						"Incoming request with {TraceParentHeaderName} header. DistributedTracingData: {DistributedTracingData} - continuing trace",
						TraceParent.TraceParentHeaderName, distributedTracingData);

				_currentTransaction = Agent.Instance.Tracer.StartTransaction($"{httpRequest.HttpMethod} {httpRequest.Path}", ApiConstants.TypeRequest,
					distributedTracingData);
			}
			else
			{
				_logger.Debug()?.Log("Incoming request doesn't have valid incoming distributed tracing data - starting trace with new trace ID");
				_currentTransaction =
					Agent.Instance.Tracer.StartTransaction($"{httpRequest.HttpMethod} {httpRequest.Path}", ApiConstants.TypeRequest);
			}

			if (_currentTransaction.IsSampled) FillSampledTransactionContextRequest(httpRequest, _currentTransaction);
		}

		private DistributedTracingData ExtractIncomingDistributedTracingData(HttpRequest httpRequest)
		{
			var headerValue = httpRequest.Headers.Get(TraceParent.TraceParentHeaderName);
			if (headerValue == null)
			{
				_logger.Debug()
					?.Log("Incoming request doesn't have {TraceParentHeaderName} header - " +
						"it means request doesn't have incoming distributed tracing data", TraceParent.TraceParentHeaderName);
				return null;
			}
			return TraceParent.TryExtractTraceparent(headerValue);
		}

		private static void FillSampledTransactionContextRequest(HttpRequest httpRequest, ITransaction transaction)
		{
			var httpRequestUrl = httpRequest.Url;
			var queryString = httpRequestUrl.Query;
			var fullUrl = httpRequestUrl.AbsoluteUri;
			if (queryString.IsEmpty())
			{
				// Uri.Query returns empty string both when query string is empty ("http://host/path?") and
				// when there's no query string at all ("http://host/path") so we need a way to distinguish between these cases
				// HttpRequest.RawUrl contains only raw URL's path and query (not a full raw URL with protocol, host, etc.)
				if (httpRequest.RawUrl.IndexOf('?') == -1)
					queryString = null;
				else if (!fullUrl.IsEmpty() && fullUrl[fullUrl.Length - 1] != '?')
					fullUrl += "?";
			}
			else if (queryString[0] == '?')
				queryString = queryString.Substring(1, queryString.Length - 1);
			var url = new Url
			{
				Full = fullUrl,
				HostName = httpRequestUrl.Host,
				Protocol = "HTTP",
				Raw = fullUrl,
				PathName = httpRequestUrl.AbsolutePath,
				Search = queryString
			};

			transaction.Context.Request = new Request(httpRequest.HttpMethod, url)
			{
				Socket = new Socket { Encrypted = httpRequest.IsSecureConnection, RemoteAddress = httpRequest.UserHostAddress },
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

			if (_currentTransaction == null) return;

			_currentTransaction.Result = Transaction.StatusCodeToResult("HTTP", httpResponse.StatusCode);

			if (_currentTransaction.IsSampled)
			{
				FillSampledTransactionContextResponse(httpResponse, _currentTransaction);
				FillSampledTransactionContextUser(httpCtx, _currentTransaction);
			}

			_currentTransaction.End();
			_currentTransaction = null;
		}

		private void FillSampledTransactionContextResponse(HttpResponse httpResponse, ITransaction transaction) =>
			transaction.Context.Response = new Response
			{
				Finished = true,
				StatusCode = httpResponse.StatusCode,
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(httpResponse.Headers) : null
			};

		private void FillSampledTransactionContextUser(HttpContext httpCtx, ITransaction transaction)
		{
			var userIdentity = httpCtx.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated) return;

			transaction.Context.User = new User { UserName = userIdentity.Name };

			_logger.Debug()?.Log("Captured user - {CapturedUser}", transaction.Context.User);
		}

		private static string FindAspNetVersion(IApmLogger logger)
		{
			var aspNetVersion = "N/A";
			try
			{
				// We would like to report the same ASP.NET version as the one printed at the bottom of the error page
				// (see https://github.com/microsoft/referencesource/blob/master/System.Web/ErrorFormatter.cs#L431)
				// It is stored in VersionInfo.EngineVersion
				// (see https://github.com/microsoft/referencesource/blob/3b1eaf5203992df69de44c783a3eda37d3d4cd10/System.Web/Util/versioninfo.cs#L91)
				// which is unfortunately an internal property of an internal class in System.Web assembly so we use reflection to get it
				const string versionInfoTypeName = "System.Web.Util.VersionInfo";
				var versionInfoType = typeof(HttpRuntime).Assembly.GetType(versionInfoTypeName);
				if (versionInfoType == null)
				{
					logger.Error()
						?.Log("Type {TypeName} was not found in assembly {AssemblyFullName} - {AspNetVersion} will be used as ASP.NET version",
							versionInfoTypeName, typeof(HttpRuntime).Assembly.FullName, aspNetVersion);
					return aspNetVersion;
				}

				const string engineVersionPropertyName = "EngineVersion";
				var engineVersionProperty = versionInfoType.GetProperty(engineVersionPropertyName,
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (engineVersionProperty == null)
				{
					logger.Error()
						?.Log("Property {PropertyName} was not found in type {TypeName} - {AspNetVersion} will be used as ASP.NET version",
							engineVersionPropertyName, versionInfoType.FullName, aspNetVersion);
					return aspNetVersion;
				}

				var engineVersionPropertyValue = (string)engineVersionProperty.GetValue(null);
				if (engineVersionPropertyValue == null)
				{
					logger.Error()
						?.Log("Property {PropertyName} (in type {TypeName}) is of type {TypeName} and not a string as expected" +
							" - {AspNetVersion} will be used as ASP.NET version",
							engineVersionPropertyName, versionInfoType.FullName, engineVersionPropertyName.GetType().FullName, aspNetVersion);
					return aspNetVersion;
				}

				aspNetVersion = engineVersionPropertyValue;
			}
			catch (Exception ex)
			{
				logger.Error()?.LogException(ex, "Failed to obtain ASP.NET version - {AspNetVersion} will be used as ASP.NET version", aspNetVersion);
			}

			logger.Debug()?.Log("Found ASP.NET version: {AspNetVersion}", aspNetVersion);
			return aspNetVersion;
		}

		private static bool InitOnceForAllInstancesUnderLock(string dbgInstanceName)
		{
			var agentComponents = BuildAgentComponents(dbgInstanceName);

			return InitOnceHelper.IfNotInited?.Init(() =>
			{
				Agent.Setup(agentComponents);

				_isCaptureHeadersEnabled = Agent.Instance.ConfigurationReader.CaptureHeaders;

				Agent.Instance.Subscribe(new HttpDiagnosticsSubscriber());
			}) ?? false;
		}

		private static AgentComponents BuildAgentComponents(string dbgInstanceName)
		{
			var rootLogger = AgentDependencies.Logger ?? ConsoleLogger.Instance;
			var scopedLogger = rootLogger.Scoped(dbgInstanceName);

			var agentComponents = new AgentComponents(rootLogger, new FullFrameworkConfigReader(scopedLogger));

			var aspNetVersion = FindAspNetVersion(scopedLogger);

			agentComponents.Service.Framework = new Framework { Name = "ASP.NET", Version = aspNetVersion };
			agentComponents.Service.Language = new Language { Name = "C#" }; //TODO

			return agentComponents;
		}

	}
}
