// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Web;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetFullFramework.Extensions;
using Elastic.Apm.Config.Net4FullFramework;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Reflection;
using Environment = System.Environment;
using TraceContext = Elastic.Apm.DistributedTracing.TraceContext;

namespace Elastic.Apm.AspNetFullFramework
{
	/// <summary>
	/// Captures each request in an APM transaction
	/// </summary>
	public class ElasticApmModule : IHttpModule
	{
		private static bool _isCaptureHeadersEnabled;
		// ReSharper disable once RedundantDefaultMemberInitializer
		private static readonly DbgInstanceNameGenerator DbgInstanceNameGenerator = new();
		private static readonly LazyContextualInit InitOnceHelper = new();
		private static readonly MethodInfo OnExecuteRequestStepMethodInfo = typeof(HttpApplication).GetMethod("OnExecuteRequestStep");


		private readonly string _dbgInstanceName;
		private HttpApplication _application;
		private IApmLogger _logger;

		private readonly Lazy<Type> _httpRouteDataInterfaceType =
			new Lazy<Type>(() => Type.GetType("System.Web.Http.Routing.IHttpRouteData,System.Web.Http"));

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

		internal static AgentComponents CreateAgentComponents(string debugName)
		{
			var logger = AgentDependencies.Logger ?? FullFrameworkDefaultImplementations.CreateDefaultLogger(null);

			var config = FullFrameworkDefaultImplementations.CreateConfigurationReaderFromConfiguredType(logger)
				?? new ElasticApmModuleConfiguration(logger);
			var agentComponents = new AgentComponentsUsingHttpContext(logger, config);
			agentComponents.Service.Language = new Language { Name = "C#" }; //TODO

			var scopedLogger = logger.Scoped(debugName);
			var aspNetVersion = AspNetVersion.GetEngineVersion(scopedLogger);
			if (aspNetVersion != null)
				agentComponents.Service.Framework = new Framework { Name = "ASP.NET", Version = aspNetVersion };

			return agentComponents;
		}

		private void InitImpl(HttpApplication application)
		{
			var isInitedByThisCall = InitOnceForAllInstancesUnderLock(_dbgInstanceName);

			_logger = Agent.Instance.Logger.Scoped(_dbgInstanceName);

			if (!Agent.Config.Enabled)
				return;

			if (!HttpRuntime.UsingIntegratedPipeline)
			{
				_logger.Error()
					?.Log("Skipping Initialization. Elastic APM Module requires the application pool to run under an Integrated Pipeline."
						+ " .NET runtime: {DotNetRuntimeDescription}; IIS: {IisVersion}",
						PlatformDetection.DotNetRuntimeDescription, HttpRuntime.IISVersion);
				return;
			}

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

			if (OnExecuteRequestStepMethodInfo != null)
			{
				// OnExecuteRequestStep is available starting with 4.7.1
				try
				{
					OnExecuteRequestStepMethodInfo.Invoke(application, new object[] { (Action<HttpContextBase, Action>)OnExecuteRequestStep });
				}
				catch (Exception e)
				{
					_logger.Error()
						 ?.LogException(e, "Failed to invoke OnExecuteRequestStep. .NET runtime: {DotNetRuntimeDescription}; IIS: {IisVersion}",
							 PlatformDetection.DotNetRuntimeDescription, HttpRuntime.IISVersion);
				}
			}
		}

