// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Ben.Demystifier;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Filters
{
	/// <summary>
	/// Stack trace capturing itself happens on the application thread (in order to get the real stack trace).
	/// This filter turns <see cref="Span.RawStackTrace" /> (which is a plain .NET System.Diagnostics.StackTrace instance) into
	/// <see cref="Span.StackTrace" /> (which is the representation of the intake API stacktrace model).
	/// This can be done on a non-application thread right before the span gets sent to the APM Server.
	/// </summary>
	internal class SpanStackTraceCapturingFilter
	{
		private readonly IApmServerInfo _apmServerInfo;
		private readonly IApmLogger _logger;

		public SpanStackTraceCapturingFilter(IApmLogger logger, IApmServerInfo apmServerInfo) =>
			(_logger, _apmServerInfo) = (logger, apmServerInfo);

		public ISpan Filter(ISpan iSpan)
		{
			if (!(iSpan is Span span)) return iSpan;

			if (span.RawStackTrace == null) return span;

			StackFrame[] trace;
			try
			{
				// I saw EnhancedStackTrace throwing exceptions in some environments
				// therefore we try-catch and fall back to a non-demystified call stack.
				trace = new EnhancedStackTrace(span.RawStackTrace).GetFrames();
			}
			catch
			{
				trace = span.RawStackTrace.GetFrames();
			}

			span.StackTrace = StacktraceHelper.GenerateApmStackTrace(trace,
				_logger,
				span.ConfigSnapshot, _apmServerInfo, $"Span `{span.Name}'");

			return span;
		}
	}
}
