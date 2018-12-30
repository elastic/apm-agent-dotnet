using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
    public interface IElasticApm
    {
        /// <summary>
        /// Returns the currently active transaction.
        /// </summary>
        ITransaction CurrentTransaction { get; }

        /// <summary>
        /// Starts and returns a custom transaction.
        /// </summary>
        /// <param name="name">The name of the transaction.</param>
        /// <param name="type">The type of the transaction.</param>
        /// <returns></returns>
        ITransaction StartTransaction(string name, string type);

        void CaptureTransaction(string name, string type, Action<ITransaction> action);
        
        void CaptureTransaction(string name, string type, Action action);
        
        T CaptureTransaction<T>(string name, string type, Func<ITransaction, T> func);
        
        T CaptureTransaction<T>(string name, string type, Func<T> func);
        
        Task CaptureTransaction(string name, string type, Func<Task> func);
        
        Task CaptureTransaction(string name, string type, Func<ITransaction, Task> func);

        Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func);

        Task<T> CaptureTransaction<T>(string name, string type, Func<ITransaction, Task<T>> func);

        /// <summary>
        /// Identifies the monitored service. If this remains unset the agent
        /// automatically populates it based on the entry assembly.
        /// </summary>
        Service Service { get; set; }
    }
}