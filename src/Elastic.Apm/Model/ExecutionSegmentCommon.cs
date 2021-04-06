// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Encapsulates common functionality shared between <see cref="Span" /> and <see cref="Transaction" />
	/// </summary>
	internal static class ExecutionSegmentCommon
	{
		private const string DefaultCulprit = "ElasticApm.UnknownCulprit";

		internal static void CaptureSpan(ISpan span, Action<ISpan> capturedAction)
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

		internal static void CaptureSpan(ISpan span, Action capturedAction)
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

		internal static T CaptureSpan<T>(ISpan span, Func<ISpan, T> func)
		{
			var retVal = default(T);
			try
			{
				retVal = func(span);
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, span)) { }
			finally
			{
				span.End();
			}

			return retVal;
		}

		internal static T CaptureSpan<T>(ISpan span, Func<T> func)
		{
			var retVal = default(T);
			try
			{
				retVal = func();
			}
			catch (Exception e) when (ExceptionFilter.Capture(e, span)) { }
			finally
			{
				span.End();
			}

			return retVal;
		}

		internal static Task CaptureSpan(ISpan span, Func<Task> func)
		{
			var task = func();
			RegisterContinuation(task, span);
			return task;
		}

		internal static Task CaptureSpan(ISpan span, Func<ISpan, Task> func)
		{
			var task = func(span);
			RegisterContinuation(task, span);
			return task;
		}

		internal static Task<T> CaptureSpan<T>(ISpan span, Func<Task<T>> func)
		{
			var task = func();
			RegisterContinuation(task, span);

			return task;
		}

		internal static Task<T> CaptureSpan<T>(ISpan span, Func<ISpan, Task<T>> func)
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
					if (t.Exception is { } aggregateException)
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
			IApmServerInfo apmServerInfo,
			string culprit = null,
			bool isHandled = false,
			string parentId = null,
			Dictionary<string, Label> labels = null
		)
		{
			if(executionSegment != null)
				executionSegment.Outcome = Outcome.Failure;

			var capturedCulprit = string.IsNullOrEmpty(culprit) ? GetCulprit(exception, configurationReader) : culprit;

			var capturedException = new CapturedException { Message = exception.Message, Type = exception.GetType().FullName, Handled = isHandled };

			capturedException.StackTrace = StacktraceHelper.GenerateApmStackTrace(exception, logger,
				$"{nameof(Transaction)}.{nameof(CaptureException)}", configurationReader, apmServerInfo);

			payloadSender.QueueError(new Error(capturedException, transaction, parentId ?? executionSegment?.Id, logger, labels)
			{
				Culprit = capturedCulprit
			});
		}

		private static string GetCulprit(Exception exception, IConfigurationReader configurationReader)
		{
			if (exception == null) return DefaultCulprit;

			var stackTrace = new StackTrace(exception);
			var frames = stackTrace.GetFrames();
			if (frames == null) return DefaultCulprit;

			var excludedNamespaces = configurationReader.ExcludedNamespaces;
			var applicationNamespaces = configurationReader.ApplicationNamespaces;

			foreach (var frame in frames)
			{
				var method = frame.GetMethod();
				var fullyQualifiedTypeName = method.DeclaringType?.FullName ?? "Unknown Type";
				if (IsInApp(fullyQualifiedTypeName, excludedNamespaces, applicationNamespaces)) return fullyQualifiedTypeName;
			}

			return DefaultCulprit;
		}

		private static bool IsInApp(string fullyQualifiedTypeName, IReadOnlyCollection<string> excludedNamespaces,
			IReadOnlyCollection<string> applicationNamespaces
		)
		{
			if (string.IsNullOrEmpty(fullyQualifiedTypeName)) return false;

			if (applicationNamespaces.Count != 0)
			{
				foreach (var include in applicationNamespaces)
				{
					if (fullyQualifiedTypeName.StartsWith(include, StringComparison.Ordinal))
						return true;
				}

				return false;
			}

			foreach (var exclude in excludedNamespaces)
			{
				if (fullyQualifiedTypeName.StartsWith(exclude, StringComparison.Ordinal))
					return false;
			}

			return true;
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
			IApmServerInfo apmServerInfo,
			string parentId = null,
			Dictionary<string, Label> labels = null
		)
		{
			if(executionSegment != null)
				executionSegment.Outcome = Outcome.Failure;

			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;

			var capturedException = new CapturedException { Message = message };

			if (frames != null)
			{
				capturedException.StackTrace
					= StacktraceHelper.GenerateApmStackTrace(frames, logger, configurationReader, apmServerInfo, "failed capturing stacktrace");
			}

			payloadSender.QueueError(new Error(capturedException, transaction, parentId ?? executionSegment?.Id, logger, labels)
			{
				Culprit = capturedCulprit
			});
		}

		internal static ISpan StartSpanOnCurrentExecutionSegment(IApmAgent agent, string spanName, string spanType, string subType = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None, bool captureStackTraceOnStart = false
		)
		{
			var currentExecutionSegment = agent.GetCurrentExecutionSegment();

			if (currentExecutionSegment == null)
				return null;

			return currentExecutionSegment switch
			{
				Span span => span.StartSpanInternal(spanName, spanType, subType, instrumentationFlag: instrumentationFlag,
					captureStackTraceOnStart: captureStackTraceOnStart),
				Transaction transaction => transaction.StartSpanInternal(spanName, spanType, subType, instrumentationFlag: instrumentationFlag,
					captureStackTraceOnStart: captureStackTraceOnStart),
				ISpan iSpan => iSpan.StartSpan(spanName, spanType, subType),
				ITransaction iTransaction => iTransaction.StartSpan(spanName, spanType, subType),
				_ => null
			};
		}

		/// <summary>
		/// Captures an error based on a log
		/// </summary>
		/// <param name="errorLog"></param>
		/// <param name="payloadSender"></param>
		/// <param name="logger"></param>
		/// <param name="executionSegment"></param>
		/// <param name="configSnapshot"></param>
		/// <param name="enclosingTransaction"></param>
		/// <param name="parentId"></param>
		/// <param name="serverInfo"></param>
		/// <param name="exception"></param>
		/// <param name="labels"></param>
		internal static void CaptureErrorLog(ErrorLog errorLog, IPayloadSender payloadSender, IApmLogger logger,
			IExecutionSegment executionSegment, IConfigSnapshot configSnapshot, Transaction enclosingTransaction, string parentId,
			IApmServerInfo serverInfo,
			Exception exception = null,
			Dictionary<string, Label> labels = null
		)
		{
			if(executionSegment != null)
				executionSegment.Outcome = Outcome.Failure;

			var error = new Error(errorLog, enclosingTransaction, parentId ?? executionSegment?.Id, logger, labels)
			{
				Culprit = $"{errorLog.Level ?? "Error"} log"
			};

			if (exception != null)
			{
				error.Exception = new CapturedException { Message = exception.Message, Type = exception.GetType().FullName };

				if (exception.StackTrace != null)
				{
					error.Exception.StackTrace
						= StacktraceHelper
							.GenerateApmStackTrace(exception, logger, $"Exception callstack for {nameof(CaptureErrorLog)}", configSnapshot,
								serverInfo);
				}
			}

			payloadSender.QueueError(error);
		}
	}
}
