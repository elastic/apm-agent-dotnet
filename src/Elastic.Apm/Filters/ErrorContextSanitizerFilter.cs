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
	/// A filter that sanitizes fields on error based on the <see cref="IConfigurationReader.SanitizeFieldNames"/> setting
	/// </summary>
	internal class ErrorContextSanitizerFilter
	{
		public IError Filter(IError error)
		{
			if (error is Error realError && realError.Configuration != null)
			{
				if (realError.Context?.Request?.Headers != null)
				{
					foreach (var key in realError.Context.Request.Headers.Keys.ToList())
					{
						if (WildcardMatcher.IsAnyMatch(realError.Configuration.SanitizeFieldNames, key))
							realError.Context.Request.Headers[key] = Consts.Redacted;
					}
				}

				if (realError.Context?.Message?.Headers != null)
				{
					foreach (var key in realError.Context.Message.Headers.Keys.ToList())
					{
						if (WildcardMatcher.IsAnyMatch(realError.Configuration.SanitizeFieldNames, key))
							realError.Context.Message.Headers[key] = Consts.Redacted;
					}
				}
			}

			return error;
		}
	}
}
