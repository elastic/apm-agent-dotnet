using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	public class StringTruncationValueResolver : CamelCasePropertyNamesContractResolver
	{
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);

			if (property.PropertyType == typeof(string) && member.CustomAttributes.All(n => n.AttributeType != typeof(NoTruncationAttribute)))
			{
				// Wrap value provider supplied by Json.NET.
				property.ValueProvider = new StringValueProvider(property.ValueProvider);
			}
			return property;
		}
	}

	public class StringValueProvider : IValueProvider
	{
		private readonly IValueProvider _provider;

		public StringValueProvider(IValueProvider provider) => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

		// SetValue gets called by Json.Net during deserialization.
		// The value parameter has the original value read from the JSON;
		// target is the object on which to set the value.
		public void SetValue(object target, object value) => _provider.SetValue(target, value);

		// GetValue is called by Json.Net during serialization.
		// The target parameter has the object from which to read the value;
		// the return value is what gets written to the JSON
		public object GetValue(object target)
		{
			var value = _provider.GetValue(target);
			var strValue = (value as string);
			return "bbb"; // !string.IsNullOrEmpty(strValue) ? strValue.Trim() : value;
		}
	}
}
