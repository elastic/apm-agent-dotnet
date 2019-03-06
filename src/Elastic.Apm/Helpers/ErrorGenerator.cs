using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Helpers
{
	public static class ErrorReporter
	{
//		public static void CaptureException(Exception exception, string traceId, string transactionId, string parentId, AbstractLogger logger, IPayloadSender sender, string culprit = null, bool isHandled = false)
//		{
//			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;
//
//			var capturedException = new CapturedException
//			{
//				Message = exception.Message,
//				Type = exception.GetType().FullName,
//				Handled = isHandled
//			};
//
//			var error = new Error.ErrorDetail
//			{
//				Culprit = capturedCulprit,
//				Exception = capturedException,
//			};
//
//			if (!string.IsNullOrEmpty(exception.StackTrace))
//			{
//				capturedException.StacktTrace
//					= StacktraceHelper.GenerateApmStackTrace(new StackTrace(exception, true).GetFrames(), logger,
//						"failed capturing stacktrace");
//			}
//
//			error.Context = Context;
//			sender.QueueError(new Error(traceId, transactionId, parentId)  { Errors = new List<IErrorDetail> { error }, Service = Service });
//		}
//
//		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
//		{
//			var capturedException = new CapturedException
//			{
//				Message = message
//			};
//			var error = new Error.ErrorDetail
//			{
//				Culprit = culprit,
//				Exception = capturedException,
//			};
//
//			if (frames != null)
//			{
//				capturedException.StacktTrace
//					= StacktraceHelper.GenerateApmStackTrace(frames, _logger, "failed capturing stacktrace");
//			}
//
//			error.Context = Context;
//			_sender.QueueError(new Error(TraceId, Id, parentId ?? this.Id) { Errors = new List<IErrorDetail> { error }, Service = Service });
//		}
	}
}
