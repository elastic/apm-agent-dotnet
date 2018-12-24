using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
    public interface IElasticApm
    {
        Transaction CurrentTransaction { get; }

        Transaction StartTransaction(string name, string type);

        void CaptureTransaction(string name, string type, Action<Transaction> action);
        
        void CaptureTransaction(string name, string type, Action action);
        
        T CaptureTransaction<T>(string name, string type, Func<Transaction, T> func);
        
        T CaptureTransaction<T>(string name, string type, Func<T> func);
        
        Task CaptureTransaction(string name, string type, Func<Task> func);
        
        Task CaptureTransaction(string name, string type, Func<Transaction, Task> func);

        Task<T> CaptureTransaction<T>(string name, string type, Func<Task<T>> func);

        Task<T> CaptureTransaction<T>(string name, string type, Func<Transaction, Task<T>> func);

        Service Service { get; set; }
    }
}