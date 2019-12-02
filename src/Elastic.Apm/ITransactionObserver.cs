using Elastic.Apm.Api;

namespace Elastic.Apm
{
	public interface ITransactionObserver
	{
		void TransactionStarted(ITransaction transaction);
	}
}
