using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report;

namespace Elastic.Apm.Model.Payload
{
	public class Transaction : ITransaction
	{
		private readonly IPayloadSender _sender;
		public const string TypeRequest = "request";
		internal readonly DateTimeOffset Start;

		public Transaction(string name, string type, IPayloadSender sender)
		{
			_sender = sender;
			Start = DateTimeOffset.UtcNow;
			Name = name;
			Type = type;
			Id = Guid.NewGuid();
		}

		public Context Context { get; set; }

		/// <inheritdoc />
		/// <summary>
		/// The duration of the transaction.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public long? Duration { get; set; } //TODO datatype?, TODO: Greg, imo should be internal, TBD!

		public Guid Id { get; }

		public string Name { get; set; }

		/// <inheritdoc />
		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		public string Result { get; set; }

		internal Service Service;

		//TODO: measure! What about List<T> with lock() in our case?
		internal BlockingCollection<Span> SpansInternal = new BlockingCollection<Span>();

		//TODO: probably won't need with intake v2
		public ISpan[] Spans => SpansInternal.ToArray();

		public string Timestamp => Start.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");

		public string Type { get; set; }

		public void End()
		{
			if (!Duration.HasValue) Duration = (long)(DateTimeOffset.UtcNow - Start).TotalMilliseconds;

			_sender.QueuePayload(new Payload
			{
				Transactions = new List<Transaction>
				{
					this
				},
				Service = Service
			});

			TransactionContainer.Transactions.Value = null;
		}

		public ISpan StartSpan(string name, string type, string subType = null, string action = null)
		{
			var retVal = new Span(name, type, this);

			if (!string.IsNullOrEmpty(subType)) retVal.Subtype = subType;

			if (!string.IsNullOrEmpty(action)) retVal.Action = action;

			var currentTime = DateTimeOffset.UtcNow;
			retVal.Start = (decimal)(currentTime - Start).TotalMilliseconds;
			retVal.Transaction = this;
			return retVal;
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;
			var error = new Error.Err
			{
				Culprit = capturedCulprit,
				Exception = new CapturedException
				{
					Message = exception.Message,
					Type = exception.GetType().FullName,
					Handled = isHandled
				},
				Transaction = new Error.Err.Trans
				{
					Id = Id
				}
			};

			if (!string.IsNullOrEmpty(exception.StackTrace))
			{
				error.Exception.StacktTrace
					= StacktraceHelper.GenerateApmStackTrace(new StackTrace(exception).GetFrames(), Tracer.PublicTracerLogger,
						"failed capturing stacktrace");
			}

			error.Context = Context;
			_sender.QueueError(new Error { Errors = new List<Error.Err> { error }, Service = Service });
		}

		public void CaptureError(string message, string culprit, StackFrame[] frames)
		{
			var error = new Error.Err
			{
				Culprit = culprit,
				Exception = new CapturedException
				{
					Message = message
				},
				Transaction = new Error.Err.Trans
				{
					Id = Id
				}
			};

			if (frames != null)
			{
				error.Exception.StacktTrace
					= StacktraceHelper.GenerateApmStackTrace(frames, Tracer.PublicTracerLogger, "failed capturing stacktrace");
			}

			error.Context = Context;
			_sender.QueueError(new Error { Errors = new List<Error.Err> { error }, Service = Service });
		}

		public void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);

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

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);

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

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);
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

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);
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

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);
			var task = func();
			RegisterContinuation(task, span);
			return task;
		}

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);
			var task = func(span);
			RegisterContinuation(task, span);
			return task;
		}

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);
			var task = func();
			RegisterContinuation(task, span);

			return task;
		}

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null)
		{
			var span = StartSpan(name, type, subType, action);
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
					span.CaptureError("Task faulted", "A task faulted", new StackTrace().GetFrames());
			}
			else if (t.IsCanceled)
			{
				if (t.Exception == null)
				{
					span.CaptureError("Task canceled", "A task was canceled",
						new StackTrace().GetFrames()); //TODO: this async stacktrace is hard to use, make it readable!
				}
				else
					span.CaptureException(t.Exception);
			}

			span.End();
		}, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}
}
