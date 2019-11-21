using System;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// Sanitizes request body based on the config passed to the constructor, in case it's in `Key1=Value1& Key2=Value2` format.
	/// Ideally this would inherit from <code> JsonConverter{string} </code>.
	/// Until https://github.com/elastic/apm-agent-dotnet/issues/555 is not done, we roll with a generic JsonConverter.
	/// </summary>
	internal class BodyStringSanitizerConverter : JsonConverter
	{
		private readonly IConfigurationReader _configurationReader;

		public BodyStringSanitizerConverter(IConfigurationReader configurationReader)
			=> _configurationReader = configurationReader;

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is string strValue)
			{
				if (!FormatCheck(strValue))
					writer.WriteValue(strValue);
				else
				{
					var formValues = strValue.Split('&');

					var sb = new StringBuilder();
					foreach (var formValue in formValues)
					{
						var formsValueSplit = formValue.Split('=');
						if (formsValueSplit.Length != 2)
							continue;

						if (sb.Length != 0) sb.Append("&");

						sb.Append(formsValueSplit[0]);
						sb.Append("=");
						sb.Append(WildcardMatcher.IsAnyMatch(_configurationReader.SanitizeFieldNames, formsValueSplit[0])
							? Consts.Redacted
							: formsValueSplit[1]);
					}

					writer.WriteValue(sb.ToString());
				}
			}
			else
				//If the content of the request body is not a string, we serialize it as null
				writer.WriteNull();
		}

		/// <summary>
		/// Returns <code>true</code> if <paramref name="bodyValue"/> follows the Key1=Value1& Key2=Value2 format, <code>false</code> otherwise
		/// </summary>
		/// <param name="bodyValue"></param>
		/// <returns></returns>
		private static bool FormatCheck(string bodyValue)
		{
			if (!bodyValue.Contains("=") && !bodyValue.Contains("&"))
				return false;

			var numberOfEqual = 0;
			var numberOfAnds = 0;

			var i = 0;
			foreach (var c in bodyValue)
			{
				if (c == '=')
				{
					numberOfEqual++;
					i++;
					if (i > 1)
						return false;
				}
				if (c == '&')
				{
					numberOfAnds++;
					i--;
					if (i < 0)
						return false;
				}
			}

			return numberOfAnds != 0 || numberOfEqual != 0;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
			existingValue.ToString();

		public override bool CanConvert(Type objectType) => objectType == typeof(string);
	}
}
