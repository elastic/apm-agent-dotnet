using System;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	/// <inheritdoc />
	/// <summary>
	/// Automatically applies <see cref="StringValueProvider"/> to <see cref="String"/> properties, unless they are marked with
	/// <see cref="NoTruncationInJsonNetAttribute"/>.
	/// </summary>
	internal class StringTruncationValueResolver : CamelCasePropertyNamesContractResolver
	{
		protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
		{
			var property = base.CreateProperty(member, memberSerialization);

			if (property.PropertyType == typeof(string) && member.CustomAttributes.All(n => n.AttributeType != typeof(NoTruncationInJsonNetAttribute)))
			{
				property.ValueProvider = new StringValueProvider(property.ValueProvider);
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
}
