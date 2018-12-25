using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model.Payload
{
    public class Transaction : ITransaction
    {
        internal Service service;

        public Transaction(String name, string type)
        {
            start = DateTimeOffset.UtcNow;
            this.Name = name;
            this.Type = type;
            this.Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        /// <summary>
        /// The duration of the transaction.
        /// If it's not set (HasValue returns false) then the value 
        /// is automatically calculated when <see cref="End"/> is called.
        /// </summary>
        /// <value>The duration.</value>
        public long? Duration { get; set; } //TODO datatype?, TODO: Greg, imo should be internal, TBD!

        public String Type { get; set; }

        public String Name { get; set; }

        /// <summary>
        /// A string describing the result of the transaction. 
        /// This is typically the HTTP status code, or e.g. "success" for a background task.
        /// </summary>
        /// <value>The result.</value>
        public String Result { get; set; }

        public String Timestamp => start.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
        internal readonly DateTimeOffset start;

        public Context Context { get; set; }

        //TODO: probably won't need with intake v2
        public ISpan[] Spans => spans.ToArray();

        //TODO: measure! What about List<T> with lock() in our case?
        internal BlockingCollection<Span> spans = new BlockingCollection<Span>();

        public const string TYPE_REQUEST = "request";

        public void End()
        {
            if (!Duration.HasValue)
            {
                this.Duration = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;
            }

            Apm.Agent.PayloadSender.QueuePayload(new Payload
            {
                Transactions = new List<Transaction>{
                    this
                },
                Service =  this.service
            });

            TransactionContainer.Transactions.Value = null;
        }

        public ISpan StartSpan(string name, string type, string subType = null, String action = null)
        {
            var retVal = new Span(name, type, this);
           
            if(subType != null)
            {
                retVal.Subtype = subType;
            }

            if(action != null)
            {
                retVal.Action = action;
            }

            var currentTime = DateTimeOffset.UtcNow;
            retVal.Start = (Decimal)(currentTime - this.start).TotalMilliseconds;
            retVal.transaction = this;
            return retVal;
        }

        public void CaptureException(Exception exception, string culprit = null, bool isHandled = false)
        {
            var capturedCulprit = String.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;
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
                    Id = this.Id
                }
            };

            if (!String.IsNullOrEmpty(exception.StackTrace))
            {
                  error.Exception.Stacktrace
                       = StacktraceHelper.GenerateApmStackTrace(new System.Diagnostics.StackTrace(exception).GetFrames(), Api.ElasticApm.PublicApiLogger, "failed capturing stacktrace");
            }

            error.Context = this.Context;
            Apm.Agent.PayloadSender.QueueError(new Error { Errors = new List<Error.Err> { error }, Service = this.service});
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
                    Id = this.Id
                }
            };

            if (frames != null)
            {
                error.Exception.Stacktrace
                    = StacktraceHelper.GenerateApmStackTrace(frames, Api.ElasticApm.PublicApiLogger, "failed capturing stacktrace");
            }

            error.Context = this.Context;
            Apm.Agent.PayloadSender.QueueError(new Error { Errors = new List<Error.Err> { error }, Service = this.service});
        }

        public void CaptureSpan(string name, string type, Action<ISpan> action)
        {
            var span = StartSpan(name, type);

            try
            {
                action(span);
            }
            catch (Exception e) when (Capture(e, span)) { }
            finally
            {
                span.End();
            }
        }

        public void CaptureSpan(string name, string type, Action action)
        {
            var span = StartSpan(name, type);

            try
            {
                action();
            }
            catch (Exception e) when (Capture(e, span)) { }
            finally
            {
                span.End();
            }
        }

        public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func)
        {
            var span = StartSpan(name, type);
            var retVal = default(T);
            try
            {
                retVal = func(span);
            }
            catch (Exception e) when (Capture(e, span)) { }
            finally
            {
                span.End();
            }

            return retVal;
        }

        public T CaptureSpan<T>(string name, string type, Func<T> func)
        {
            var span = StartSpan(name, type);
            var retVal = default(T);
            try
            {
                retVal = func();
            }
            catch (Exception e) when (Capture(e, span)) { }
            finally
            {
                span.End();
            }

            return retVal;
        }

        public Task CaptureSpan(string name, string type, Func<Task> func)
        {
            var span = StartSpan(name, type);
            var task = func();
            RegisterContinuation(task, span);
            return task;
        }

        public Task CaptureSpan(string name, string type, Func<ISpan, Task> func)
        {
            throw new NotImplementedException();
        }

        public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func)
        {
            throw new NotImplementedException();
        }

        public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Registers a continuation on the task.
        /// Within the continuation it ends the transaction and captures errors
        /// </summary>
        /// <param name="task">Task.</param>
        /// <param name="transaction">Transaction.</param>
        private void RegisterContinuation(Task task, ISpan span)
        {
            task.ContinueWith((t) =>
            {
                if (t.IsFaulted)
                {
                    if (t.Exception != null)
                    {
                        if (t.Exception is AggregateException aggregateException )
                        {
                            Capture(
                                aggregateException.InnerExceptions.Count == 1
                                    ? aggregateException.InnerExceptions[0]
                                    : aggregateException.Flatten(), span);
                        }
                        else
                        {
                            Capture(t.Exception, span);
                        }
                    }
                    else
                    {
                        span.CaptureError("Task faulted", "A task faulted", new StackTrace().GetFrames());
                    }
                }
                else if (t.IsCanceled)
                {
                    if (t.Exception == null)
                    {
                        span.CaptureError("Task canceled", "A task was canceled", new StackTrace().GetFrames()); //TODO: this async stacktrace is hard to use, make it readable!
                    }
                    else
                    {
                        span.CaptureException(t.Exception);
                    }
                }
               
                span.End();
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        
        private bool Capture(Exception e, ISpan span)
        {
            span.CaptureException(e);
            return false;
        }
    }
}