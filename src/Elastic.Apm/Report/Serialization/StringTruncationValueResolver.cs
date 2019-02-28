using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	/// <inheritdoc />
	/// <summary>
	/// Automatically applies <see cref="StringValueProvider"/> to <see cref="String"/> properties, and <see cref="StringDictionaryValueProvider"/>
	/// to <see cref="Dictionary{String, String}"/> unless they are marked with <see cref="NoTruncationInJsonNetAttribute"/>.
	/// </summary>
	internal class StringTruncationValueResolver : CamelCasePropertyNamesContractResolver
	{
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);
			var propertyInfo = member as PropertyInfo;

			if (propertyInfo == null)
				return property;

			switch (propertyInfo)
			{
				case PropertyInfo propInfo when propInfo.PropertyType == typeof(string) &&
					member.CustomAttributes.All(n => n.AttributeType != typeof(NoTruncationInJsonNetAttribute)):
					property.ValueProvider = new StringValueProvider(property.ValueProvider);
					break;
				case PropertyInfo propInfo when propInfo.PropertyType == typeof(Dictionary<string, string>)
					&& member.CustomAttributes.All(n => n.AttributeType != typeof(NoTruncationInJsonNetAttribute)):
					property.ValueProvider = new StringDictionaryValueProvider(property.ValueProvider);
					break;
			}
			return property;
		}
	}

	/// <inheritdoc />
	/// <summary>
	/// Trims stings by <see cref="StringExtensions.TrimToMaxLength"/>
	/// </summary>
	internal class StringValueProvider : IValueProvider
	{
		private readonly IValueProvider _provider;

		public StringValueProvider(IValueProvider provider) => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

		public void SetValue(object target, object value) => _provider.SetValue(target, value);

		public object GetValue(object target)
		{
			var value = _provider.GetValue(target);
			var strValue = (value as string);
			return !string.IsNullOrEmpty(strValue) ? strValue.TrimToMaxLength() : value;
		}
	}

	/// <inheritdoc />
	/// <summary>
	/// Trims stings within a <see cref="Dictionary{String, String}"/> that are stored as value by <see cref="StringExtensions.TrimToMaxLength"/>
	/// </summary>
	internal class StringDictionaryValueProvider : IValueProvider
	{
		private readonly IValueProvider _provider;

		public StringDictionaryValueProvider(IValueProvider provider) => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

		public void SetValue(object target, object value) => _provider.SetValue(target, value);

		public object GetValue(object target)
		{
			var value = _provider.GetValue(target);

			if (!(value is Dictionary<string, string> dictionary))
				return value;

			foreach (var val in dictionary.ToList())
			{
				dictionary[val.Key] = val.Value.TrimToMaxLength();
			}

			return dictionary;
		}
	}
}
