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
		/// <summary>
		/// Encapsulates types which are used as key to group MetricSets.
		/// </summary>
		private struct GroupKey
		{
			public TransactionInfo Transaction { get; }
			public SpanInfo Span { get; }

			public GroupKey(TransactionInfo transaction, SpanInfo span)
			{
				Transaction = transaction;
				Span = span;
			}
		}

		internal const string SpanSelfTime = "span.self_time";
		internal const string SpanSelfTimeCount = SpanSelfTime + ".count";
		internal const string SpanSelfTimeSumUs = SpanSelfTime + ".sum.us";
		internal const int MetricLimit = 1000;

		private readonly Dictionary<GroupKey, MetricSet> _itemsToSend = new();
		private readonly IApmLogger _logger;

		/// <summary>
		/// Indicates if the metric limit log was already printed.
		/// </summary>
		private bool _loggedWarning;

		/// <summary>
		/// Lock for operations on <see cref="_itemsToSend"/>.
		/// </summary>
		private readonly object _itemsToSendLock = new();

		public int ConsecutiveNumberOfFailedReads { get; set; }

		public string DbgName => nameof(BreakdownMetricsProvider);

		public BreakdownMetricsProvider(IApmLogger logger) => _logger = logger.Scoped(nameof(BreakdownMetricsProvider));

		public bool IsMetricAlreadyCaptured
		{
			get
			{
				lock (_itemsToSendLock)
					return _itemsToSend.Count > 0;
			}
		}

		public bool IsEnabled(IReadOnlyList<WildcardMatcher> matchers) => !WildcardMatcher.IsAnyMatch(matchers, SpanSelfTime);

		public void CaptureTransaction(Transaction transaction)
		{
			lock (_itemsToSendLock)
			{
				foreach (var item in transaction.SpanTimings)
				{
					var groupKey = new GroupKey(new TransactionInfo { Name = transaction.Name, Type = transaction.Type },
						new SpanInfo { Type = item.Key.Type, SubType = item.Key.SubType });

					if (_itemsToSend.TryGetValue(groupKey, out var itemToUpdate))
					{
						// We don't search in (iterate over) itemToUpdate.Sample, instead we take advantage of 2 facts here:
						// 1: Samples are stored in List<MetricsSample>
						// 2: The order is fixed: SpanSelfTimeCount, SpanSelfTimeSumUs
						var spanSelfTime = (itemToUpdate.Samples as List<MetricSample>)?[0];
						if (spanSelfTime is { KeyValue: { Key: SpanSelfTimeCount } })
						{
							spanSelfTime.KeyValue =
								new KeyValuePair<string, double>(SpanSelfTimeCount, spanSelfTime.KeyValue.Value + item.Value.Count);
							var spanSelfTimeSumUs = (itemToUpdate.Samples as List<MetricSample>)?[1];
							if (spanSelfTimeSumUs is { KeyValue: { Key: SpanSelfTimeSumUs } })
							{
								spanSelfTimeSumUs.KeyValue =
									new KeyValuePair<string, double>(SpanSelfTimeSumUs,
										spanSelfTimeSumUs.KeyValue.Value + item.Value.TotalDuration * 1000);
							}
						}
					}
					else if (_itemsToSend.Count < MetricLimit)
					{
						var metricSet =
							new MetricSet(new List<MetricSample>
							{
								new(SpanSelfTimeCount, item.Value.Count), new(SpanSelfTimeSumUs, item.Value.TotalDuration * 1000)
							})
							{ Span = groupKey.Span, Transaction = groupKey.Transaction };
						_itemsToSend.Add(groupKey, metricSet);
					}
					else
					{
						if (_loggedWarning)
							continue;

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

			lock (_itemsToSendLock)
			{
				retVal = new List<MetricSet>(_itemsToSend.Count);

				// According to the spec, timestampNow should be the time when we report the metrics.
				var timestampNow = TimeUtils.TimestampNow();

				foreach (var item in _itemsToSend.Values.ToList())
				{
					item.Timestamp = timestampNow;
					retVal.Add(item);
				}

				_itemsToSend.Clear();
				_loggedWarning = false;
			}
			return retVal;
		}
	}
}
