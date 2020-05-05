// Licensed to Elasticsearch B.V under one or more agreements.
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

		internal static T CaptureSpan<T>(Span span, Func<T> func)
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
			string culprit = null,
			bool isHandled = false,
			string parentId = null
		)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? GetCulprit(exception, configurationReader) : culprit;

			var capturedException = new CapturedException { Message = exception.Message, Type = exception.GetType().FullName, Handled = isHandled };

			capturedException.StackTrace = StacktraceHelper.GenerateApmStackTrace(exception, logger,
				$"{nameof(Transaction)}.{nameof(CaptureException)}", configurationReader);

			payloadSender.QueueError(new Error(capturedException, transaction, parentId ?? executionSegment.Id, logger)
			{
				Culprit = capturedCulprit,
			});
		}

		private const string DefaultCulprit = "ElasticApm.UnknownCulprit";
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

		private static bool IsInApp(string fullyQualifiedTypeName, IReadOnlyCollection<string> excludedNamespaces, IReadOnlyCollection<string> applicationNamespaces)
		{
			if (string.IsNullOrEmpty(fullyQualifiedTypeName)) return false;

			if (applicationNamespaces.Count != 0)
			{
				foreach (var include in applicationNamespaces)
					if (fullyQualifiedTypeName.StartsWith(include, StringComparison.Ordinal)) return true;

				return false;
			}

			foreach (var exclude in excludedNamespaces)
				if (fullyQualifiedTypeName.StartsWith(exclude, StringComparison.Ordinal)) return false;

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
			string parentId = null
		)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;

			var capturedException = new CapturedException { Message = message };

			if (frames != null)
			{
				capturedException.StackTrace
					= StacktraceHelper.GenerateApmStackTrace(frames, logger, configurationReader, "failed capturing stacktrace");
			}

			payloadSender.QueueError(new Error(capturedException, transaction, parentId ?? executionSegment.Id, logger)
			{
				Culprit = capturedCulprit,
			});
		}

		internal static IExecutionSegment GetCurrentExecutionSegment(IApmAgent agent) =>
			agent.Tracer.CurrentSpan ?? (IExecutionSegment)agent.Tracer.CurrentTransaction;

		internal static Span StartSpanOnCurrentExecutionSegment(IApmAgent agent, string spanName, string spanType, string subType = null, InstrumentationFlag instrumentationFlag = InstrumentationFlag.None)
		{
			var currentExecutionSegment = GetCurrentExecutionSegment(agent);

			if (currentExecutionSegment == null)
				return null;

			return currentExecutionSegment switch
			{
				Span span => span.StartSpanInternal(spanName, spanType, subType, instrumentationFlag: instrumentationFlag),
				Transaction transaction => transaction.StartSpanInternal(spanName, spanType, subType, instrumentationFlag: instrumentationFlag),
				_ => null
			};
		}
	}
}
