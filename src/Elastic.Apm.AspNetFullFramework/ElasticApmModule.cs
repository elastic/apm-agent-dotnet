﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetFullFramework.Extensions;
using Elastic.Apm.AspNetFullFramework.Helper;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.AspNetFullFramework
{
	internal static class OpenIdClaimTypes
	{
		internal const string Email = "email";
		internal const string UserId = "sub";
	}

	public class ElasticApmModule : IHttpModule
	{
		private static bool _isCaptureHeadersEnabled;
		private static readonly DbgInstanceNameGenerator DbgInstanceNameGenerator = new DbgInstanceNameGenerator();

		private static readonly LazyContextualInit InitOnceHelper = new LazyContextualInit();

		private readonly string _dbgInstanceName;

		// ReSharper disable once ImpureMethodCallOnReadonlyValueField
		public ElasticApmModule() => _dbgInstanceName = DbgInstanceNameGenerator.Generate($"{nameof(ElasticApmModule)}.#");

		private HttpApplication _httpApp;

		private IApmLogger _logger;
		private static Version IisVersion => HttpRuntime.IISVersion;

		public void Init(HttpApplication httpApp)
		{
			try
			{
				InitImpl(httpApp);
			}
			catch (Exception ex)
			{
				const string linePrefix = "Elastic APM .NET Agent: ";
				System.Diagnostics.Trace.WriteLine($"{linePrefix}[CRITICAL] Exception thrown by {nameof(ElasticApmModule)}.{nameof(InitImpl)}."
					+ Environment.NewLine + linePrefix + $"+-> Exception: {ex.GetType().FullName}: {ex.Message}"
					+ Environment.NewLine + TextUtils.PrefixEveryLine(ex.StackTrace, linePrefix + " ".Repeat(4))
				);
			}
		}

		private void InitImpl(HttpApplication httpApp)
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

			if (WildcardMatcher.IsAnyMatch(Agent.Instance.ConfigurationReader.TransactionIgnoreUrls, httpRequest.Unvalidated.Path))
			{
				_logger.Debug()?.Log("Request ignored based on TransactionIgnoreUrls, url: {urlPath}", httpRequest.Unvalidated.Path);
				return;
			}

			var transactionName = $"{httpRequest.HttpMethod} {httpRequest.Unvalidated.Path}";

			var soapAction = SoapRequest.ExtractSoapAction(httpRequest.Unvalidated.Headers, httpRequest.InputStream, _logger);
			if (soapAction != null) transactionName += $" {soapAction}";

			var distributedTracingData = ExtractIncomingDistributedTracingData(httpRequest);
			ITransaction transaction;

			if (distributedTracingData != null)
			{
				_logger.Debug()
					?.Log(
						"Incoming request with {TraceParentHeaderName} header. DistributedTracingData: {DistributedTracingData} - continuing trace",
						DistributedTracing.TraceContext.TraceParentHeaderNamePrefixed, distributedTracingData);

				// we set ignoreActivity to true to avoid the HttpContext W3C DiagnosticSource issue (see https://github.com/elastic/apm-agent-dotnet/issues/867#issuecomment-650170150)
				transaction = Agent.Instance.Tracer.StartTransaction(transactionName, ApiConstants.TypeRequest, distributedTracingData, true);
			}
			else
			{
				_logger.Debug()
					?.Log("Incoming request doesn't have valid incoming distributed tracing data - starting trace with new trace ID");

				// we set ignoreActivity to true to avoid the HttpContext W3C DiagnosticSource issue(see https://github.com/elastic/apm-agent-dotnet/issues/867#issuecomment-650170150)
				transaction = Agent.Instance.Tracer.StartTransaction(transactionName, ApiConstants.TypeRequest, ignoreActivity: true);
			}

			if (transaction.IsSampled) FillSampledTransactionContextRequest(httpRequest, transaction);
		}

		/// <summary>
		/// Extracts the traceparent and the tracestate headers from the <see cref="httpRequest"/>
		/// </summary>
		/// <param name="httpRequest"></param>
		/// <returns>Null if traceparent is not set, otherwise the filled DistributedTracingData instance</returns>
		private DistributedTracingData ExtractIncomingDistributedTracingData(HttpRequest httpRequest)
		{
			var traceParentHeaderValue = httpRequest.Unvalidated.Headers.Get(DistributedTracing.TraceContext.TraceParentHeaderName);
			// ReSharper disable once InvertIf
			if (traceParentHeaderValue == null)
			{
				traceParentHeaderValue = httpRequest.Unvalidated.Headers.Get(DistributedTracing.TraceContext.TraceParentHeaderNamePrefixed);

				if (traceParentHeaderValue == null)
				{
					_logger.Debug()
						?.Log("Incoming request doesn't have {TraceParentHeaderName} header - " +
							"it means request doesn't have incoming distributed tracing data", DistributedTracing.TraceContext.TraceParentHeaderNamePrefixed);
					return null;
				}
			}

			var traceStateHeaderValue = httpRequest.Unvalidated.Headers.Get(DistributedTracing.TraceContext.TraceStateHeaderName);

			return traceStateHeaderValue != null
				? DistributedTracing.TraceContext.TryExtractTracingData(traceParentHeaderValue, traceStateHeaderValue)
				: DistributedTracing.TraceContext.TryExtractTracingData(traceParentHeaderValue);
		}

		private static void FillSampledTransactionContextRequest(HttpRequest httpRequest, ITransaction transaction)
		{
			var httpRequestUrl = httpRequest.Unvalidated.Url;
			var queryString = httpRequestUrl.Query;
			var fullUrl = httpRequestUrl.AbsoluteUri;
			if (queryString.IsEmpty())
			{
				// Uri.Query returns empty string both when query string is empty ("http://host/path?") and
				// when there's no query string at all ("http://host/path") so we need a way to distinguish between these cases
				// HttpRequest.RawUrl contains only raw URL path and query (not a full raw URL with protocol, host, etc.)
				if (httpRequest.Unvalidated.RawUrl.IndexOf('?') == -1)
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
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(httpRequest.Unvalidated.Headers) : null
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
					return protocolString?.Replace("HTTP/", string.Empty);
			}
		}

		private static Dictionary<string, string> ConvertHeaders(NameValueCollection httpHeaders)
		{
			var convertedHeaders = new Dictionary<string, string>(httpHeaders.Count);
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
			var transaction = Agent.Instance.Tracer.CurrentTransaction;

			if (transaction == null) return;

			SendErrorEventIfPresent(httpCtx, transaction);

			// update the transaction name based on route values, if applicable
			if (transaction is Transaction t && !t.HasCustomName)
			{
				var values = httpApp.Request.RequestContext?.RouteData?.Values;
				if (values?.Count > 0)
				{
					// Determine if the route data *actually* routed to a controller action or not i.e.
					// we need to differentiate between
					// 1. route data that didn't route to a controller action and returned a 404
					// 2. route data that did route to a controller action, and the action result returned a 404
					//
					// In normal MVC setup, the former will set a HttpException with a 404 status code with System.Web.Mvc as the source.
					// We need to check the source of the exception because we want to differentiate between a 404 HttpException from the
					// framework and a 404 HttpException from the application.
					if (httpCtx.Error is null || !(httpCtx.Error is HttpException httpException) ||
						httpException.Source != "System.Web.Mvc" || httpException.GetHttpCode() != StatusCodes.Status404NotFound)
					{
						// handle MVC areas. The area name will be included in the DataTokens.
						object area = null;
						httpApp.Request.RequestContext?.RouteData?.DataTokens?.TryGetValue("area", out area);
						IDictionary<string, object> routeData;
						if (area != null)
						{
							routeData = new Dictionary<string, object>(values.Count + 1);
							foreach (var value in values) routeData.Add(value.Key, value.Value);
							routeData.Add("area", area);
						}
						else
							routeData = values;

						_logger?.Trace()?.Log("Calculating transaction name based on route data");
						var name = Transaction.GetNameFromRouteContext(routeData);
						if (!string.IsNullOrWhiteSpace(name)) transaction.Name = $"{httpCtx.Request.HttpMethod} {name}";
					}
					else
					{
						// dealing with a 404 HttpException that came from System.Web.Mvc
						_logger?.Trace()?
							.Log("Route data found but a HttpException with 404 status code was thrown from System.Web.Mvc - setting transaction name to 'unknown route");
						transaction.Name = $"{httpCtx.Request.HttpMethod} unknown route";
					}
				}
			}

			transaction.Result = Transaction.StatusCodeToResult("HTTP", httpResponse.StatusCode);

			if (httpResponse.StatusCode >= 500)
				transaction.Outcome = Outcome.Failure;
			else
				transaction.Outcome = Outcome.Success;

			if (transaction.IsSampled)
			{
				FillSampledTransactionContextResponse(httpResponse, transaction);
				FillSampledTransactionContextUser(httpCtx, transaction);
			}

			transaction.End();
			transaction = null;
		}

		private void SendErrorEventIfPresent(HttpContext httpCtx, ITransaction transaction)
		{
			var lastError = httpCtx.Server.GetLastError();
			if (lastError != null) transaction.CaptureException(lastError);
		}

		private static void FillSampledTransactionContextResponse(HttpResponse httpResponse, ITransaction transaction) =>
			transaction.Context.Response = new Response
			{
				Finished = true,
				StatusCode = httpResponse.StatusCode,
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(httpResponse.Headers) : null
			};

		private void FillSampledTransactionContextUser(HttpContext httpCtx, ITransaction transaction)
		{
			if (transaction.Context.User != null) return;

			var userIdentity = httpCtx.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated) return;

			var user = new User { UserName = userIdentity.Name };

			if (httpCtx.User is ClaimsPrincipal claimsPrincipal)
			{
				static string GetClaimWithFallbackValue(ClaimsPrincipal principal, string claimType, string fallbackClaimType)
				{
					var claim = principal.Claims.FirstOrDefault(n => n.Type == claimType || n.Type == fallbackClaimType);
					return claim != null ? claim.Value : string.Empty;
				}

				user.Email = GetClaimWithFallbackValue(claimsPrincipal, ClaimTypes.Email, OpenIdClaimTypes.Email);
				user.Id = GetClaimWithFallbackValue(claimsPrincipal, ClaimTypes.NameIdentifier, OpenIdClaimTypes.UserId);
			}

			transaction.Context.User = user;

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

		private static bool InitOnceForAllInstancesUnderLock(string dbgInstanceName) =>
			InitOnceHelper.IfNotInited?.Init(() =>
			{
				SafeAgentSetup(dbgInstanceName);

				_isCaptureHeadersEnabled = Agent.Instance.ConfigurationReader.CaptureHeaders;

				Agent.Instance.Subscribe(new HttpDiagnosticsSubscriber());
			}) ?? false;

		private static IApmLogger BuildLogger() => AgentDependencies.Logger ?? ConsoleLogger.Instance;

		private static AgentComponents BuildAgentComponents(string dbgInstanceName)
		{
			var rootLogger = BuildLogger();
			var scopedLogger = rootLogger.Scoped(dbgInstanceName);

			var reader = ConfigHelper.CreateReader(rootLogger) ?? new FullFrameworkConfigReader(rootLogger);

			var agentComponents = new AgentComponents(
				rootLogger,
				reader,
				null,
				null,
				new HttpContextCurrentExecutionSegmentsContainer(),
				null);

			var aspNetVersion = FindAspNetVersion(scopedLogger);

			agentComponents.Service.Framework = new Framework { Name = "ASP.NET", Version = aspNetVersion };
			agentComponents.Service.Language = new Language { Name = "C#" }; //TODO

			return agentComponents;
		}

		private static void SafeAgentSetup(string dbgInstanceName)
		{
			var agentComponents = BuildAgentComponents(dbgInstanceName);
			try
			{
				Agent.Setup(agentComponents);
			}
			catch (Agent.InstanceAlreadyCreatedException ex)
			{
				BuildLogger().Scoped(dbgInstanceName)
					.Error()
					?.LogException(ex, "The Elastic APM agent was already initialized before call to"
						+ $" {nameof(ElasticApmModule)}.{nameof(Init)} - {nameof(ElasticApmModule)} will use existing instance"
						+ " even though it might lead to unexpected behavior"
						+ " (for example agent using incorrect configuration source such as environment variables instead of Web.config).");

				agentComponents.Dispose();
			}
		}
	}

	internal static class StatusCodes
	{
		public const int Status404NotFound = 404;
	}
}
