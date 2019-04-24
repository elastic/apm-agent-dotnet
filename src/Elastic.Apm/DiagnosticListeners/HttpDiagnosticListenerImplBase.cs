using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
	internal abstract class HttpDiagnosticListenerImplBase<TRequest, TResponse> : IDiagnosticListener
		where TRequest : class
		where TResponse : class
	{
		private const string EventExceptionPropertyName = "Exception";
		private const string EventRequestPropertyName = "Request";
		private const string EventResponsePropertyName = "Response";

		private readonly IConfigurationReader _configurationReader;

		private readonly ScopedLogger _logger;

		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		internal readonly ConcurrentDictionary<TRequest, Span> ProcessingRequests = new ConcurrentDictionary<TRequest, Span>();

		protected HttpDiagnosticListenerImplBase(IApmAgent components)
		{
			_logger = components.Logger?.Scoped("HttpDiagnosticListenerImplBase");
			_configurationReader = components.ConfigurationReader;
		}

		protected abstract string RequestGetMethod(TRequest request);

		protected abstract Uri RequestGetUri(TRequest request);

		protected abstract void RequestHeadersAdd(TRequest request, string headerName, string headerValue);

		protected abstract bool RequestHeadersContains(TRequest request, string headerName);

		protected abstract int ResponseGetStatusCode(TResponse response);

		public abstract string Name { get; }

		internal abstract string ExceptionEventKey { get; }
		internal abstract string StartEventKey { get; }
		internal abstract string StopEventKey { get; }

		public void OnCompleted() { }

		public void OnError(Exception error) => _logger.Error()?.LogExceptionWithCaller(error, nameof(OnError));

		public void OnNext(KeyValuePair<string, object> kv)
		{
			_logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}', value: `{DiagnosticEventValue}'", kv.Key, kv.Value);

			if (string.IsNullOrEmpty(kv.Key))
			{
				_logger.Trace()?.Log($"Key is {(kv.Key == null ? "null" : "an empty string")} - exiting");
				return;
			}

			if (kv.Value == null)
			{
				_logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty(EventRequestPropertyName)?.GetValue(kv.Value);
			if (requestObject == null)
			{
				_logger.Trace()?.Log("Event's {EventRequestPropertyName} property is null - exiting", EventRequestPropertyName);
				return;
			}

			var request = requestObject as TRequest;
			if (request == null)
			{
				_logger.Trace()
					?.Log("Actual type of object ({EventRequestPropertyActualType}) in event's {EventRequestPropertyName} property " +
						"doesn't match the expected type ({EventRequestPropertyExpectedType}) - exiting",
						requestObject.GetType().FullName, EventRequestPropertyName, typeof(TRequest).FullName);
				return;
			}

			var requestUrl = RequestGetUri(request);
			if (requestUrl == null)
			{
				_logger.Trace()?.Log("Request URL is null - exiting", EventRequestPropertyName);
				return;
			}

			if (IsRequestFilteredOut(requestUrl))
			{
				_logger.Trace()?.Log("Request URL ({RequestUrl}) is filtered out - exiting", requestUrl);
				return;
			}

			if (kv.Key.Equals(StartEventKey))
				ProcessStartEvent(kv.Value, request, requestUrl);
			else if (kv.Key.Equals(StopEventKey))
				ProcessStopEvent(kv.Value, request, requestUrl);
			else if (kv.Key.Equals(ExceptionEventKey))
				ProcessExceptionEvent(kv.Value, request, requestUrl);
			else
				_logger.Error()?.Log("Unrecognized key `{DiagnosticEventKey}'", kv.Key);
		}

		private void ProcessStartEvent(object eventValue, TRequest request, Uri requestUrl)
		{
			_logger.Trace()?.Log("Processing start event - {RequestUrl}...", requestUrl);
			if (Agent.TransactionContainer.Transactions == null || Agent.TransactionContainer.Transactions.Value == null)
			{
				_logger.Debug()?.Log("No active transaction, skip creating span for outgoing HTTP request");
				return;
			}

			var transaction = Agent.TransactionContainer.Transactions.Value;

			var span = transaction.StartSpanInternal(
				$"{RequestGetMethod(request)} {requestUrl.Host}",
				ApiConstants.TypeExternal,
				ApiConstants.SubtypeHttp);

			if (!ProcessingRequests.TryAdd(request, span))
			{
				// Sergey_Kleyman_TODO: Implement error handling
				_logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
				return;
			}

			if (!RequestHeadersContains(request, TraceParent.TraceParentHeaderName))
				// We call TraceParent.BuildTraceparent explicitly instead of DistributedTracingData.SerializeToString because
				// in the future we might change DistributedTracingData.SerializeToString to use some other internal format
				// but here we want the string to be in W3C 'traceparent' header format.
				RequestHeadersAdd(request, TraceParent.TraceParentHeaderName, TraceParent.BuildTraceparent(span.OutgoingDistributedTracingData));

			if (transaction.IsSampled)
			{
				span.Context.Http = new Http
				{
					Url = requestUrl.ToString(),
					Method = RequestGetMethod(request)
				};

				var frames = new StackTrace(true).GetFrames();
				var stackFrames = StacktraceHelper.GenerateApmStackTrace(frames, _logger, span.Name);
				span.StackTrace = stackFrames;
			}
		}

		private void ProcessStopEvent(object eventValue, TRequest request, Uri requestUrl)
		{
			_logger.Trace()?.Log("Processing stop event - {RequestUrl}...", requestUrl);

			if (!ProcessingRequests.TryRemove(request, out var span))
			{
				_logger.Warning()
					?.Log("Failed capturing request (failed to remove from ProcessingRequests) - " +
						"This Span will be skipped in case it wasn't captured before. " +
						"Request: method: {HttpMethod}, URL: {RequestUrl}", RequestGetMethod(request), requestUrl);
				return;
			}

			//TODO: response can be null if for example the request Task is Faulted.
			//E.g. writing this from an airplane without internet, and requestTaskStatus is "Faulted" and response is null
			//How do we report this? There is no response code in that case.
			var responseObject = eventValue.GetType().GetTypeInfo().GetDeclaredProperty(EventResponsePropertyName)?.GetValue(eventValue);
			if (responseObject != null)
			{
				var response = responseObject as TResponse;
				if (response == null)
				{
					_logger.Trace()
						?.Log("Actual type of object ({EventResponsePropertyActualType}) in event's {EventResponsePropertyName} property " +
							"doesn't match the expected type ({EventResponsePropertyExpectedType})",
							responseObject.GetType().FullName, EventResponsePropertyName, typeof(TResponse).FullName);
				}
				else
					span.Context.Http.StatusCode = ResponseGetStatusCode(response);
			}

			span.End();
		}

		private void ProcessExceptionEvent(object eventValue, TRequest request, Uri requestUrl)
		{
			_logger.Trace()?.Log("Processing exception event - {RequestUrl}...", requestUrl);
			var exception = eventValue.GetType().GetTypeInfo().GetDeclaredProperty(EventExceptionPropertyName).GetValue(eventValue) as Exception;
			var transaction = Agent.TransactionContainer.Transactions?.Value;

			transaction?.CaptureException(exception, "Failed outgoing HTTP request");
			//TODO: we don't know if exception is handled, currently reports handled = false
		}

		/// <summary>
		/// Tells if the given request should be filtered from being captured.
		/// </summary>
		/// <returns><c>true</c>, if request should not be captured, <c>false</c> otherwise.</returns>
		/// <param name="requestUri">Request URI. It cannot be null</param>
		private bool IsRequestFilteredOut(Uri requestUri) => _configurationReader.ServerUrls.Any(n => n.IsBaseOf(requestUri));
	}
}
