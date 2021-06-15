// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Model;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Utilities
{
	internal class NoopPayloadSender : IPayloadSender
	{
		private readonly BreakdownMetricsProvider _breakdownMetricsProvider;

		public NoopPayloadSender(BreakdownMetricsProvider breakdownMetricsProvider = null) => _breakdownMetricsProvider = breakdownMetricsProvider;

		public void QueueError(IError error) { }

		public void QueueTransaction(ITransaction transaction)
		{
			if(transaction is Transaction realTransaction)
				_breakdownMetricsProvider?.CaptureTransaction(realTransaction);
		}

		public void QueueSpan(ISpan span) { }

		public void QueueMetrics(IMetricSet metricSet) { }
	}
}
