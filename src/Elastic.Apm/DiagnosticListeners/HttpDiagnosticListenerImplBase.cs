using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Logging;

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
		protected const string EventRequestPropertyName = "Request";
		private const string EventResponsePropertyName = "Response";

		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		internal readonly ConcurrentDictionary<TRequest, ISpan> ProcessingRequests = new ConcurrentDictionary<TRequest, ISpan>();

		private readonly IApmAgent _agent;
		protected readonly ScopedLogger Logger;

		protected HttpDiagnosticListenerImplBase(IApmAgent agent)
		{
			_agent = agent;
			Logger = _agent.Logger?.Scoped("HttpDiagnosticListenerImplBase");
		}

		protected abstract string RequestGetMethod(TRequest request);

		protected abstract Uri RequestGetUri(TRequest request);

		protected abstract void RequestHeadersAdd(TRequest request, string headerName, string headerValue);

		protected abstract bool RequestHeadersContain(TRequest request, string headerName);

		protected abstract int ResponseGetStatusCode(TResponse response);

		internal abstract string ExceptionEventKey { get; }

		public abstract string Name { get; }
		internal abstract string StartEventKey { get; }
		internal abstract string StopEventKey { get; }

		public void OnCompleted() { }

		public void OnError(Exception error) => Logger.Error()?.LogExceptionWithCaller(error, nameof(OnError));

		public void OnNext(KeyValuePair<string, object> kv)
		{
			Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}', value: `{DiagnosticEventValue}'", kv.Key, kv.Value);

			if (string.IsNullOrEmpty(kv.Key))
			{
				Logger.Trace()?.Log($"Key is {(kv.Key == null ? "null" : "an empty string")} - exiting");
				return;
			}

			if (kv.Value == null)
			{
				Logger.Trace()?.Log("Value is null - exiting");
				return;
			}

			var requestObject = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty(EventRequestPropertyName)?.GetValue(kv.Value);
			if (requestObject == null)
			{
				Logger.Trace()?.Log("Event's {EventRequestPropertyName} property is null - exiting", EventRequestPropertyName);
				return;
			}

			if (!(requestObject is TRequest request))
			{
				Logger.Trace()
					?.Log("Actual type of object ({EventRequestPropertyActualType}) in event's {EventRequestPropertyName} property " +
						"doesn't match the expected type ({EventRequestPropertyExpectedType}) - exiting",
						requestObject.GetType().FullName, EventRequestPropertyName, typeof(TRequest).FullName);
				return;
			}

			var requestUrl = RequestGetUri(request);
			if (requestUrl == null)
			{
				Logger.Trace()?.Log("Request URL is null - exiting", EventRequestPropertyName);
				return;
			}

			if (IsRequestFilteredOut(requestUrl))
			{
				Logger.Trace()?.Log("Request URL ({RequestUrl}) is filtered out - exiting", requestUrl);
				return;
			}

			if (kv.Key.Equals(StartEventKey))
				ProcessStartEvent(request, requestUrl);
			else if (kv.Key.Equals(StopEventKey))
				ProcessStopEvent(kv.Value, request, requestUrl);
			else if (kv.Key.Equals(ExceptionEventKey))
				ProcessExceptionEvent(kv.Value, requestUrl);
			else
				Logger.Trace()?.Log("Unrecognized key `{DiagnosticEventKey}'", kv.Key);
		}

		private void ProcessStartEvent(TRequest request, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing start event... Request URL: {RequestUrl}", requestUrl);

			var transaction = _agent.Tracer.CurrentTransaction;
			if (transaction == null)
			{
				Logger.Debug()?.Log("No current transaction, skip creating span for outgoing HTTP request");
				return;
			}

			var currentExecutionSegment = _agent.Tracer.CurrentSpan ?? (IExecutionSegment)transaction;
			var span = currentExecutionSegment.StartSpan(
				$"{RequestGetMethod(request)} {requestUrl.Host}",
				ApiConstants.TypeExternal,
				ApiConstants.SubtypeHttp);

			if (!ProcessingRequests.TryAdd(request, span))
			{
				// Consider improving error reporting - see https://github.com/elastic/apm-agent-dotnet/issues/280
				Logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
				return;
			}

			if (!RequestHeadersContain(request, TraceParent.TraceParentHeaderName))
				// We call TraceParent.BuildTraceparent explicitly instead of DistributedTracingData.SerializeToString because
				// in the future we might change DistributedTracingData.SerializeToString to use some other internal format
				// but here we want the string to be in W3C 'traceparent' header format.
				RequestHeadersAdd(request, TraceParent.TraceParentHeaderName, TraceParent.BuildTraceparent(span.OutgoingDistributedTracingData));

			if (transaction.IsSampled) span.Context.Http = new Http { Url = requestUrl.ToString(), Method = RequestGetMethod(request) };
		}

		private void ProcessStopEvent(object eventValue, TRequest request, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing stop event... Request URL: {RequestUrl}", requestUrl);

			if (!ProcessingRequests.TryRemove(request, out var span))
			{
				Logger.Warning()
					?.Log("Failed capturing request (failed to remove from ProcessingRequests) - " +
						"This Span will be skipped in case it wasn't captured before. " +
						"Request: method: {HttpMethod}, URL: {RequestUrl}", RequestGetMethod(request), requestUrl);
				return;
			}

			// if span.Context.Http == null that means the transaction is not sampled (see ProcessStartEvent)
			if (span.Context.Http != null)
			{
				//TODO: response can be null if for example the request Task is Faulted.
				//E.g. writing this from an airplane without internet, and requestTaskStatus is "Faulted" and response is null
				//How do we report this? There is no response code in that case.
				var responseObject = eventValue.GetType().GetTypeInfo().GetDeclaredProperty(EventResponsePropertyName)?.GetValue(eventValue);
				if (responseObject != null)
				{
					if (responseObject is TResponse response)
						span.Context.Http.StatusCode = ResponseGetStatusCode(response);
					else
					{
						Logger.Trace()
							?.Log("Actual type of object ({EventResponsePropertyActualType}) in event's {EventResponsePropertyName} property " +
								"doesn't match the expected type ({EventResponsePropertyExpectedType})",
								responseObject.GetType().FullName, EventResponsePropertyName, typeof(TResponse).FullName);
					}
				}
			}

			span.End();
		}

		protected virtual void ProcessExceptionEvent(object eventValue, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing exception event... Request URL: {RequestUrl}", requestUrl);

			if (!(eventValue.GetType().GetTypeInfo().GetDeclaredProperty(EventExceptionPropertyName)?.GetValue(eventValue) is Exception exception))
			{
				Logger.Trace()?.Log("Failed reading exception property");
				return;
			}

			var transaction = _agent.Tracer.CurrentTransaction;

			transaction?.CaptureException(exception, "Failed outgoing HTTP request");
			//TODO: we don't know if exception is handled, currently reports handled = false
		}

		/// <summary>
		/// Tells if the given request should be filtered from being captured.
		/// </summary>
		/// <returns><c>true</c>, if request should not be captured, <c>false</c> otherwise.</returns>
		/// <param name="requestUri">Request URI. It cannot be null</param>
		private bool IsRequestFilteredOut(Uri requestUri) => _agent.ConfigurationReader.ServerUrls.Any(n => n.IsBaseOf(requestUri));
	}
}
