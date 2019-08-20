using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Encapsulates common functionality shared between <see cref="Span" /> and <see cref="Transaction" />
	/// </summary>
	internal static class ExecutionSegmentCommon
	{
		internal static void CaptureSpan(Span span, Action<ISpan> capturedAction)
		{
			try
			{
				capturedAction(span);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, span)) { }
			finally
			{
				span.End();
			}
		}

		internal static void CaptureSpan(Span span, Action capturedAction)
		{
			try
			{
				capturedAction();
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, span)) { }
			finally
			{
				span.End();
			}
		}

		internal static T CaptureSpan<T>(Span span, Func<ISpan, T> func)
		{
			try
			{
				return func(span);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, span)) { }
			finally
			{
				span.End();
			}

			return default;
		}

		internal static T CaptureSpan<T>(Span span, Func<T> func)
		{
			try
			{
				return func();
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, span)) { }
			finally
			{
				span.End();
			}

			return default;
		}

		internal static Task CaptureSpan(Span span, Func<Task> func)
		{
			var task = func();
			RegisterContinuation(task, span);
			return task;
		}

		internal static Task CaptureSpan(Span span, Func<ISpan, Task> func)
		{
			var task = func(span);
			RegisterContinuation(task, span);
			return task;
		}

		internal static Task<T> CaptureSpan<T>(Span span, Func<Task<T>> func)
		{
			var task = func();
			RegisterContinuation(task, span);

			return task;
		}

		internal static Task<T> CaptureSpan<T>(Span span, Func<ISpan, Task<T>> func)
		{
			var task = func(span);
			RegisterContinuation(task, span);
			return task;
		}

		/// <summary>
		/// Registers a continuation on the task.
		/// Within the continuation it ends the transaction and captures errors
		/// </summary>
		private static void RegisterContinuation(Task task, ISpan span) => task.ContinueWith(t =>
		{
			if (t.IsFaulted)
			{
				if (t.Exception != null)
				{
					if (t.Exception is AggregateException aggregateException)
					{
						ExceptionFilter.Capture(
							aggregateException.InnerExceptions.Count == 1
								? aggregateException.InnerExceptions[0]
								: aggregateException.Flatten(), span);
					}
					else
						ExceptionFilter.Capture(t.Exception, span);
				}
				else
					span.CaptureError("Task faulted", "A task faulted", new StackTrace(true).GetFrames());
			}
			else if (t.IsCanceled)
			{
				if (t.Exception == null)
				{
					span.CaptureError("Task canceled", "A task was canceled",
						new StackTrace(true).GetFrames()); //TODO: this async stacktrace is hard to use, make it readable!
				}
				else
					span.CaptureException(t.Exception);
			}

			span.End();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

		public static void CaptureException(
			Exception exception,
			IApmLogger logger,
			IPayloadSender payloadSender,
			IExecutionSegment executionSegment,
			IConfigurationReader configurationReader,
			Transaction transaction,
			string culprit = null,
			bool isHandled = false,
			string parentId = null
		)
		{
			var capturedException = new CapturedException(exception, isHandled, configurationReader, logger);

			payloadSender.QueueError(new Error(capturedException, transaction, parentId ?? executionSegment.Id, logger)
			{
				Culprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit,
				Context = transaction.Context
			});
		}

		public static void CaptureError(
			string message,
			string culprit,
			StackFrame[] frames,
			IPayloadSender payloadSender,
			IApmLogger logger,
			IExecutionSegment executionSegment,
			IConfigurationReader configurationReader,
			Transaction transaction,
			string parentId = null
		)
		{
			var capturedException = new CapturedException(message, frames != null ?
				StacktraceHelper.GenerateApmStackTrace(frames, logger, configurationReader, "failed capturing stacktrace") : null);

			payloadSender.QueueError(new Error(capturedException, transaction, parentId ?? executionSegment.Id, logger)
			{
				Culprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit,
				Context = transaction.Context
			});
		}
	}
}
