// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	internal class ElasticApmContractResolver : DefaultContractResolver
	{
		private readonly HeaderDictionarySanitizerConverter _headerDictionarySanitizerConverter;

		public ElasticApmContractResolver(IConfigurationReader configurationReader)
		{
			NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true, OverrideSpecifiedNames = true };
			_headerDictionarySanitizerConverter = new HeaderDictionarySanitizerConverter(configurationReader);
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);

			if (member.MemberType != MemberTypes.Property || !(member is PropertyInfo propInfo)
				|| propInfo.CustomAttributes.All(n => n.AttributeType != typeof(SanitizationAttribute))) return property;

			if (propInfo.PropertyType == typeof(Dictionary<string, string>))
				property.Converter = _headerDictionarySanitizerConverter;

			return property;
		}
	}
}
