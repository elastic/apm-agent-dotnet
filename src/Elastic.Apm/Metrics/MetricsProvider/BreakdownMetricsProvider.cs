// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class BreakdownMetricsProvider : IMetricsProvider
	{
		internal const string SpanSelfTime = "span.self_time";
		internal const string SpanSelfTimeCount = SpanSelfTime + ".count";
		internal const string SpanSelfTimeSumUs = SpanSelfTime + ".sum.us";
		internal const int MetricLimit = 1000;

		private readonly List<MetricSet> _itemsToSend = new();
		private readonly IApmLogger _logger;

		/// <summary>
		/// Indicates if the metric limit log was already printed.
		/// </summary>
		private bool _loggedWarning;

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
								new(SpanSelfTimeCount, item.Value.Count),
								new(SpanSelfTimeSumUs, item.Value.TotalDuration * 1000)
							})
						{
							Span = new SpanInfo { Type = item.Key.Type, SubType = item.Key.SubType },
							Transaction = new TransactionInfo { Name = transaction.Name, Type = transaction.Type }
						};

					if (_itemsToSend.Count < MetricLimit)
						_itemsToSend.Add(metricSet);
					else
					{
						if (_loggedWarning) continue;

						_logger.Warning()
							?.Log(
								"The limit of {MetricLimit} metricsets has been reached, no new metricsets will be created until "
								+ "the current set is sent to APM Server.",
								MetricLimit);
						_loggedWarning = true;
					}
				}
			}
		}

		public IEnumerable<MetricSet> GetSamples()
		{
			List<MetricSet> retVal;

			lock (_lock)
			{
				retVal = new List<MetricSet>(_itemsToSend.Count);
				retVal.AddRange(_itemsToSend);
				_itemsToSend.Clear();
				_transactionCount = 0;
				_loggedWarning = false;
			}

			return retVal;
		}
	}
}
