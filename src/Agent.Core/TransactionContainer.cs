using System;
using System.Collections.Generic;
using Elastic.Agent.Core.Model.Payload;

namespace Elastic.Agent.Core
{
    /// <summary>
    /// Transaction container storing and managing transactions that are in progress (started, but not ended)
    /// </summary>
    public class TransactionContainer //TODO: make it internal and friend other elastic.apm dlls
    {
        //Dummy storage, we need logic here to store multiple onces across threads, etc.
        //Plan: AsyncLocal<T>
        public static List<Transaction> Transactions { get; set; }
    }
}
