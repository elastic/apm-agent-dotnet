using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.AspNet.WebApi.SelfHost.Extensions;
using Elastic.Apm.Config;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNet.WebApi.SelfHost.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

namespace Elastic.Apm.AspNet.WebApi.SelfHost
{
	internal class ApmMessageHandler : DelegatingHandler
	{
		private readonly Tracer _tracer;
		private readonly IApmLogger _logger;

		internal ApmMessageHandler(ApmAgent agent)
		{
			_tracer = agent.TracerInternal;
			_logger = agent.Logger.Scoped(nameof(ApmMessageHandler));
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			HttpResponseMessage response = null;

			var transaction = StartTransaction(request);
			if (transaction?.IsSampled ?? false) await FillSampledTransactionContextRequest(request, transaction);
			try
			{
				response = await base.SendAsync(request, cancellationToken);
			}
			catch (Exception e) when (transaction != null)
			{
				transaction.CaptureException(e);
				// It'd be nice to have this in an exception filter, but that would force us capturing the request body synchronously.
				// Therefore we rather unwind the stack in the catch block and call the async method.
				await transaction.CollectRequestBody(true, request, _logger);

				throw;
			}
			finally
			{
				// In case an error handler middleware is registered, the catch block above won't be executed, because the
				// error handler handles all the exceptions - in this case, based on the response code and the config, we may capture the body here
				if (NeedToCollectRequestBody(response, transaction)) await transaction.CollectRequestBody(true, request, _logger);

				StopTransaction(transaction, request, response);
			}

			return response;

			static bool NeedToCollectRequestBody(HttpResponseMessage response, Transaction transaction)
			{
				return response?.StatusCode != null && (transaction != null && transaction.IsContextCreated && (int)response.StatusCode >= 400
					&& transaction.Context?.Request?.Body is string body
					&& (string.IsNullOrEmpty(body) || body == Consts.Redacted));
			}
		}

		private void StopTransaction(Transaction transaction, HttpRequestMessage request, HttpResponseMessage response)
		{
			if (transaction == null) return;

			try
			{
				transaction.Result = Transaction.StatusCodeToResult(GetProtocolName(request.RequestUri.Scheme), (int)response.StatusCode);

				if (transaction.IsSampled)
				{
					FillSampledTransactionContextResponse(response, transaction);
					// todo: user is optional
					// FillSampledTransactionContextUser(context, transaction);
				}
			}
			catch (Exception ex)
			{
				_logger?.Error()?.LogException(ex, "Exception thrown while trying to stop transaction");
			}
			finally
			{
				transaction.End();
			}
		}

		private void FillSampledTransactionContextResponse(HttpResponseMessage response, Transaction transaction)
		{
			try
			{
				transaction.Context.Response = new Response
				{
					Finished = true, StatusCode = (int)response.StatusCode, Headers = GetHeaders(response.Headers, transaction.ConfigSnapshot)
				};
			}
			catch (Exception ex)
			{
				// context.response is optional: https://github.com/elastic/apm-server/blob/64a4ab96ba138050fe496b17d31deb2cf8830deb/docs/spec/context.json#L16
				_logger?.Error()
					?.LogException(ex, "Exception thrown while trying to fill response context for sampled transaction {TransactionId}",
						transaction.Id);
			}
		}

		private Transaction StartTransaction(HttpRequestMessage request)
		{
			try
			{
				Transaction transaction;
				var transactionName = $"{request.Method} {request.RequestUri.AbsolutePath}";

				if (request.Headers.Contains(TraceContext.TraceParentHeaderName)
					|| request.Headers.Contains(TraceContext.TraceParentHeaderName))
				{
					var headerValue = request.Headers.Contains(TraceContext.TraceParentHeaderName)
                    						? request.Headers.GetValues(TraceContext.TraceParentHeaderName).First()
											: request.Headers.GetValues(TraceContext.TraceParentHeaderNamePrefixed).First();

					var tracingData = request.Headers.Contains(TraceContext.TraceStateHeaderName)
						? TraceContext.TryExtractTracingData(headerValue, request.Headers.GetValues(TraceContext.TraceStateHeaderName).First())
						: TraceContext.TryExtractTracingData(headerValue);

					if (tracingData != null)
					{
						_logger.Debug()
							?.Log(
								"Incoming request with {TraceParentHeaderName} header. DistributedTracingData: {DistributedTracingData}. Continuing trace.",
								TraceContext.TraceParentHeaderName, tracingData);

						transaction = _tracer.StartTransactionInternal(
							transactionName,
							ApiConstants.TypeRequest,
							tracingData);
					}
					else
					{
						_logger.Debug()
							?.Log(
								"Incoming request with invalid {TraceParentHeaderName} header (received value: {TraceParentHeaderValue}). Starting trace with new trace id.",
								TraceContext.TraceParentHeaderName, headerValue);

						transaction = _tracer.StartTransactionInternal(transactionName,
							ApiConstants.TypeRequest);
					}
				}
				else
				{
					_logger.Debug()?.Log("Incoming request. Starting Trace.");
					transaction = _tracer.StartTransactionInternal(transactionName,
						ApiConstants.TypeRequest);
				}

				return transaction;
			}
			catch (Exception ex)
			{
				_logger?.Error()?.LogException(ex, "Exception thrown while trying to start transaction");
				return null;
			}
		}

		private async Task FillSampledTransactionContextRequest(HttpRequestMessage request, Transaction transaction)
		{
			try
			{
				if (request == null) return;

				var absoluteUrl = request.RequestUri.AbsoluteUri;

				var url = new Url
				{
					Full = absoluteUrl,
					HostName = request.RequestUri.Host,
					Protocol = GetProtocolName(request.RequestUri.Scheme),
					Raw = absoluteUrl,
					//todo: check
					PathName = request.RequestUri.LocalPath,
					Search = request.RequestUri.Query.Length > 0 ? request.RequestUri.Query.Substring(1) : string.Empty
				};

				transaction.Context.Request = new Request(request.Method.ToString(), url)
				{
					Socket = new Socket { Encrypted = string.Equals(request.RequestUri.Scheme, "https", StringComparison.OrdinalIgnoreCase) },
					//todo: check
					HttpVersion = GetHttpVersion(request.Version.ToString()),
					Headers = GetHeaders(request.Headers, transaction.ConfigSnapshot)
				};

				await transaction.CollectRequestBody(false, request, _logger);
			}
			catch (Exception ex)
			{
				// context.request is optional: https://github.com/elastic/apm-server/blob/64a4ab96ba138050fe496b17d31deb2cf8830deb/docs/spec/request.json#L5
				_logger?.Error()
					?.LogException(ex, "Exception thrown while trying to fill request context for sampled transaction {TransactionId}",
						transaction.Id);
			}
		}

		[SuppressMessage("ReSharper", "PatternAlwaysOfType")]
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
				case null:
					return "unknown";
				default:
					return protocolString.Replace("HTTP/", string.Empty);
			}
		}

		private Dictionary<string, string> GetHeaders(HttpHeaders headers, IConfigSnapshot configSnapshot) =>
			configSnapshot.CaptureHeaders && headers != null
				? headers.ToDictionary(header => header.Key, header => string.Join(", ", header.Value))
				: null;
	}
}
