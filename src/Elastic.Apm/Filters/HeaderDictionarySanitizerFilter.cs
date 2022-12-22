// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.Filters
{
	/// <summary>
	/// A filter that redacts HTTP headers based on the <see cref="IConfigurationReader.SanitizeFieldNames"/> setting
	/// </summary>
	public class HeaderDictionarySanitizerFilter
	{
		public IError Filter(IError error)
		{
			if (error is Error realError)
				Sanitize(realError.Context, realError.Configuration);
			return error;
		}

		public ITransaction Filter(ITransaction transaction)
		{
			if (transaction is Transaction { IsContextCreated: true })
				Sanitize(transaction.Context, transaction.Configuration);
			return transaction;
		}

		private static void Sanitize(Context context, IConfigurationReader configuration)
		{
			if (context?.Request?.Headers != null)
			{
				foreach (var key in context.Request.Headers.Keys.ToList())
				{
					if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, key))
						context.Request.Headers[key] = Consts.Redacted;
				}
			}

			if (context?.Message?.Headers != null)
			{
				foreach (var key in context.Message.Headers.Keys.ToList())
				{
					if (WildcardMatcher.IsAnyMatch(configuration.SanitizeFieldNames, key))
						context.Message.Headers[key] = Consts.Redacted;
				}
			}
		}
	}
}
