using System.Threading;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm
{
	/// <summary>
	/// Transaction container storing and managing transactions that are in progress (started, but not ended)
	/// </summary>
	internal class TransactionContainer
	{
		public AsyncLocal<Transaction> Transactions { get; set; } = new AsyncLocal<Transaction>();
	}
}
