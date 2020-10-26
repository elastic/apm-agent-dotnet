// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	internal class ElasticApmContractResolver : DefaultContractResolver
	{
		private readonly HeaderDictionarySanitizerConverter _headerDictionarySanitizerConverter;

		private readonly TruncateJsonConverter _defaultTruncateJsonConverter =
			new TruncateJsonConverter(Consts.PropertyMaxLength);

		public ElasticApmContractResolver(IConfigurationReader configurationReader)
		{
			NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true, OverrideSpecifiedNames = true };
			_headerDictionarySanitizerConverter = new HeaderDictionarySanitizerConverter(configurationReader);
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);

			if (property.PropertyType == typeof(string))
			{
				var maxLengthAttribute = member.GetCustomAttribute<MaxLengthAttribute>();
				if (maxLengthAttribute != null)
				{
					property.Converter = maxLengthAttribute.Length == Consts.PropertyMaxLength
						? _defaultTruncateJsonConverter
						: new TruncateJsonConverter(maxLengthAttribute.Length);
				}
			}
			else if (property.PropertyType == typeof(Dictionary<string, string>))
			{
				var sanitizeAttribute = member.GetCustomAttribute<SanitizeAttribute>();
				if (sanitizeAttribute != null)
					property.Converter = _headerDictionarySanitizerConverter;
			}

			return property;
		}
	}
}
