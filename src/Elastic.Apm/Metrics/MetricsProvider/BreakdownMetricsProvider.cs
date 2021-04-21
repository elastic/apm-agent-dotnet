// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.Metrics.MetricsProvider
{
	internal class BreakdownMetricsProvider : IMetricsProvider
	{
		public int ConsecutiveNumberOfFailedReads { get; set; }

		private readonly object _lock = new();

		public string DbgName => nameof(BreakdownMetricsProvider);

		public bool IsMetricAlreadyCaptured
		{
			get
			{
				lock (_lock)
					return _itemsToSend.Count > 0;
			}
		}

		private readonly List<MetricSet> _itemsToSend = new();

		public void CaptureTransaction(Transaction transaction)
		{
			lock (_lock)
			{
				var timestampNow = TimeUtils.TimestampNow();

				foreach (var item in transaction.SpanTimings)
				{
					var metricSet =
						new MetricSet(timestampNow,
							new List<MetricSample>
							{
								new("span.self_time.count", item.Value.Count), new("span.self_time.sum.us", item.Value.TotalDuration)
							})
						{
							Span = new SpanInfo { Type = item.Key.Item1, SubType = item.Key.Item2 },
							Transaction = new TransactionInfo { Name = transaction.Name, Type = transaction.Type }
						};

					_itemsToSend.Add(metricSet);
				}

				var transactionMetric =
					new MetricSet(timestampNow,
						new List<MetricSample>
						{
							new("transaction.duration.count", _itemsToSend.Count + 1),
							new("transaction.duration.sum.us", transaction.Duration!.Value),
							new("transaction.breakdown.count", _itemsToSend.Count + 1),
						})
					{ Transaction = new TransactionInfo { Name = transaction.Name, Type = transaction.Type } };

				_itemsToSend.Add(transactionMetric);
			}
		}

		public IEnumerable<MetricSet> GetSamples()
		{
			var retVal = new List<MetricSet>();

			lock (_lock)
			{
				retVal.AddRange(_itemsToSend.Take(1000));
				_itemsToSend.Clear();
			}

			return retVal;
		}
	}
}
