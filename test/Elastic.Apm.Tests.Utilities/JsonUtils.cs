// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;

namespace Elastic.Apm.Tests.Utilities
{
	public static class JsonUtils
	{
		public static string PrettyFormat(this string inputJson)
		{
			using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
			using var jsonTextWriter = new JsonTextWriter(stringWriter)
			{
				Formatting = Formatting.Indented,
				Indentation = TextUtils.IndentationLength,
				IndentChar = TextUtils.IndentationChar
			};
			JToken.Parse(inputJson).WriteTo(jsonTextWriter);
			return stringWriter.ToString();
		}
	}
}
