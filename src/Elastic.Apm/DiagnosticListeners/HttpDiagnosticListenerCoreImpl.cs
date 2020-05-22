// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Elastic.Apm.DiagnosticListeners
{
	internal class HttpDiagnosticListenerCoreImpl : HttpDiagnosticListenerImplBase<HttpRequestMessage, HttpResponseMessage>
	{
		public HttpDiagnosticListenerCoreImpl(IApmAgent agent)
			: base(agent) { }

		protected override Dictionary<string, string[]> GetHeaders(HttpResponseMessage response) =>
			response.Headers?.ToDictionary(
				header => header.Key,
				header => header.Value?.ToArray());

		internal override string ExceptionEventKey => "System.Net.Http.Exception";

		public override string Name => "HttpHandlerDiagnosticListener";
		internal override string StartEventKey => "System.Net.Http.HttpRequestOut.Start";
		internal override string StopEventKey => "System.Net.Http.HttpRequestOut.Stop";

		protected override Uri RequestGetUri(HttpRequestMessage request) => request.RequestUri;

		protected override string RequestGetMethod(HttpRequestMessage request) => request.Method.Method;

		protected override bool RequestHeadersContain(HttpRequestMessage request, string headerName)
			=> request.Headers.Contains(headerName);

		protected override void RequestHeadersAdd(HttpRequestMessage request, string headerName, string headerValue) =>
			request.Headers.Add(headerName, headerValue);

		protected override int ResponseGetStatusCode(HttpResponseMessage response) => (int)response.StatusCode;
	}
}
