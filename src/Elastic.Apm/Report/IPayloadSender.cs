// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;

namespace Elastic.Apm.Report
{
	public interface IPayloadSender
	{
		void QueueError(IError error);

		void QueueMetrics(IMetricSet metrics);

		void QueueSpan(ISpan span);

		void QueueTransaction(ITransaction transaction);
	}
}
