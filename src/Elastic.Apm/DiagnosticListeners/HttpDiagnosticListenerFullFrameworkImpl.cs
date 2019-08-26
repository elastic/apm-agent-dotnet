using System;
using System.Net;
using System.Reflection;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticListeners
{
	internal class HttpDiagnosticListenerFullFrameworkImpl : HttpDiagnosticListenerImplBase<HttpWebRequest, HttpWebResponse>
	{
		public HttpDiagnosticListenerFullFrameworkImpl(IApmAgent agent)
			: base(agent) { }

		internal override string ExceptionEventKey => "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

		public override string Name => "System.Net.Http.Desktop";
		internal override string StartEventKey => "System.Net.Http.Desktop.HttpRequestOut.Start";
		internal override string StopEventKey => "System.Net.Http.Desktop.HttpRequestOut.Stop";

		protected override Uri RequestGetUri(HttpWebRequest request) => request.RequestUri;

		protected override string RequestGetMethod(HttpWebRequest request) => request.Method;

		protected override bool RequestHeadersContain(HttpWebRequest request, string headerName)
		{
			var values = request.Headers.GetValues(headerName);
			return values != null && values.Length > 0;
		}

		protected override void RequestHeadersAdd(HttpWebRequest request, string headerName, string headerValue) =>
			request.Headers.Add(headerName, headerValue);

		protected override int ResponseGetStatusCode(HttpWebResponse response) => (int)response.StatusCode;

		/// <summary>
		/// In Full Framework "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop" does not send the exception property.
		/// Therefore we have a specialized ProcessExceptionEvent for Full Framework.
		/// </summary>
		protected override void ProcessExceptionEvent(object eventValue, Uri requestUrl)
		{
			Logger.Trace()?.Log("Processing stop.ex event... Request URL: {RequestUrl}", requestUrl);

			var requestObject = eventValue.GetType().GetTypeInfo().GetDeclaredProperty(EventRequestPropertyName)?.GetValue(eventValue);

			if (!(requestObject is HttpWebRequest request))
			{
				Logger.Trace()
					?.Log("Actual type of object ({EventRequestPropertyActualType}) in event's {EventRequestPropertyName} property " +
						"doesn't match the expected type ({EventRequestPropertyExpectedType}) - exiting",
						requestObject?.GetType().FullName, EventRequestPropertyName, typeof(HttpWebRequest).FullName);
				return;
			}
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
				var statusCodeObject = eventValue.GetType().GetTypeInfo().GetDeclaredProperty("StatusCode")?.GetValue(eventValue);
				if (statusCodeObject != null)
				{
					if (statusCodeObject is HttpStatusCode statusCode)
					{
						span.Context.Http.StatusCode = (int)statusCode;

						if (span.Context.Http.StatusCode >= 300)
						{
							span.CaptureError($"Failed outgoing HTTP call with HttpClient - StatusCode: {statusCode.ToString()}",
								$"HTTP {statusCode}", null);
						}
					}
					else
					{
						Logger.Trace()
							?.Log("Actual type of object ({EventStatusCodePropertyActualType}) in event's {EventStatusCodePropertyName} property " +
								"doesn't match the expected type ({EventStatusCodeePropertyExpectedType})",
								statusCodeObject.GetType().FullName, "StatusCode", typeof(HttpStatusCode).FullName);
					}
				}
			}

			span.End();
		}
	}
}
