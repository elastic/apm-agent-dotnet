using Elastic.Apm.Api;
using log4net;

namespace Elastic.Apm.Log4Net
{
	public class Log4NetCorrelation : ITransactionObserver
	{
		public void TransactionStarted(ITransaction transaction)
		{
			if (!Agent.IsConfigured) return;

			var currentTransaction = Agent.Tracer.CurrentTransaction;

			if (currentTransaction == null)
			{
				LogicalThreadContext.Properties.Remove("Transaction.Id");
				LogicalThreadContext.Properties.Remove("Trace.Id");
				return;
			}

			LogicalThreadContext.Properties["Transaction.Id"] = currentTransaction.Id;
			LogicalThreadContext.Properties["Trace.Id"] = currentTransaction.TraceId;
		}
	}
}
