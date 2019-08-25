using System;
using System.Collections.Generic;
using System.Net.Http;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticListeners
{
	internal class HttpDiagnosticListenerCoreImpl : HttpDiagnosticListenerImplBase
	{
		internal const string ExceptionEventKey = "System.Net.Http.Exception";
		internal const string StartEventKey = "System.Net.Http.HttpRequestOut.Start";
		internal const string StopEventKey = "System.Net.Http.HttpRequestOut.Stop";

		private readonly ScopedLogger _logger;

		public HttpDiagnosticListenerCoreImpl(IApmAgent agent)
			: base(agent) =>
			_logger = agent.Logger?.Scoped(nameof(HttpDiagnosticListenerCoreImpl));

		public override string Name => "HttpHandlerDiagnosticListener";

		protected override bool DispatchEventProcessing(KeyValuePair<string, object> kv)
		{
			if (kv.Key.Equals(StartEventKey, StringComparison.Ordinal))
			{
				ProcessStartStopEvent(new EventData(kv), /* isStart */ true);
				return true;
			}

			if (kv.Key.Equals(StopEventKey, StringComparison.Ordinal))
			{
				ProcessStartStopEvent(new EventData(kv), /* isStart */ false);
				return true;
			}

			// ReSharper disable once InvertIf
			if (kv.Key.Equals(ExceptionEventKey, StringComparison.Ordinal))
			{
				ProcessExceptionEvent(new EventData(kv));
				return true;
			}

			return false;
		}

		private void ProcessExceptionEvent(EventData eventData)
		{
			_logger.Trace()?.Log("Processing exception event... Request URL: {RequestUrl}", eventData.Url);

			if (IsRequestFilteredOut(eventData.Url))
			{
				_logger.Trace()?.Log("Request URL ({RequestUrl}) is filtered out - exiting", eventData.Url);
				return;
			}

			var exception = ExtractProperty<Exception>(EventExceptionPropertyName, eventData.EventKeyValue);

			if (!ProcessingRequests.TryGetValue(eventData.Request, out var span))
			{
				_logger.Warning()
					?.Log("Failed to get from ProcessingRequests - " +
						"it might be because Start event was not captured successfully." +
						" Request: method: {HttpMethod}; URL: {RequestUrl}; exception in event: {Exception}",
						eventData.Method, eventData.Url, exception);
				return;
			}

			//TODO: we don't know if exception is handled, currently reports handled = false
			span?.CaptureException(exception, "Failed outgoing HTTP request");
		}

		private class EventData : IEventData
		{
			private readonly HttpRequestMessage _request;

			internal EventData(KeyValuePair<string, object> kv)
			{
				EventKeyValue = kv;
				_request = ExtractProperty<HttpRequestMessage>(EventRequestPropertyName, EventKeyValue);
			}

			internal KeyValuePair<string, object> EventKeyValue { get; }

			public string Method => _request.Method.Method;

			public object Request => _request;

			public int? StatusCode
			{
				get
				{
					var responsePropertyValue = ExtractProperty<HttpResponseMessage>(EventResponsePropertyName, EventKeyValue);
					return (int?)responsePropertyValue?.StatusCode;
				}
			}

			public Uri Url => _request.RequestUri;

			public bool ContainsRequestHeader(string headerName)
				=> _request.Headers.Contains(headerName);

			public void AddRequestHeader(string headerName, string headerValue) =>
				_request.Headers.Add(headerName, headerValue);

			public override string ToString() => new ToStringBuilder($"{nameof(HttpDiagnosticListenerCoreImpl)}.{nameof(EventData)}")
			{
				{ "Event key/value", EventKeyValue }
			}.ToString();
		}
	}
}
