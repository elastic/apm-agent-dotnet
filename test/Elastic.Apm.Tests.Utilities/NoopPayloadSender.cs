// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Utilities
{
	public class NoopPayloadSender : IPayloadSender, IPayloadSenderWithFilters
	{
		public void QueueError(IError error) { }

		public void QueueTransaction(ITransaction transaction) { }

		public void QueueSpan(ISpan span) { }

		public void QueueMetrics(IMetricSet metricSet) { }

		public bool AddFilter(Func<ITransaction, ITransaction> transactionFilter) => true;

		public bool AddFilter(Func<ISpan, ISpan> spanFilter) => true;

		public bool AddFilter(Func<IError, IError> errorFilter) => true;

	}
}
