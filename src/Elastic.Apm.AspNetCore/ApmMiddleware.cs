using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.AspNetCore
{
	// ReSharper disable once ClassNeverInstantiated.Global
	internal class ApmMiddleware
	{
		private readonly IConfigurationReader _configurationReader;
		private readonly IApmLogger _logger;

		private readonly RequestDelegate _next;
		private readonly Tracer _tracer;

		public ApmMiddleware(RequestDelegate next, Tracer tracer, IApmAgent agent)
		{
			_next = next;
			_tracer = tracer;
			_configurationReader = agent.ConfigurationReader;
			_logger = agent.Logger.Scoped(nameof(ApmMiddleware));
		}

		public async Task InvokeAsync(HttpContext context)
		{
			Transaction transaction;

			if (context.Request.Headers.ContainsKey(TraceParent.TraceParentHeaderName))
			{
				var headerValue = context.Request.Headers[TraceParent.TraceParentHeaderName].ToString();

				var distributedTracingData = TraceParent.TryExtractTraceparent(headerValue);
				if (distributedTracingData != null)
				{
					_logger.Debug()
						?.Log(
							"Incoming request with {TraceParentHeaderName} header. DistributedTracingData: {DistributedTracingData}. Continuing trace.",
							TraceParent.TraceParentHeaderName, distributedTracingData);

					transaction = _tracer.StartTransactionInternal(
						$"{context.Request.Method} {context.Request.Path}",
						ApiConstants.TypeRequest,
						distributedTracingData);
				}
				else
				{
					_logger.Debug()
						?.Log(
							"Incoming request with invalid {TraceParentHeaderName} header (received value: {TraceParentHeaderValue}). Starting trace with new trace id.",
							TraceParent.TraceParentHeaderName, headerValue);

					transaction = _tracer.StartTransactionInternal($"{context.Request.Method} {context.Request.Path}",
						ApiConstants.TypeRequest);
				}
			}
			else
			{
				_logger.Debug()?.Log("Incoming request. Starting Trace.");
				transaction = _tracer.StartTransactionInternal($"{context.Request.Method} {context.Request.Path}",
					ApiConstants.TypeRequest);
			}

			if (transaction.IsSampled) FillSampledTransactionContextRequest(context, transaction);

			try
			{
				await _next(context);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, transaction)) { }
			finally
			{
				//fixup Transaction.Name - e.g. /user/profile/1 -> /user/profile/{id}
				var routeData = (context.Features[typeof(IRoutingFeature)] as IRoutingFeature)?.RouteData;
				if (routeData != null)
				{
					var name = GetNameFromRouteContext(routeData.Values);

					if (!string.IsNullOrWhiteSpace(name)) transaction.Name = $"{context.Request.Method} {name}";
				}

				transaction.Result = Transaction.StatusCodeToResult(GetProtocolName(context.Request.Protocol), context.Response.StatusCode);

				if (transaction.IsSampled)
				{
					FillSampledTransactionContextResponse(context, transaction);
					FillSampledTransactionContextUser(context, transaction);
				}

				transaction.End();
			}
		}

		private string GetRawUrl(HttpRequest httpRequest)
		{
			var rawPathAndQuery = httpRequest.HttpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;
			return rawPathAndQuery == null ? null : UriHelper.BuildAbsolute(httpRequest.Scheme, httpRequest.Host, rawPathAndQuery);
		}

		private void FillSampledTransactionContextRequest(HttpContext context, Transaction transaction)
		{
			if (context.Request == null) return;

			var url = new Url
			{
				Full = context.Request.GetEncodedUrl(),
				HostName = context.Request.Host.Host,
				Protocol = GetProtocolName(context.Request.Protocol),
				Raw = GetRawUrl(context.Request) ?? context.Request.GetEncodedUrl(),
				PathName = context.Request.Path,
				Search = context.Request.QueryString.Value?.Skip(1)?.ToString()
			};

			Dictionary<string, string> requestHeaders = null;
			if (_configurationReader.CaptureHeaders)
			{
				requestHeaders = new Dictionary<string, string>();

				foreach (var header in context.Request.Headers)
					requestHeaders.Add(header.Key, header.Value.ToString());
			}

			transaction.Context.Request = new Request(context.Request.Method, url)
			{
				Socket = new Socket
				{
					Encrypted = context.Request.IsHttps,
					RemoteAddress = context.Connection?.RemoteIpAddress?.ToString()
				},
				HttpVersion = GetHttpVersion(context.Request.Protocol),
				Headers = requestHeaders
			};
		}

		private void FillSampledTransactionContextResponse(HttpContext context, Transaction transaction)
		{
			Dictionary<string, string> responseHeaders = null;

			if (_configurationReader.CaptureHeaders)
				responseHeaders = context.Response.Headers.ToDictionary(header => header.Key, header => header.Value.ToString());

			transaction.Context.Response = new Response
			{
				Finished = context.Response.HasStarted, //TODO ?
				StatusCode = context.Response.StatusCode,
				Headers = responseHeaders
			};
		}

		private void FillSampledTransactionContextUser(HttpContext context, Transaction transaction)
		{
			if (context.User?.Identity != null && context.User.Identity.IsAuthenticated && transaction.Context.User == null)
			{
				transaction.Context.User = new User
				{
					UserName = context.User.Identity.Name,
					Id = GetClaimWithFallbackValue(ClaimTypes.NameIdentifier, Consts.OpenIdClaimTypes.UserId),
					Email = GetClaimWithFallbackValue(ClaimTypes.Email, Consts.OpenIdClaimTypes.Email)
				};

				_logger.Debug()?.Log("Captured user - {CapturedUser}", transaction.Context.User);
			}

			string GetClaimWithFallbackValue(string claimType, string fallbackClaimType)
			{
				var idClaims = context.User.Claims.Where(n => n.Type == claimType || n.Type == fallbackClaimType);
				var enumerable = idClaims.ToList();
				return enumerable.Any() ? enumerable.First().Value : string.Empty;
			}
		}

		//credit: https://github.com/Microsoft/ApplicationInsights-aspnetcore
		private static string GetNameFromRouteContext(IDictionary<string, object> routeValues)
		{
			string name = null;

			if (routeValues.Count <= 0) return null;

			routeValues.TryGetValue("controller", out var controller);
			var controllerString = controller == null ? string.Empty : controller.ToString();

			if (!string.IsNullOrEmpty(controllerString))
			{
				name = controllerString;

				routeValues.TryGetValue("action", out var action);
				var actionString = action == null ? string.Empty : action.ToString();

				if (!string.IsNullOrEmpty(actionString)) name += "/" + actionString;

				if (routeValues.Keys.Count <= 2) return name;

				// Add parameters
				var sortedKeys = routeValues.Keys
					.Where(key =>
						!string.Equals(key, "controller", StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(key, "action", StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(key, "!__route_group", StringComparison.OrdinalIgnoreCase))
					.OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
					.ToArray();

				if (sortedKeys.Length <= 0) return name;

				var arguments = string.Join(@"/", sortedKeys);
				name += " {" + arguments + "}";
			}
			else
			{
				routeValues.TryGetValue("page", out var page);
				var pageString = page == null ? string.Empty : page.ToString();
				if (!string.IsNullOrEmpty(pageString)) name = pageString;
			}

			return name;
		}

		private static string GetProtocolName(string protocol)
		{
			switch (protocol)
			{
				case string s when string.IsNullOrEmpty(s):
					return string.Empty;
				case string s when s.StartsWith("HTTP", StringComparison.InvariantCulture): //in case of HTTP/2.x we only need HTTP
					return "HTTP";
				default:
					return protocol;
			}
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
	}
}
