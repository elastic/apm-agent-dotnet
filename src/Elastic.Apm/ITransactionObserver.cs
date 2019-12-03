using Elastic.Apm.Api;

namespace Elastic.Apm
{
	/// <summary>
	/// An interface which is used by the <see cref="Agent"/> to notify external components that the currently active transaction changed.
	/// </summary>
	internal interface ITransactionObserver
	{
		void ActiveTransactionChanged(ITransaction currentTransaction);
	}
}
