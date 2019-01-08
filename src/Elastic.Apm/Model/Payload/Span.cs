using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model.Payload
{
    public class Span : ISpan
    {
        public const String TypeDb = "db";
        public const String TypeExternal = "external";

        public const String SubtypeHttp = "http";
        public const String SubtypeMssql = "mssql";
        public const String SubtypeSqlite = "sqlite";

        public const String ActionQuery = "query";
        public const String ActionExec = "exec";

        public IContext Context { get; set; }

        /// <summary>
        /// The duration of the span.
        /// If it's not set (HasValue returns false) then the value 
        /// is automatically calculated when <see cref="End"/> is called.
        /// </summary>
        /// <value>The duration.</value>
        public double? Duration { get; set; }

        public String Name { get; set; }

        public String Type { get; set; }

        public String Subtype { get; set; }

        public String Action { get; set; }

        public Decimal Start { get; set; }

        public int Id { get; set; }

        public List<Stacktrace> Stacktrace { get; set; }

        public Guid TransactionId => Transaction.Id;
        internal Transaction Transaction;

        private readonly DateTimeOffset _start;

        public Span(string name, string type, Transaction transaction)
        {
            this.Transaction = transaction;
            _start = DateTimeOffset.UtcNow;
            Start = (decimal)(_start - transaction.Start).TotalMilliseconds;
            this.Name = name;
            this.Type = type;

            Random rnd = new Random();
            Id = rnd.Next();
        }

        public void End()
        {
            if (!Duration.HasValue)
            {
                this.Duration = (DateTimeOffset.UtcNow - _start).TotalMilliseconds;
            }

            Transaction?.SpanCollection.Add(this);
        }

        public void CaptureException(Exception exception, string culprit = null)
            => Transaction?.CaptureException(exception, culprit);

        public void CaptureError(string message, string culprit, StackFrame[] frames)
            => Transaction?.CaptureError(message, culprit, frames);

        public class ContextC : IContext
        {
            public IDb Db { get; set; }
            public IHttp Http { get; set; }
        }
    }

    public class Db : IDb
    {
        public string Instance { get; set; }
        public string Statement { get; set; }
        public string Type { get; set; }
    }

    public class Http : IHttp
    {
        public string Url { get; set; }
        public int StatusCode { get; set; }
        public string Method { get; set; }
    }
}
