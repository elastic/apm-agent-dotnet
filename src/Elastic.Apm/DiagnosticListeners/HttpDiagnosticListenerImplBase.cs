// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Api;
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
	internal abstract class HttpDiagnosticListenerImplBase<TRequest, TResponse> : DiagnosticListenerBase
		where TRequest : class
		where TResponse : class
	{
		private const string EventExceptionPropertyName = "Exception";
		protected const string EventRequestPropertyName = "Request";
		private const string EventResponsePropertyName = "Response";

		/// <summary>
		/// Keeps track of ongoing requests
		/// </summary>
		internal readonly ConcurrentDictionary<TRequest, ISpan> ProcessingRequests = new();

		private readonly HttpTraceConfiguration _configuration;
		private readonly ApmAgent _realAgent;

		protected HttpDiagnosticListenerImplBase(IApmAgent agent) : base(agent)
		{
			_realAgent = agent as ApmAgent;
			_configuration = _realAgent?.HttpTraceConfiguration;
		}

		protected abstract string RequestGetMethod(TRequest request);

		protected abstract Uri RequestGetUri(TRequest request);

		protected abstract void RequestHeadersAdd(TRequest request, string headerName, string headerValue);

		protected abstract bool RequestHeadersContain(TRequest request, string headerName);

		protected abstract bool RequestTryGetHeader(TRequest request, string headerName, out string value);

		protected abstract int ResponseGetStatusCode(TResponse response);

		protected abstract string ExceptionEventKey { get; }

		internal abstract string StartEventKey { get; }
		internal abstract string StopEventKey { get; }

		public override void OnError(Exception error) => Logger.Error()?.LogExceptionWithCaller(error, nameof(OnError));

		protected override void HandleOnNext(KeyValuePair<string, object> kv)
		{
			try
			{
				// We only print the key, we don't print the value, because it can contain a http request object, and its ToString prints
				// the URL, which can contain username and password. See: https://github.com/elastic/apm-agent-dotnet/issues/515
				Logger.Trace()?.Log("Called with key: `{DiagnosticEventKey}'", kv.Key);

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
					Logger.Trace()?.Log("Request URL ({RequestUrl}) is filtered out - exiting", requestUrl.Sanitize());
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
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "Exception during capturing outgoing HTTP request");
			}
		}

		private void ProcessStartEvent(TRequest request, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing start event... Request URL: {RequestUrl}", requestUrl.Sanitize());

			var transaction = ApmAgent.Tracer.CurrentTransaction;
			if (transaction is null)
			{
				Logger.Debug()?.Log("No current transaction, skip creating span for outgoing HTTP request");
				return;
			}

			if (_realAgent?.TracerInternal.CurrentSpan is Span currentSpan && currentSpan.IsExitSpan)
			{
				PropagateTraceContext(request, transaction, currentSpan);
				return;
			}

			var method = RequestGetMethod(request);
			ISpan span = null;
			var suppressSpanCreation = false;
			if (_configuration?.HasTracers ?? false)
			{
				using (var httpTracers = _configuration.GetTracers())
				{
					foreach (var httpSpanTracer in httpTracers)
					{
						suppressSpanCreation = httpSpanTracer.ShouldSuppressSpanCreation();
						if (suppressSpanCreation)
							break;

						if (httpSpanTracer.IsMatch(method, requestUrl,
								header => RequestTryGetHeader(request, header, out var value) ? value : null))
						{
							span = httpSpanTracer.StartSpan(ApmAgent, method, requestUrl,
								header => RequestTryGetHeader(request, header, out var value) ? value : null);
							if (span != null)
								break;
						}
					}
				}
			}

			if (span is null)
			{
				if (_configuration?.CaptureSpan ?? false)
				{
					if (suppressSpanCreation)
					{
						Logger.Trace()
							?.Log("Skip creating span for outgoing HTTP request to {RequestUrl} as it was suppressed by an HttpSpanTracer",
								requestUrl.Sanitize());
						return;
					}

					span = ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(ApmAgent, $"{method} {requestUrl.Host}",
						ApiConstants.TypeExternal, ApiConstants.SubtypeHttp, InstrumentationFlag.HttpClient, true, true);

					if (span is null)
					{
						Logger.Trace()?.Log("Could not create span for outgoing HTTP request to {RequestUrl}", requestUrl.Sanitize());
						return;
					}
				}
				else
				{
					Logger.Trace()
						?.Log("Skip creating span for outgoing HTTP request to {RequestUrl} as not to known service", requestUrl.Sanitize());
					return;
				}
			}

			if (!ProcessingRequests.TryAdd(request, span))
			{
				// Consider improving error reporting - see https://github.com/elastic/apm-agent-dotnet/issues/280
				Logger.Error()?.Log("Failed to add to ProcessingRequests - ???");
				return;
			}

			PropagateTraceContext(request, transaction, span);

			if (span is Span { ShouldBeSentToApmServer: false } realSpan)
			{
				var type = !string.IsNullOrEmpty(realSpan.Subtype) ? realSpan.Subtype : realSpan.Type;
				var target = new Target(type, UrlUtils.ExtractService(requestUrl, realSpan));
				realSpan.DroppedSpanStatCache = new Span.DroppedSpanStatCacheStruct(target, target.ToDestinationServiceResource());
				return;
			}

			span.Context.Http = new Http { Method = method };
			span.Context.Http.SetUrl(requestUrl);
		}

		private void PropagateTraceContext(TRequest request, ITransaction transaction, ISpan span)
		{
			if (!RequestHeadersContain(request, TraceContext.TraceParentHeaderName))
				// We call TraceParent.BuildTraceparent explicitly instead of DistributedTracingData.SerializeToString because
				// in the future we might change DistributedTracingData.SerializeToString to use some other internal format
				// but here we want the string to be in W3C 'traceparent' header format.
				RequestHeadersAdd(request, TraceContext.TraceParentHeaderName, TraceContext.BuildTraceparent(span.OutgoingDistributedTracingData));

			if (transaction is Transaction t)
			{
				if (t.Configuration.UseElasticTraceparentHeader)
				{
					if (!RequestHeadersContain(request, TraceContext.TraceParentHeaderNamePrefixed))
					{
						RequestHeadersAdd(request, TraceContext.TraceParentHeaderNamePrefixed,
							TraceContext.BuildTraceparent(span.OutgoingDistributedTracingData));
					}
				}
			}

			if (!RequestHeadersContain(request, TraceContext.TraceStateHeaderName) && span.OutgoingDistributedTracingData != null
				&& span.OutgoingDistributedTracingData.HasTraceState)
				RequestHeadersAdd(request, TraceContext.TraceStateHeaderName, span.OutgoingDistributedTracingData.TraceState.ToTextHeader());
		}

		private void ProcessStopEvent(object eventValue, TRequest request, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing stop event... Request URL: {RequestUrl}", requestUrl.Sanitize().ToString());

			if (!ProcessingRequests.TryRemove(request, out var span))
			{
				// if we don't find the request in the dictionary and current transaction is null, then this is not a big deal -
				// it was probably not captured in Start either - so we skip with a debug log
				if (ApmAgent.Tracer.CurrentTransaction is null)
				{
					Logger.Debug()
						?.Log("{eventName} called with no active current transaction, url: {url} - skipping event", nameof(ProcessStopEvent),
							requestUrl.Sanitize().ToString());
				}
				else
				{
					Logger.Debug()
						?.Log("Could not remove request from processing requests. This likely means it was not captured to begin with." +
							"Request: method: {HttpMethod}, URL: {RequestUrl}", RequestGetMethod(request), requestUrl.Sanitize().ToString());
				}

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
					{
						span.Context.Http.StatusCode = ResponseGetStatusCode(response);
						SetOutcome(span, span.Context.Http.StatusCode);
					}
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

		internal static void SetOutcome(ISpan span, int statusCode) =>
			span.Outcome = statusCode >= 400 || statusCode < 100 ? Outcome.Failure : Outcome.Success;

		protected virtual void ProcessExceptionEvent(object eventValue, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing exception event... Request URL: {RequestUrl}", requestUrl.Sanitize().ToString());

			if (!(eventValue.GetType().GetTypeInfo().GetDeclaredProperty(EventExceptionPropertyName)?.GetValue(eventValue) is Exception exception))
			{
				Logger.Trace()?.Log("Failed reading exception property");
				return;
			}

			var transaction = ApmAgent.Tracer.CurrentTransaction;

			transaction?.CaptureException(exception, "Failed outgoing HTTP request");
			//TODO: we don't know if exception is handled, currently reports handled = false
		}

		/// <summary>
		/// Tells if the given request should be filtered from being captured.
		/// </summary>
		/// <returns><c>true</c>, if request should not be captured, <c>false</c> otherwise.</returns>
		/// <param name="requestUri">Request URI. It cannot be null</param>
		private bool IsRequestFilteredOut(Uri requestUri) => ApmAgent.ConfigurationReader.ServerUrl.IsBaseOf(requestUri);
	}
}
