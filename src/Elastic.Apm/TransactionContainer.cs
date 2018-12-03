using System.Collections.Generic;
using System.Threading;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm
{
    /// <summary>
    /// Transaction container storing and managing transactions that are in progress (started, but not ended)
    /// </summary>
    public static class TransactionContainer //TODO: make it internal and friend other elastic.apm dlls
    {
        public static AsyncLocal<List<Transaction>> Transactions { get; set; } = new AsyncLocal<List<Transaction>>();
    }
}
