using System.Threading;
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

			private static string DbgCurrentThreadNameOrId => Thread.CurrentThread.Name ?? Thread.CurrentThread.ManagedThreadId.ToString();

			internal T Value
			{
				get
				{
					_logger.Trace()
						?.Log("Getting value..." +
							" Thread: {DbgThreadNameOrId}. Current value: {ExecutionSegment}.",
							DbgCurrentThreadNameOrId, _value.Value);
					return _value.Value;
				}

				set
				{
					_logger.Trace()
						?.Log("Setting value..." +
							" Thread: {DbgThreadNameOrId}. Current value: {ExecutionSegment}. New value: {ExecutionSegment}.",
							DbgCurrentThreadNameOrId, _value.Value, value);
					_value.Value = value;
				}
			}
		}
	}
}
