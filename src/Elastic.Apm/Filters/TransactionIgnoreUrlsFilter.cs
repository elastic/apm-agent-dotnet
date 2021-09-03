// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.Filters
{
	/// <summary>
	/// Contains a transaction filter which filters out transactions based on request url path.
	/// </summary>
	internal class TransactionIgnoreUrlsFilter
	{
		public ITransaction Filter(ITransaction transaction)
		{
			if (transaction is Transaction realTransaction)
			{
				// If there is no context, there is no URL either, therefore this transaction can't be filtered based on the URL.
				if (!realTransaction.IsContextCreated)
					return transaction;

				return WildcardMatcher.IsAnyMatch(realTransaction.ConfigurationSnapshot.TransactionIgnoreUrls, transaction.Context?.Request?.Url?.PathName)
					? null
					: transaction;
			}

			return transaction;
		}
	}
}
