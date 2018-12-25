using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
    public interface ITransaction
    {
        Guid Id { get; }

        /// <summary>
        /// The duration of the transaction.
        /// If it's not set (HasValue returns false) then the value 
        /// is automatically calculated when <see cref="End"/> is called.
        /// </summary>
        /// <value>The duration.</value>
        long? Duration { get; set; } //TODO datatype?, TODO: Greg, imo should be internal, TBD!

        String Type { get; set; }

        String Name { get; set; }

        /// <summary>
        /// A string describing the result of the transaction. 
        /// This is typically the HTTP status code, or e.g. "success" for a background task.
        /// </summary>
        /// <value>The result.</value>
        String Result { get; set; }

        String Timestamp { get; }

        Context Context { get; set; }

        //TODO: probably won't need with intake v2
        ISpan[] Spans { get; }

        void End();

        ISpan StartSpan(string name, string type, string subType = null, String action = null);

        void CaptureException(Exception exception, string culprit = null, bool isHandled = false);

        void CaptureError(string message, string culprit, StackFrame[] frames);
        
        void CaptureSpan(string name, string type, Action<ISpan> action);
        
        void CaptureSpan(string name, string type, Action action);
        
        T CaptureSpan<T>(string name, string type, Func<ISpan, T> func);
        
        T CaptureSpan<T>(string name, string type, Func<T> func);
        
        Task CaptureSpan(string name, string type, Func<Task> func);
        
        Task CaptureSpan(string name, string type, Func<ISpan, Task> func);

        Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func);

        Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func);
    }
}