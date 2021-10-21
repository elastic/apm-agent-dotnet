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
		public ITransaction Filter(ITransaction transaction)
		{
			if (transaction is Transaction realTransaction)
			{
				if (realTransaction.IsContextCreated)
				{
					if (realTransaction.Context.Request?.Headers != null)
					{
						foreach (var key in realTransaction.Context.Request.Headers.Keys.ToList())
						{
							if (WildcardMatcher.IsAnyMatch(realTransaction.Configuration.SanitizeFieldNames, key))
								realTransaction.Context.Request.Headers[key] = Consts.Redacted;
						}
					}

					if (realTransaction.Context.Message?.Headers != null)
					{
						foreach (var key in realTransaction.Context.Message.Headers.Keys.ToList())
						{
							if (WildcardMatcher.IsAnyMatch(realTransaction.Configuration.SanitizeFieldNames, key))
								realTransaction.Context.Message.Headers[key] = Consts.Redacted;
						}
					}
				}
			}

			return transaction;
		}
	}
}
