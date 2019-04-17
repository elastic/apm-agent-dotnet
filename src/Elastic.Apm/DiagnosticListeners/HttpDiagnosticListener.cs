using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.DiagnosticListeners
{
	/// <inheritdoc />
	/// <summary>
	/// Captures web requests initiated by <see cref="T:System.Net.Http.HttpClient" />
	/// </summary>
	internal class HttpDiagnosticListener : IDiagnosticListener
	{
		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		internal readonly ConcurrentDictionary<HttpRequestMessage, Span> ProcessingRequests = new ConcurrentDictionary<HttpRequestMessage, Span>();

		public HttpDiagnosticListener(IApmAgent components) =>
			(_logger, _configurationReader) = (components.Logger?.Scoped(nameof(HttpDiagnosticListener)), components.ConfigurationReader);

		private readonly ScopedLogger _logger;
		private readonly IConfigurationReader _configurationReader;

		public string Name => "HttpHandlerDiagnosticListener"; // "HttpHandlerDiagnosticListener" for .NET Core, "System.Net.Http.Desktop" for Full .NET Framework

		public void OnCompleted() { }

		public void OnError(Exception error) => _logger.Error()?.LogExceptionWithCaller(error, nameof(OnError));

		public void OnNext(KeyValuePair<string, object> kv)
		{
			if (kv.Value == null || string.IsNullOrEmpty(kv.Key)) return;

			if (!(kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request")?.GetValue(kv.Value) is HttpRequestMessage request)) return;

			if (IsRequestFiltered(request?.RequestUri)) return;

			switch (kv.Key)
			{
				case "System.Net.Http.Exception":
					_logger.Debug()?.Log("System.Net.Http.Exception - {url}", request.RequestUri);
					var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Exception").GetValue(kv.Value) as Exception;
					var transaction = Agent.TransactionContainer.Transactions?.Value;

					transaction?.CaptureException(exception, "Failed outgoing HTTP request");
					//TODO: we don't know if exception is handled, currently reports handled = false
					break;
				case "System.Net.Http.HttpRequestOut.Start": //TODO: look for consts
					_logger.Debug()?.Log("System.Net.Http.Start - {url}", request.RequestUri);
					if (Agent.TransactionContainer.Transactions == null || Agent.TransactionContainer.Transactions.Value == null)
					{
						_logger.Debug()?.Log("No active transaction, skip creating span for outgoing HTTP request");
						return;
					}

					transaction = Agent.TransactionContainer.Transactions.Value;

					var span = transaction.StartSpanInternal($"{request?.Method} {request?.RequestUri?.Host}", ApiConstants.TypeExternal,
						ApiConstants.SubtypeHttp);

					if (ProcessingRequests.TryAdd(request, span))
					{
						if(!request.Headers.Contains(TraceParent.TraceParentHeaderName))
							// We call TraceParent.BuildTraceparent explicitly instead of DistributedTracingData.SerializeToString because
							// in the future we might change DistributedTracingData.SerializeToString to use some other internal format
							// but here we want the string to be in W3C 'traceparent' header format.
							request.Headers.Add(TraceParent.TraceParentHeaderName, TraceParent.BuildTraceparent(span.OutgoingDistributedTracingData));

						span.Context.Http = new Http
						{
							Url = request?.RequestUri?.ToString(),
							Method = request?.Method?.Method
						};

						var frames = new StackTrace(true).GetFrames();
						var stackFrames = StacktraceHelper.GenerateApmStackTrace(frames, _logger, span.Name);
						span.StackTrace = stackFrames;
					}
					break;

				case "System.Net.Http.HttpRequestOut.Stop":
					_logger.Debug()?.Log("System.Net.Http.Stop - {url}", request.RequestUri);
					var response = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Response").GetValue(kv.Value) as HttpResponseMessage;

					if (ProcessingRequests.TryRemove(request, out var mspan))
					{
						//TODO: response can be null if for example the request Task is Faulted.
						//E.g. writing this from an airplane without internet, and requestTaskStatus is "Faulted" and response is null
						//How do we report this? There is no response code in that case.
						if (response != null) mspan.Context.Http.StatusCode = (int)response.StatusCode;

						mspan.End();
					}
					else
					{
						const string message = "Failed capturing request '{HttpMethod} {Url}' in System.Net.Http.HttpRequestOut.Stop. This Span will be skipped in case it wasn't captured before.";
						var url = request?.RequestUri?.AbsoluteUri;
						var method = request?.Method?.Method;
						_logger?.Warning()?.Log(message, method, url);
					}
					break;
			}
		}

		/// <summary>
		/// Tells if the given request should be filtered from being captured.
		/// </summary>
		/// <returns><c>true</c>, if request should not be captured, <c>false</c> otherwise.</returns>
		/// <param name="requestUri">Request URI. Can be null, which is not filtered</param>
		private bool IsRequestFiltered(Uri requestUri)
		{
			switch (requestUri)
			{
				case Uri uri when uri == null: return true;
				case Uri uri when _configurationReader.ServerUrls.Any(n => n.IsBaseOf(uri)): //TODO: measure the perf of this!
					return true;
				default:
					return false;
			}
		}
	}
}
