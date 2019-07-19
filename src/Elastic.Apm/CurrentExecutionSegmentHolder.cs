using System.Threading;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm
{
	internal class CurrentExecutionSegmentHolder: ICurrentExecutionSegmentHolder
	{
		private readonly AsyncLocal<Transaction> _localCurrentTransaction = new AsyncLocal<Transaction>();
//		private readonly AsyncLocal<Span> _localCurrentSpan = new AsyncLocal<Span>();
		private readonly ScopedLogger _logger;

		public CurrentExecutionSegmentHolder(IApmLogger logger)
		{
			_logger = logger?.Scoped(nameof(CurrentExecutionSegmentHolder));
		}

		public Transaction CurrentTransactionInternal
		{
			get => _localCurrentTransaction.Value;
			set => SetCurrentExecutionSegment(_localCurrentTransaction, "Transaction", value);
		}

//		public Span CurrentSpanInternal
//		{
//			get => _localCurrentSpan.Value;
//			set => SetCurrentExecutionSegment(_localCurrentSpan, "Span", value);
//		}

		private void SetCurrentExecutionSegment<TExecutionSegment>(
			AsyncLocal<TExecutionSegment> localCurrentExecutionSegment,
			string dbgExecutionSegmentKind,
			TExecutionSegment newCurrentExecutionSegment
		)
		{
			var currentExecutionSegment = localCurrentExecutionSegment.Value;

			if (currentExecutionSegment == null)
			{
				if (newCurrentExecutionSegment != null)
					_logger.Debug()?.Log($"Setting current {dbgExecutionSegmentKind} to {{{dbgExecutionSegmentKind}}}", newCurrentExecutionSegment);
			}
			else
			{
				if (newCurrentExecutionSegment == null)
				{
					_logger.Debug()
						?.Log($"Resetting current {dbgExecutionSegmentKind}."
							+ $" Just ended {dbgExecutionSegmentKind}: {{{dbgExecutionSegmentKind}}}", currentExecutionSegment);
				}
				else
				{
					_logger.Error()
						?.Log($"Setting current {dbgExecutionSegmentKind} to a new one"
							+ $" even though the current {dbgExecutionSegmentKind} did not end yet."
							+ $" Ongoing {dbgExecutionSegmentKind}: {{{dbgExecutionSegmentKind}}}, new transaction: {{{dbgExecutionSegmentKind}}}",
							currentExecutionSegment, newCurrentExecutionSegment);
				}
			}

			localCurrentExecutionSegment.Value = newCurrentExecutionSegment;
		}
	}
}
