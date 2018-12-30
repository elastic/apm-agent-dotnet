using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
    public interface ISpan
    {
        IContext Context { get; set; }

        /// <summary>
        /// The duration of the span.
        /// If it's not set (its HasValue property is false) then the value 
        /// is automatically calculated when <see cref="End"/> is called.
        /// </summary>
        /// <value>The duration.</value>
        double? Duration { get; set; }

        /// <summary>
        /// The name of the span.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The type of the span.
        /// Examples: 'db', 'external'.
        /// </summary>
        string Type { get; set; }

        /// <summary>
        /// The subtype of the span.
        /// Examples: 'http', 'mssql'.
        /// </summary>
        string Subtype { get; set; }

        /// <summary>
        /// The action of the span.
        /// Examples: 'query'.
        /// </summary>
        string Action { get; set; }

        /// <summary>
        /// Offset relative to the transaction's timestamp identifying the start of the span, in milliseconds.
        /// </summary>
        decimal Start { get; set; }

        /// <summary>
        /// The id of the span.
        /// </summary>
        int Id { get; set; }

        List<Stacktrace> Stacktrace { get; set; }

        /// <summary>
        /// UUID of the enclosing transaction.
        /// </summary>
        Guid Transaction_id { get; }

        /// <summary>
        /// Ends the span and schedules it to be reported to the APM Server.
        /// It is illegal to call any methods on a span instance which has already ended.
        /// </summary>
        void End();

        /// <summary>
        /// Captures an exception and reports it to the APM server.
        /// </summary>
        /// <param name="exception">The exception to capture.</param>
        /// <param name="culprit">The value of this parameter is shown as 'Culprit' on the APM UI.</param>
        void CaptureException(Exception exception, string culprit = null);
        
        /// <summary>
        /// Captures a custom error and reports it to the APM server.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="culprit">The culprit of the error.</param>
        /// <param name="frames">The stack trace when the error occured.</param>
        void CaptureError(string message, string culprit, StackFrame[] frames);
    }
    
    public interface IContext
    {
        IDb Db { get; set; }
        IHttp Http { get; set; }
    }

    public interface IDb
    {
        string Instance { get; set; }
        string Statement { get; set; }
        string Type { get; set; }
    }

    public interface IHttp 
    {
        string Url { get; set; }
        int Status_code { get; set; }
        string Method { get; set; }
    }
}