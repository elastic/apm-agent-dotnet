// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Model;

namespace Elastic.Apm.Filters
{
	/// <summary>
	/// A filter that redacts HTTP headers based on the <see cref="IConfigurationReader.SanitizeFieldNames"/> setting
	/// </summary>
	public class HeaderDictionarySanitizerFilter
	{
		public static IError Filter(IError error)
		{
			if (error is Error realError)
				Sanitization.SanitizeHeadersInContext(realError.Context, realError.Configuration);
			return error;
		}

		public static ITransaction Filter(ITransaction transaction)
		{
			if (transaction is Transaction { IsContextCreated: true })
				Sanitization.SanitizeHeadersInContext(transaction.Context, transaction.Configuration);
			return transaction;
		}
	}
}
