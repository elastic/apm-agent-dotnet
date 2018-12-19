using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Model.Payload
{
    public class Transaction
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
        public Span[] Spans => spans.ToArray();

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

        public Span StartSpan(string name, string type, string subType = null, String action = null)
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
    }
}