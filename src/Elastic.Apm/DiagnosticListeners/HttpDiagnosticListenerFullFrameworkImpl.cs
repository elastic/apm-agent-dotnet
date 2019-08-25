using System;
using System.Collections.Generic;
using System.Net;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticListeners
{
	internal class HttpDiagnosticListenerFullFrameworkImpl : HttpDiagnosticListenerImplBase
	{
		internal const string InitializationFailedEventKey = "System.Net.Http.InitializationFailed";
		internal const string InitializationFailedExceptionPropertyName = "Exception";
		internal const string StartEventKey = "System.Net.Http.Desktop.HttpRequestOut.Start";
		internal const string StopEventKey = "System.Net.Http.Desktop.HttpRequestOut.Stop";
		internal const string StopExEventKey = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";
		internal const string StopExStatusCodePropertyName = "StatusCode";

		private readonly ScopedLogger _logger;

		public HttpDiagnosticListenerFullFrameworkImpl(IApmAgent agent)
			: base(agent) =>
			_logger = agent.Logger?.Scoped(nameof(HttpDiagnosticListenerFullFrameworkImpl));

		public override string Name => "System.Net.Http.Desktop";

		protected override bool DispatchEventProcessing(KeyValuePair<string, object> kv)
		{
			if (kv.Key.Equals(StartEventKey, StringComparison.Ordinal))
			{
				ProcessStartStopEvent(new EventData(kv), /* isStart */ true);
				return true;
			}

			var isStop = kv.Key.Equals(StopEventKey, StringComparison.Ordinal);
			var isStopEx = false;
			if (!isStop) isStopEx = kv.Key.Equals(StopExEventKey, StringComparison.Ordinal);
			if (isStop || isStopEx)
			{
				ProcessStartStopEvent(new EventData(kv, isStopEx), /* isStart */ false);
				return true;
			}

			// ReSharper disable once InvertIf
			if (kv.Key.Equals(InitializationFailedEventKey, StringComparison.Ordinal))
			{
				ProcessInitializationFailedEvent(kv);
				return true;
			}

			return false;
		}

		private void ProcessInitializationFailedEvent(KeyValuePair<string, object> kv)
		{
			// https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/HttpHandlerDiagnosticListener.cs#L84
			//
			//	catch (Exception ex)
			//	{
			//		// If anything went wrong, just no-op. Write an event so at least we can find out.
			//		this.Write(InitializationFailed, new { Exception = ex });
			//	}

			var causeException = ExtractProperty<Exception>(InitializationFailedExceptionPropertyName, kv);
			_logger.Error()?.LogException(causeException, "Received {DiagnosticEventKey} event", InitializationFailedEventKey);
		}

		internal class EventData : IEventData
		{
			private readonly KeyValuePair<string, object> _eventKeyValue;
			private readonly bool _isStopEx;
			private readonly HttpWebRequest _request;

			internal EventData(KeyValuePair<string, object> kv, bool isStopEx = false)
			{
				_eventKeyValue = kv;
				_request = ExtractProperty<HttpWebRequest>(EventRequestPropertyName, _eventKeyValue);
				_isStopEx = isStopEx;
			}

			public string Method => _request.Method;

			public object Request => _request;

			public int? StatusCode
			{
				get
				{
					if (_isStopEx)
					{
						// https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/HttpHandlerDiagnosticListener.cs#L675
						//
						//	private void RaiseResponseEvent(HttpWebRequest request, HttpStatusCode statusCode, WebHeaderCollection headers)
						//	{
						//		// Response event could be received several times for the same request in case it was redirected
						//		// IsLastResponse checks if response is the last one (no more redirects will happen)
						//		// based on response StatusCode and number or redirects done so far
						//		if (request.Headers.Get(RequestIdHeaderName) != null && IsLastResponse(request, statusCode))
						//		{
						//			this.Write(RequestStopExName, new { Request = request, StatusCode = statusCode, Headers = headers });
						//		}
						//	}

						return (int)ExtractProperty<HttpStatusCode>(StopExStatusCodePropertyName, _eventKeyValue);
					}

					var responsePropertyValue = ExtractProperty<HttpWebResponse>(EventResponsePropertyName, _eventKeyValue);
					return (int?)responsePropertyValue?.StatusCode;
				}
			}

			public Uri Url => _request.RequestUri;

			public bool ContainsRequestHeader(string headerName)
			{
				var values = _request.Headers.GetValues(headerName);
				return values != null && values.Length > 0;
			}

			public void AddRequestHeader(string headerName, string headerValue) =>
				_request.Headers.Add(headerName, headerValue);

			public override string ToString() => new ToStringBuilder($"{nameof(HttpDiagnosticListenerFullFrameworkImpl)}.{nameof(EventData)}")
			{
				{ "Event key/value", _eventKeyValue }
			}.ToString();
		}
	}
}
