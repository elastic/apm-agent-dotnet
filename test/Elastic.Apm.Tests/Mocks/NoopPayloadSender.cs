// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	public class NoopPayloadSender : IPayloadSender
	{
		public void QueueError(IError error) { }

		public void QueueTransaction(ITransaction transaction) { }

		public void QueueSpan(ISpan span) { }

		public void QueueMetrics(IMetricSet metricSet) { }
	}
}
