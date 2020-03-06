using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal sealed class CurrentExecutionSegmentsContainer : ICurrentExecutionSegmentsContainer
	{
		private readonly ContextLocalHolder<Span> _currentSpan;
		private readonly ContextLocalHolder<Transaction> _currentTransaction;

		internal CurrentExecutionSegmentsContainer(IApmLogger logger)
		{
			_currentTransaction = new ContextLocalHolder<Transaction>(logger, "transaction");
			_currentSpan = new ContextLocalHolder<Span>(logger, "span");
		}

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

		private sealed class ContextLocalHolder<T>
		{
			private readonly IApmLogger _logger;
			private readonly AsyncLocal<T> _value = new AsyncLocal<T>();

			internal ContextLocalHolder(IApmLogger logger, string loggerNameSuffix) =>
				_logger = logger.Scoped($"{nameof(CurrentExecutionSegmentsContainer)}.{loggerNameSuffix}");

			internal T Value
			{
				get => _value.Value;
				set => _value.Value = value;
			}
		}
	}
}
