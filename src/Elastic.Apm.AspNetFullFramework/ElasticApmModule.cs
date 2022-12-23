// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetFullFramework.Extensions;
using Elastic.Apm.AspNetFullFramework.Helper;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using TraceContext = Elastic.Apm.DistributedTracing.TraceContext;
using Elastic.Apm.Reflection;
using Elastic.Apm.Extensions;

namespace Elastic.Apm.AspNetFullFramework
{
	/// <summary>
	/// Captures each request in an APM transaction
	/// </summary>
	public class ElasticApmModule : IHttpModule
	{
		private static bool _isCaptureHeadersEnabled;
		private static readonly DbgInstanceNameGenerator DbgInstanceNameGenerator = new DbgInstanceNameGenerator();
		private static readonly LazyContextualInit InitOnceHelper = new LazyContextualInit();

		private readonly string _dbgInstanceName;
		private HttpApplication _application;
		private IApmLogger _logger;
		private readonly Lazy<Type> _httpRouteDataInterfaceType = new Lazy<Type>(() => Type.GetType("System.Web.Http.Routing.IHttpRouteData,System.Web.Http"));
		private Func<object, string> _routeDataTemplateGetter;
		private Func<object, decimal> _routePrecedenceGetter;

		public ElasticApmModule() =>
			// ReSharper disable once ImpureMethodCallOnReadonlyValueField
			_dbgInstanceName = DbgInstanceNameGenerator.Generate($"{nameof(ElasticApmModule)}.#");

		public void Dispose() => _application = null;

