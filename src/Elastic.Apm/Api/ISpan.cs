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
        /// If it's not set (HasValue returns false) then the value 
        /// is automatically calculated when <see cref="End"/> is called.
        /// </summary>
        /// <value>The duration.</value>
        double? Duration { get; set; }

        string Name { get; set; }

        string Type { get; set; }

        string Subtype { get; set; }

        string Action { get; set; }

        decimal Start { get; set; }

        int Id { get; set; }

        List<Stacktrace> Stacktrace { get; set; }

        Guid Transaction_id { get; }

        void End();

        void CaptureException(Exception exception, string culprit = null);
        
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