using System.Threading;
using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal sealed class CurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		private readonly AsyncLocal<Span> _currentSpan = new AsyncLocal<Span>();
		private readonly AsyncLocal<Transaction> _currentTransaction = new AsyncLocal<Transaction>();

		public Span CurrentSpan
		{
			get => _currentSpan.Value;
			set => _currentSpan.Value = value;
		}

		public Transaction CurrentTransaction
		{
			get => _currentTransaction.Value;
			set => _currentTransaction.Value = value;
		}
	}
}
