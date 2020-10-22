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

		private readonly TruncateToMaxLengthJsonConverter _defaultTruncateToMaxLengthJsonConverter =
			new TruncateToMaxLengthJsonConverter(Consts.PropertyMaxLength);

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
				var maxLengthAttributes = property.AttributeProvider.GetAttributes(typeof(MaxLengthAttribute), true);
				if (maxLengthAttributes.Count > 0)
				{
					var maxLengthAttribute = (MaxLengthAttribute)maxLengthAttributes[0];
					property.Converter = maxLengthAttribute.Length == Consts.PropertyMaxLength
						? _defaultTruncateToMaxLengthJsonConverter
						: new TruncateToMaxLengthJsonConverter(maxLengthAttribute.Length);
				}
			}

			if (member.MemberType != MemberTypes.Property || !(member is PropertyInfo propInfo)
				|| propInfo.CustomAttributes.All(n => n.AttributeType != typeof(SanitizationAttribute))) return property;

			if (propInfo.PropertyType == typeof(Dictionary<string, string>))
				property.Converter = _headerDictionarySanitizerConverter;

			return property;
		}
	}
}
