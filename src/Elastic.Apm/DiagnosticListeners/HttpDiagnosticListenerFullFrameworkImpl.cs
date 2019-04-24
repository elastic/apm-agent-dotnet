using System;
using System.Net;

namespace Elastic.Apm.DiagnosticListeners
{
	internal class HttpDiagnosticListenerFullFrameworkImpl : HttpDiagnosticListenerImplBase<HttpWebRequest, HttpWebResponse>
	{
		public HttpDiagnosticListenerFullFrameworkImpl(IApmAgent components)
			: base(components) { }

		internal override string ExceptionEventKey => "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

		public override string Name => "System.Net.Http.Desktop";
		internal override string StartEventKey => "System.Net.Http.Desktop.HttpRequestOut.Start";
		internal override string StopEventKey => "System.Net.Http.Desktop.HttpRequestOut.Stop";

		protected override Uri RequestGetUri(HttpWebRequest request) => request.RequestUri;

		protected override string RequestGetMethod(HttpWebRequest request) => request.Method;

		protected override bool RequestHeadersContains(HttpWebRequest request, string headerName) =>
			request.Headers.GetValues(headerName).Length > 0;

		protected override void RequestHeadersAdd(HttpWebRequest request, string headerName, string headerValue) =>
			request.Headers.Add(headerName, headerValue);

		protected override int ResponseGetStatusCode(HttpWebResponse response) => (int)response.StatusCode;
	}
}
