using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	public class ElasticApmContractResolver : DefaultContractResolver
	{
		private readonly HeaderDictionarySanitizerConverter _headerDictionarySanitizerConverter;
		private readonly BodyStringSanitizerConverter _bodyStringSanitizerConverter;

		public ElasticApmContractResolver(IConfigurationReader configurationReader)
		{
			NamingStrategy = new CamelCaseNamingStrategy { ProcessDictionaryKeys = true, OverrideSpecifiedNames = true };
			_headerDictionarySanitizerConverter = new HeaderDictionarySanitizerConverter(configurationReader);
			_bodyStringSanitizerConverter = new BodyStringSanitizerConverter(configurationReader);
		}

		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);

			if (member.MemberType != MemberTypes.Property || !(member is PropertyInfo propInfo)
				|| propInfo.CustomAttributes.All(n => n.AttributeType != typeof(SanitizationAttribute))) return property;

			if (propInfo.PropertyType == typeof(Dictionary<string, string>))
				property.Converter = _headerDictionarySanitizerConverter;
			// Currently Request.Body is an object, which makes asserting on the type harder.
			// Once https://github.com/elastic/apm-agent-dotnet/issues/555 is done this can be changed
			if (propInfo.Name == nameof(Request.Body))
				property.Converter = _bodyStringSanitizerConverter;

			return property;
		}
	}
}
