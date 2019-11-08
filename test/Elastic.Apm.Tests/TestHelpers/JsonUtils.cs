using System.Globalization;
using System.IO;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Tests.TestHelpers
{
	public static class JsonUtils
	{
		public static string PrettyFormat(string inputJson)
		{
			using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				using (var jsonTextWriter = new JsonTextWriter(stringWriter))
				{
					jsonTextWriter.Formatting = Formatting.Indented;
					jsonTextWriter.Indentation = TextUtils.IndentationLength;
					jsonTextWriter.IndentChar = TextUtils.IndentationChar;
					JToken.Parse(inputJson).WriteTo(jsonTextWriter);
					return stringWriter.ToString();
				}
			}
		}
	}
}
