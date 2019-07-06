using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class CapturedException
	{
		public CapturedException(Exception exception, bool isHandled, IApmLogger logger) : this(
			exception.Message + (exception.InnerException != null
				? Environment.NewLine + "Inner Exception:" + Environment.NewLine + new CapturedException(exception.InnerException, isHandled, logger)
				: null),
			StacktraceHelper.GenerateApmStackTrace(exception, logger, $"{nameof(CapturedException)}.{nameof(Stacktrace)}"))
		{
			Handled = isHandled;
			Type = exception.GetType().FullName;
		}

		public CapturedException(string message, IEnumerable<CapturedStackFrame> stacktrace)
		{
			Message = message;
			Stacktrace = stacktrace.ToList();
		}

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Code { get; set; }

		public bool Handled { get; }

		public string Message { get; }

		public List<CapturedStackFrame> Stacktrace { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; }

		public override string ToString() => new ToStringBuilder(nameof(CapturedException))
		{
			{ nameof(Type), Type },
			{ nameof(Message), Message },
			{ nameof(Handled), Handled },
			{ nameof(Code), Code }
		}.ToString();
	}
}
