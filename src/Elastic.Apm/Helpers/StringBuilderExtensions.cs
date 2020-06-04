// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class StringBuilderExtensions
	{
		internal static bool IsEmpty(this StringBuilder stringBuilder) => stringBuilder.Length == 0;

		internal static StringBuilder AppendSeparatedIfNotEmpty(this StringBuilder stringBuilder, string separator, string stringToAppend)
		{
			if (!stringBuilder.IsEmpty()) stringBuilder.Append(separator);
			return stringBuilder.Append(stringToAppend);
		}
	}
}
