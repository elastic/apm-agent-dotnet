// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class BreakdownMetricsProvider : IMetricsProvider
	{
		internal const string SpanSelfTime = "span.self_time";

		private readonly List<MetricSet> _itemsToSend = new();
		private readonly IApmLogger _logger;

		/// <summary>
		/// Indicates if the 10K limit log was already printed.
		/// </summary>
		private bool loggedWarning = false;

		private readonly object _lock = new();
		private int _transactionCount;
		public int ConsecutiveNumberOfFailedReads { get; set; }

		public string DbgName => nameof(BreakdownMetricsProvider);

		public BreakdownMetricsProvider(IApmLogger logger) => _logger = logger.Scoped(nameof(BreakdownMetricsProvider));

		public bool IsMetricAlreadyCaptured
		{
			get
			{
				lock (_lock)
					return _itemsToSend.Count > 0;
			}
		}

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> matchers) => !WildcardMatcher.IsAnyMatch(matchers, SpanSelfTime);

		public void CaptureTransaction(Transaction transaction)
		{
			lock (_lock)
			{
				_transactionCount++;
				var timestampNow = TimeUtils.TimestampNow();

				foreach (var item in transaction.SpanTimings)
				{
					var metricSet =
						new MetricSet(timestampNow,
							new List<MetricSample>
							{
								new($"{SpanSelfTime}.count", item.Value.Count), new($"{SpanSelfTime}.sum.us", item.Value.TotalDuration * 1000)
							})
						{
							Span = new SpanInfo { Type = item.Key.Type, SubType = item.Key.SubType },
							Transaction = new TransactionInfo { Name = transaction.Name, Type = transaction.Type }
						};

					if (_itemsToSend.Count < 1000)
						_itemsToSend.Add(metricSet);
					else
					{
						if (loggedWarning) continue;

						_logger.Warning()
							?.Log(
								"The limit of 1000 metricsets has been reached, no new metricsets will be created.");
						loggedWarning = true;
					}
				}

				var transactionMetric =
					new MetricSet(timestampNow,
						new List<MetricSample>
						{
							new("transaction.duration.count", _transactionCount),
							new("transaction.duration.sum.us", transaction.Duration!.Value * 1000),
							new("transaction.breakdown.count", _transactionCount),
						}) { Transaction = new TransactionInfo { Name = transaction.Name, Type = transaction.Type } };

				if (_itemsToSend.Count < 1000)
					_itemsToSend.Add(transactionMetric);
				else
				{
					if (!loggedWarning)
					{
						_logger.Warning()
							?.Log(
								"The limit of 1000 metricsets has been reached, no new metricsets will be created until the current set is sent to APM Server.");
					}
				}
			}
		}

		public IEnumerable<MetricSet> GetSamples()
		{
			var retVal = new List<MetricSet>(_itemsToSend.Count < 1000 ? _itemsToSend.Count : 1000);

			lock (_lock)
			{
				retVal.AddRange(_itemsToSend);
				_itemsToSend.Clear();
				_transactionCount = 0;
				loggedWarning = false;
			}

			return retVal;
		}
	}
}
