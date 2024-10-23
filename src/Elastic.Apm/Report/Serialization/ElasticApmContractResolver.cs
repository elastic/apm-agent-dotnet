// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Reflection;
using Elastic.Apm.Api.Constraints;

namespace Elastic.Apm.Report.Serialization
{
	internal class ElasticApmContractResolver
	{
		private readonly TruncateJsonConverter _defaultTruncateJsonConverter = new TruncateJsonConverter(Consts.PropertyMaxLength);

		protected void CreateProperty(MemberInfo member)
		{
			var maxLengthAttribute = member.GetCustomAttribute<MaxLengthAttribute>();
			if (maxLengthAttribute != null)
			{
				var c = maxLengthAttribute.Length == Consts.PropertyMaxLength
					? _defaultTruncateJsonConverter
					: new TruncateJsonConverter(maxLengthAttribute.Length);
			}
		}
	}
}
