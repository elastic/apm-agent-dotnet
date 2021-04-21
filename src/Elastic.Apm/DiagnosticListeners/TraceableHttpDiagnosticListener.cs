// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.DiagnosticListeners
{
	internal abstract class TraceableHttpDiagnosticListener : DiagnosticListenerBase
	{
		private volatile bool _startHttpSpan;

		/// <summary>
		/// Whether a HTTP span should be started.
		/// The HTTP diagnostic listener is used to listen for all HTTP requests, but it may be started
		/// to capture spans for HTTP requests only to specific known services. In the event a HTTP request is not
		/// to a known service, <see cref="StartHttpSpan"/> determines whether a HTTP span should be captured.
		/// </summary>
		internal bool StartHttpSpan
		{
			get => _startHttpSpan;
			set => _startHttpSpan = value;
		}

		protected TraceableHttpDiagnosticListener(IApmAgent apmAgent, bool startHttpSpan) : base(apmAgent) =>
			_startHttpSpan = startHttpSpan;
	}
}
