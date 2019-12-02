using System;
using Elastic.Apm.Api;
using NLog;

namespace Elastic.Apm.NLog
{
	/// <summary>
	/// Adds Trace and Transaction Ids for every log in NLog that are created within an active transaction.
	/// Turn this on by passing an instance to <see cref="Agent.SetLogCorrelation"/>
	/// </summary>
	public class NLogCorrelation : ITransactionObserver
	{
		public void TransactionStarted(ITransaction transaction)
		{
			if (!Agent.IsConfigured) return;

			var currentTransaction = Agent.Tracer.CurrentTransaction;

			if (currentTransaction == null)
			{
				MappedDiagnosticsLogicalContext.Clear();
				return;
			}

			MappedDiagnosticsLogicalContext.Set("Transaction.Id", currentTransaction.Id);
			MappedDiagnosticsLogicalContext.Set("Trace.Id", currentTransaction.TraceId);
		}
	}
}