		/// <inheritdoc />
		public void Init(HttpApplication application)
		{
			try
			{
				InitImpl(application);
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

		/// <summary>
		/// Creates a new instance of <see cref="AgentComponents"/> configured
		/// to use with ASP.NET Full Framework.
		/// </summary>
		/// <returns>a new instance of <see cref="AgentComponents"/></returns>
		public static AgentComponents CreateAgentComponents() => CreateAgentComponents($"{nameof(ElasticApmModule)}.#0");

		private void InitImpl(HttpApplication application)
		{
			var isInitedByThisCall = InitOnceForAllInstancesUnderLock(_dbgInstanceName);

			_logger = Agent.Instance.Logger.Scoped(_dbgInstanceName);

			if (!Agent.Config.Enabled)
				return;

			if (isInitedByThisCall)
			{
				_logger.Debug()
					?.Log("Initialized Agent singleton. .NET runtime: {DotNetRuntimeDescription}; IIS: {IisVersion}",
						PlatformDetection.DotNetRuntimeDescription, HttpRuntime.IISVersion);
			}

			_routeDataTemplateGetter = CreateWebApiAttributeRouteTemplateGetter();
			_routePrecedenceGetter = CreateRoutePrecedenceGetter();
			_application = application;
			_application.BeginRequest += OnBeginRequest;
			_application.EndRequest += OnEndRequest;
			_application.Error += OnError;
		}

		private void OnBeginRequest(object sender, EventArgs e)
		{
			_logger.Debug()?.Log("Incoming request processing started - starting trace...");

			try
			{
				ProcessBeginRequest(sender);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Processing BeginRequest event failed");
			}
		}

		private void OnError(object sender, EventArgs e)
		{
			try
			{
				ProcessError(sender);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Processing Error event failed");
			}
		}

		private void OnEndRequest(object sender, EventArgs e)
		{
			_logger.Debug()?.Log("Incoming request processing finished - ending trace...");

			try
			{
				ProcessEndRequest(sender);
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Processing EndRequest event failed");
			}
		}

		private void ProcessBeginRequest(object sender)
		{
			var application = (HttpApplication)sender;
			var request = application.Context.Request;

			if (WildcardMatcher.IsAnyMatch(Agent.Instance.ConfigurationReader.TransactionIgnoreUrls, request.Unvalidated.Path))
			{
				_logger.Debug()?.Log("Request ignored based on TransactionIgnoreUrls, url: {urlPath}", request.Unvalidated.Path);
				return;
			}

			var transactionName = $"{request.HttpMethod} {request.Unvalidated.Path}";

			var distributedTracingData = ExtractIncomingDistributedTracingData(request);
			ITransaction transaction;

			if (distributedTracingData != null)
			{
				_logger.Debug()
					?.Log(
						"Incoming request with {TraceParentHeaderName} header. DistributedTracingData: {DistributedTracingData} - continuing trace",
						TraceContext.TraceParentHeaderName, distributedTracingData);

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

			if (transaction.IsSampled) FillSampledTransactionContextRequest(request, transaction, _logger);
		}

		/// <summary>
		/// Extracts the traceparent and the tracestate headers from the request
		/// </summary>
		/// <param name="request">The request</param>
		/// <returns>Null if traceparent is not set, otherwise the filled DistributedTracingData instance</returns>
		private DistributedTracingData ExtractIncomingDistributedTracingData(HttpRequest request)
		{
			var traceParentHeaderValue = request.Unvalidated.Headers.Get(TraceContext.TraceParentHeaderName);
			// ReSharper disable once InvertIf
			if (traceParentHeaderValue == null)
			{
				traceParentHeaderValue = request.Unvalidated.Headers.Get(TraceContext.TraceParentHeaderNamePrefixed);

				if (traceParentHeaderValue == null)
				{
					_logger.Debug()
						?.Log("Incoming request doesn't have {TraceParentHeaderName} header - " +
							"it means request doesn't have incoming distributed tracing data", TraceContext.TraceParentHeaderNamePrefixed);
					return null;
				}
			}

			var traceStateHeaderValue = request.Unvalidated.Headers.Get(TraceContext.TraceStateHeaderName);
			return TraceContext.TryExtractTracingData(traceParentHeaderValue, traceStateHeaderValue);
		}

		private static void FillSampledTransactionContextRequest(HttpRequest request, ITransaction transaction, IApmLogger logger)
		{
			var httpRequestUrl = request.Unvalidated.Url;
			var queryString = httpRequestUrl.Query;
			var fullUrl = httpRequestUrl.AbsoluteUri;
			if (queryString.IsEmpty())
			{
				// Uri.Query returns empty string both when query string is empty ("http://host/path?") and
				// when there's no query string at all ("http://host/path") so we need a way to distinguish between these cases
				// HttpRequest.RawUrl contains only raw URL path and query (not a full raw URL with protocol, host, etc.)
				if (request.Unvalidated.RawUrl.IndexOf('?') == -1)
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

			transaction.Context.Request = new Request(request.HttpMethod, url)
			{
				Socket = new Socket { RemoteAddress = request.UserHostAddress },
				HttpVersion = GetHttpVersion(request.ServerVariables["SERVER_PROTOCOL"]),
				Headers = _isCaptureHeadersEnabled
					? ConvertHeaders(request.Unvalidated.Headers, (transaction as Transaction)?.Configuration)
					: null
			};
		}

		private static string GetHttpVersion(string protocol)
		{
			switch (protocol)
			{
				case "HTTP/1.0":
					return "1.0";
				case "HTTP/1.1":
					return "1.1";
				case "HTTP/2.0":
					return "2.0";
				default:
					return protocol?.Replace("HTTP/", string.Empty);
			}
		}

		private static Dictionary<string, string> ConvertHeaders(NameValueCollection headers, IConfiguration configuration)
		{
			var convertedHeaders = new Dictionary<string, string>(headers.Count);
			foreach (var key in headers.AllKeys)
			{
				var value = headers.Get(key);
				if (value != null)
				{
					convertedHeaders.Add(key,
						WildcardMatcher.IsAnyMatch(configuration?.SanitizeFieldNames, key) ? Consts.Redacted : value);
				}
			}
			return convertedHeaders;
		}

		private void ProcessError(object sender)
		{
			var transaction = Agent.Instance.Tracer.CurrentTransaction;
			if (transaction is null) return;

			var application = (HttpApplication)sender;
			var exception = application.Server.GetLastError();
			if (exception != null)
			{
				if (exception is HttpUnhandledException unhandledException && unhandledException.InnerException != null)
					exception = unhandledException.InnerException;

				transaction.CaptureException(exception);
			}
		}

		private void ProcessEndRequest(object sender)
		{
			var transaction = Agent.Instance.Tracer.CurrentTransaction;
			if (transaction is null) return;

			var application = (HttpApplication)sender;
			var context = application.Context;
			var response = context.Response;

			// update the transaction name based on route values, if applicable
			if (transaction is Transaction t && !t.HasCustomName)
			{
				var request = application.Request;
				var values = request.RequestContext?.RouteData?.Values;
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
					if (context.Error is not HttpException httpException ||
						httpException.Source != "System.Web.Mvc" ||
						httpException.GetHttpCode() != 404)
					{
						// handle MVC areas. The area name will be included in the DataTokens.
						object area = null;
						request.RequestContext?.RouteData?.DataTokens?.TryGetValue("area", out area);
						IDictionary<string, object> routeData;
						if (area != null)
						{
							routeData = new Dictionary<string, object>(values.Count + 1);
							foreach (var value in values) routeData.Add(value.Key, value.Value);
							routeData.Add("area", area);
						}
						else
							routeData = values;

						string name = null;

						// if we're dealing with Web API attribute routing, get transaction name from the route template
						if (routeData.TryGetValue("MS_SubRoutes", out var template) && _httpRouteDataInterfaceType.Value != null)
						{
							if (template is IEnumerable enumerable)
							{
								var minPrecedence = decimal.MaxValue;
								var enumerator = enumerable.GetEnumerator();
								while (enumerator.MoveNext())
								{
									var subRoute = enumerator.Current;
									if (subRoute != null && _httpRouteDataInterfaceType.Value.IsInstanceOfType(subRoute))
									{
										var precedence = _routePrecedenceGetter(subRoute);
										if (precedence < minPrecedence)
										{
											_logger?.Trace()?.Log($"Calculating transaction name from web api attribute routing (route precedence: {precedence})");
											minPrecedence = precedence;
											name = _routeDataTemplateGetter(subRoute);
										}
									}
								}
							}
						}
						else
						{
							_logger?.Trace()?.Log("Calculating transaction name based on route data");
							name = Transaction.GetNameFromRouteContext(routeData);
						}

						if (!string.IsNullOrWhiteSpace(name)) transaction.Name = $"{context.Request.HttpMethod} {name}";
					}
					else
					{
						// dealing with a 404 HttpException that came from System.Web.Mvc
						_logger?.Trace()
							?
							.Log(
								"Route data found but a HttpException with 404 status code was thrown from System.Web.Mvc - setting transaction name to 'unknown route");
						transaction.Name = $"{context.Request.HttpMethod} unknown route";
					}
				}
			}

			transaction.Result = Transaction.StatusCodeToResult("HTTP", response.StatusCode);

			var realTransaction = transaction as Transaction;
			realTransaction?.SetOutcome(response.StatusCode >= 500
					? Outcome.Failure
					: Outcome.Success);

			// Try and update transaction name with SOAP action if applicable.
			if (realTransaction == null || !realTransaction.HasCustomName)
			{
				if (SoapRequest.TryExtractSoapAction(_logger, context.Request, out var soapAction))
					transaction.Name += $" {soapAction}";
			}

			if (transaction.IsSampled)
			{
				FillSampledTransactionContextResponse(response, transaction);
				FillSampledTransactionContextUser(context, transaction);
				transaction.CollectRequestBody(false, new AspNetHttpRequest(context.Request), _logger);
			}

			transaction.End();
			transaction = null;
		}

		private static void FillSampledTransactionContextResponse(HttpResponse response, ITransaction transaction) =>
			transaction.Context.Response = new Response
			{
				Finished = true,
				StatusCode = response.StatusCode,
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(response.Headers, (transaction as Transaction)?.Configuration) : null
			};

		private void FillSampledTransactionContextUser(HttpContext context, ITransaction transaction)
		{
			if (transaction.Context.User != null) return;

			var userIdentity = context.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated) return;

			var user = new User { UserName = userIdentity.Name };

			if (context.User is ClaimsPrincipal claimsPrincipal)
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

		private static bool InitOnceForAllInstancesUnderLock(string dbgInstanceName) =>
			InitOnceHelper.IfNotInited?.Init(() =>
			{
				var agentComponents = CreateAgentComponents(dbgInstanceName);
				Agent.Setup(agentComponents);

				if (!Agent.Instance.ConfigurationReader.Enabled)
					return;

				_isCaptureHeadersEnabled = Agent.Instance.ConfigurationReader.CaptureHeaders;

				Agent.Instance.Subscribe(new HttpDiagnosticsSubscriber());
			}) ?? false;

		private static IApmLogger CreateDefaultLogger()
		{
			var logLevel = ConfigurationManager.AppSettings[ConfigConsts.KeyNames.LogLevel];
			if (string.IsNullOrEmpty(logLevel))
				logLevel = Environment.GetEnvironmentVariable(ConfigConsts.EnvVarNames.LogLevel);

			var level = ConfigConsts.DefaultValues.LogLevel;
			if (!string.IsNullOrEmpty(logLevel))
				Enum.TryParse(logLevel, true, out level);

			return new TraceLogger(level);
		}

		private static AgentComponents CreateAgentComponents(string dbgInstanceName)
		{
			var rootLogger = AgentDependencies.Logger ?? CreateDefaultLogger();
			var reader = ConfigHelper.CreateReader(rootLogger) ?? new FullFrameworkConfigReader(rootLogger);
			var agentComponents = new FullFrameworkAgentComponents(rootLogger, reader);

			var scopedLogger = rootLogger.Scoped(dbgInstanceName);
			var aspNetVersion = AspNetVersion.GetEngineVersion(scopedLogger);
			agentComponents.Service.Framework = new Framework { Name = "ASP.NET", Version = aspNetVersion };
			agentComponents.Service.Language = new Language { Name = "C#" }; //TODO

			return agentComponents;
		}

		/// <summary>
		/// Compiles a delegate from a lambda expression to get a route's DataTokens property,
		/// which holds the precedence value.
		/// </summary>
		private Func<object, decimal> CreateRoutePrecedenceGetter()
		{
			if (_httpRouteDataInterfaceType.Value != null)
			{
				var routePropertyInfo = _httpRouteDataInterfaceType.Value.GetProperty("Route");
				if (routePropertyInfo != null)
				{
					var routeType = routePropertyInfo.PropertyType;
					var dataTokensPropertyInfo = routeType.GetProperty("DataTokens");
					if (dataTokensPropertyInfo != null)
					{
						var routePropertyGetter = ExpressionBuilder.BuildPropertyGetter(_httpRouteDataInterfaceType.Value, routePropertyInfo);
						var dataTokensPropertyGetter = ExpressionBuilder.BuildPropertyGetter(routeType, dataTokensPropertyInfo);
						return subRoute =>
						{
							var precedence = decimal.MaxValue;
							var route = routePropertyGetter(subRoute);
							if (route != null)
							{
								var dataTokens = dataTokensPropertyGetter(route) as IDictionary<string, object>;
								object v = null;
								if (dataTokens?.TryGetValue("precedence", out v) ?? true)
									precedence = (decimal)v;
							}
							return precedence;
						};
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Compiles a delegate from a lambda expression to get the route template from HttpRouteData when
		/// System.Web.Http is referenced.
		/// </summary>
		private Func<object, string> CreateWebApiAttributeRouteTemplateGetter()
		{
			if (_httpRouteDataInterfaceType.Value != null)
			{
				var routePropertyInfo = _httpRouteDataInterfaceType.Value.GetProperty("Route");
				if (routePropertyInfo != null)
				{
					var routeType = routePropertyInfo.PropertyType;
					var routeTemplatePropertyInfo = routeType.GetProperty("RouteTemplate");
					if (routeTemplatePropertyInfo != null)
					{
						var routePropertyGetter = ExpressionBuilder.BuildPropertyGetter(_httpRouteDataInterfaceType.Value, routePropertyInfo);
						var routeTemplatePropertyGetter = ExpressionBuilder.BuildPropertyGetter(routeType, routeTemplatePropertyInfo);
						return routeData =>
						{
							var route = routePropertyGetter(routeData);
							return route is null
								? null
								: routeTemplatePropertyGetter(route) as string;
						};
					}
				}
			}

			return null;
		}
	}
}
