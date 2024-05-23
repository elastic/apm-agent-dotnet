// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Filters;

namespace Elastic.Apm.Tests.Utilities
{
	internal class MockPayloadSenderWithFilters : MockPayloadSender
	{
		private readonly List<Func<ITransaction, ITransaction>> _transactionFilters = new();

		public MockPayloadSenderWithFilters() => _transactionFilters.Add(TransactionIgnoreUrlsFilter.Filter);

		public override void QueueTransaction(ITransaction transaction)
		{
			foreach (var filter in _transactionFilters)
			{
				if (filter(transaction) != null)
					base.QueueTransaction(transaction);
			}
		}
	}
}
