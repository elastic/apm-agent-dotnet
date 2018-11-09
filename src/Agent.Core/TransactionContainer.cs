using System;
using System.Collections.Generic;
using System.Threading;
using Elastic.Agent.Core.Model.Payload;

namespace Elastic.Agent.Core
{
    /// <summary>
    /// Transaction container storing and managing transactions that are in progress (started, but not ended)
    /// </summary>
    public static class TransactionContainer //TODO: make it internal and friend other elastic.apm dlls
    {
        public static AsyncLocal<List<Transaction>> Transactions { get; set; } = new AsyncLocal<List<Transaction>>();
    }
}