		private void RestoreContextIfNeeded(HttpContextBase context)
		{
			string EventName() => Enum.GetName(typeof(RequestNotification), context.CurrentNotification);

			var urlPath = TryGetUrlPath(context);
			var ignoreUrls = Agent.Instance?.Configuration.TransactionIgnoreUrls;
			if (urlPath != null && ignoreUrls != null && WildcardMatcher.IsAnyMatch(ignoreUrls, urlPath))
				return;


			if (Agent.Instance == null)
			{
				_logger.Trace()?.Log($"Agent.Instance is null during {nameof(OnExecuteRequestStep)}:{EventName()}. url: {urlPath}");
				return;
			}
			if (Agent.Instance.Tracer == null)
			{
				_logger.Trace()?.Log($"Agent.Instance.Tracer is null during {nameof(OnExecuteRequestStep)}:{EventName()}. url: {urlPath}");
				return;
			}
			var transaction = Agent.Instance?.Tracer?.CurrentTransaction;
			if (transaction != null)
				return;
			if (Agent.Config.LogLevel <= LogLevel.Trace)
				return;

			var transactionInCurrent = HttpContext.Current?.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentTransactionKey] is not null;
			var transactionInApplicationInstance = context.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentTransactionKey] is not null;
			var spanInCurrent = HttpContext.Current?.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentSpanKey] is not null;
			var spanInApplicationInstance = context.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentSpanKey] is not null;

			_logger.Trace()?
				.Log($"{nameof(ITracer.CurrentTransaction)} is null during {nameof(OnExecuteRequestStep)}:{EventName()}. url: {urlPath} "
					+ $"(HttpContext.Current Span: {spanInCurrent}, Transaction: {transactionInCurrent})"
					+ $"(ApplicationContext Span: {spanInApplicationInstance}, Transaction: {transactionInApplicationInstance})");

			if (HttpContext.Current == null)
			{
				_logger.Trace()?
					.Log($"HttpContext.Current is null during {nameof(OnExecuteRequestStep)}:{EventName()}. Unable to attempt to restore transaction. url: {urlPath}");
				return;
			}

			if (!transactionInCurrent && transactionInApplicationInstance)
			{
				HttpContext.Current.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentTransactionKey] =
					context.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentTransactionKey];
				_logger.Trace()?.Log($"Restored transaction to HttpContext.Current.Items {nameof(OnExecuteRequestStep)}:{EventName()}. url: {urlPath}");
			}
			if (!spanInCurrent && spanInApplicationInstance)
			{
				HttpContext.Current.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentSpanKey] =
					context.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentSpanKey];
				_logger.Trace()?.Log($"Restored span to HttpContext.Current.Items {nameof(OnExecuteRequestStep)}:{EventName()}. url: {urlPath}");
			}

		}

		private string TryGetUrlPath(HttpContextBase context)
		{
			try
			{
				return context.Request.Unvalidated.Path;
			}
			catch
			{
				//ignore
				return string.Empty;
			}

		}

		private void OnExecuteRequestStep(HttpContextBase context, Action step)
		{

			RestoreContextIfNeeded(context);
			step();
		}

		private void OnBeginRequest(object sender, EventArgs e)
		{
			_logger.Debug()?.Log("Incoming request processing started - starting trace...");

			try
			{
				var usingLegacySynchronizationContext = SynchronizationContext.Current?.GetType().Name == "LegacyAspNetSynchronizationContext";
				if (usingLegacySynchronizationContext)
					_logger.Warning()?.Log("ASP.NET is using LegacyAspNetSynchronizationContext and might not behave well for asynchronous code");
			}
			catch
			{
				// ignored
			}

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

			if (WildcardMatcher.IsAnyMatch(Agent.Instance.Configuration.TransactionIgnoreUrls, request.Unvalidated.Path))
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

			if (transaction.IsSampled)
				FillSampledTransactionContextRequest(request, transaction, _logger);
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
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(request.Unvalidated.Headers) : null
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

		private static Dictionary<string, string> ConvertHeaders(NameValueCollection headers)
		{
			var convertedHeaders = new Dictionary<string, string>(headers.Count);
			foreach (var key in headers.AllKeys)
			{
				var value = headers.Get(key);
				if (value != null)
					convertedHeaders.Add(key, value);
			}
			return convertedHeaders;
		}

		private void ProcessError(object sender)
		{
			var transaction = Agent.Instance.Tracer.CurrentTransaction;
			if (transaction is null)
				return;

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
			var application = (HttpApplication)sender;
			var context = application.Context;
			var request = context.Request;
			var transaction = Agent.Instance.Tracer.CurrentTransaction;

			if (transaction is null)
			{
				// We expect transaction to be null if `TransactionIgnoreUrls` matches
				if (WildcardMatcher.IsAnyMatch(Agent.Instance.Configuration.TransactionIgnoreUrls, request.Unvalidated.Path))
					return;

				var hasHttpContext = HttpContext.Current?.Items[HttpContextCurrentExecutionSegmentsContainer.CurrentTransactionKey] is not null;
				_logger.Warning()
					?.Log(
						$"{nameof(ITracer.CurrentTransaction)} is null in {nameof(ProcessEndRequest)}. HttpContext for transaction: {hasHttpContext}"
					);
				return;
			}

			var response = context.Response;

			// update the transaction name based on route values, if applicable
			if (transaction is Transaction t && !t.HasCustomName)
			{
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
							foreach (var value in values)
								routeData.Add(value.Key, value.Value);
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
											_logger?.Trace()
												?.Log(
													$"Calculating transaction name from web api attribute routing (route precedence: {precedence})");
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

						if (!string.IsNullOrWhiteSpace(name))
							transaction.Name = $"{context.Request.HttpMethod} {name}";
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
				Headers = _isCaptureHeadersEnabled ? ConvertHeaders(response.Headers) : null
			};

		private void FillSampledTransactionContextUser(HttpContext context, ITransaction transaction)
		{
			if (transaction.Context.User != null)
				return;

			var userIdentity = context.User?.Identity;
			if (userIdentity == null || !userIdentity.IsAuthenticated)
				return;

			var user = new User { UserName = userIdentity.Name };

			if (context.User is ClaimsPrincipal claimsPrincipal)
			{
				try
				{
					static string GetClaimWithFallbackValue(ClaimsPrincipal principal, string claimType, string fallbackClaimType)
					{
						var claim = principal.Claims.FirstOrDefault(n => n.Type == claimType || n.Type == fallbackClaimType);
						return claim != null ? claim.Value : string.Empty;
					}

					user.Email = GetClaimWithFallbackValue(claimsPrincipal, ClaimTypes.Email, OpenIdClaimTypes.Email);
					user.Id = GetClaimWithFallbackValue(claimsPrincipal, ClaimTypes.NameIdentifier, OpenIdClaimTypes.UserId);
				}
				catch (SqlException ex)
				{
					_logger.Error()?.Log("Unable to access user claims due to SqlException with message: {message}", ex.Message);
				}
			}

			transaction.Context.User = user;

			_logger.Debug()?.Log("Captured user - {CapturedUser}", transaction.Context.User);
		}

		private static bool InitOnceForAllInstancesUnderLock(string dbgInstanceName) =>
			InitOnceHelper.IfNotInited?.Init(() =>
			{
				var agentComponents = CreateAgentComponents(dbgInstanceName);
				Agent.Setup(agentComponents);

				if (!Agent.Instance.Configuration.Enabled)
					return;

				_isCaptureHeadersEnabled = Agent.Instance.Configuration.CaptureHeaders;

				Agent.Instance.SubscribeIncludingAllDefaults();
			}) ?? false;

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
