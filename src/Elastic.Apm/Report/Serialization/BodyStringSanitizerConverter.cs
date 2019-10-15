using System;
using System.Collections.Generic;
using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// Sanitizes request body, in case it's in `Key1=Value1&Key2=Value2` format.
	/// Ideally this would inherit from JsonConverter<string>,
	/// until  https://github.com/elastic/apm-agent-dotnet/issues/555 is not done, we roll with a generic JsonConverter
	/// </summary>
	public class BodyStringSanitizerConverter : JsonConverter
	{
		private readonly IConfigurationReader _configurationReader;

		public BodyStringSanitizerConverter(IConfigurationReader configurationReader)
			=> _configurationReader = configurationReader;

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is string strValue)
			{
				var formValues = strValue.Split('&');

				if(formValues.Length <= 1)
					writer.WriteValue(strValue);
				else
				{
					var sb = new StringBuilder();
					foreach (var formValue in formValues)
					{
						 var formsValueSplit = formValue.Split('=');
						 if(formsValueSplit.Length != 2)
							 continue;

						 if(sb.Length != 0) sb.Append("&");

						 sb.Append(formsValueSplit[0]);
						 sb.Append("=");
						 sb.Append(WildcardMatcher.IsAnyMatch(_configurationReader.SanitizeFieldNames, formsValueSplit[0])
							 ? "[REDACTED]"
							 : formsValueSplit[1]);
					}

					writer.WriteValue(sb.ToString());
				}
			}
			else
				//If the content of the request body is not a string, we serialize it as null
				writer.WriteNull();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
			existingValue.ToString();

		public override bool CanConvert(Type objectType) => objectType == typeof(string);
	}
}
