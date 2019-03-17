using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Helpers
{
	internal static class StacktraceHelper
	{
		/// <summary>
		/// Turns a System.Diagnostic.StackFrame[] into a <see cref="CapturedStackFrame" /> list which can be reported to the APM Server
		/// </summary>
		/// <param name="frames">The stack frames to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="capturingFor">Just for logging.</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(StackFrame[] frames, IApmLogger logger, string capturingFor)
		{
			var retVal = new List<CapturedStackFrame>(frames.Length);

			try
			{
				retVal.AddRange(from item in frames
					let fileName = item?.GetMethod()?.DeclaringType?.Assembly?.GetName()?.Name
					where !string.IsNullOrEmpty(fileName)
					select new CapturedStackFrame
					{
						Function = item?.GetMethod()?.Name,
						FileName = fileName,
						Module = item?.GetMethod()?.ReflectedType?.Name,
						LineNo = item?.GetFileLineNumber() ?? 0
					});
			}
			catch (Exception e)
			{
				logger?.Warning()?.LogException(e, "Failed capturing stacktrace for {ApmContext}", capturingFor);
			}

			return retVal;
		}

		/// <summary>
		///  Turns an <see cref="Exception"/> into a <see cref="CapturedStackFrame" /> list which can be reported to the APM Server
		/// </summary>
		/// <param name="exception">The exception to rewrite into APM stack traces</param>
		/// <param name="logger">The logger to emit exceptions on should one occur</param>
		/// <param name="capturingFor">Just for logging.</param>
		/// <returns>A prepared List that can be passed to the APM server</returns>
		internal static List<CapturedStackFrame> GenerateApmStackTrace(Exception exception, IApmLogger logger, string capturingFor)
		{
			try
			{
				return GenerateApmStackTrace(new StackTrace(exception, true).GetFrames(), logger, capturingFor);
			}
			catch (Exception e)
			{
				logger?.Warning()?.LogException(e, "Failed extracting exception from stackTrace for {ApmContext}", capturingFor);
			}

			return null;
		}
	}
}
