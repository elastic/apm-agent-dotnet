// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;

namespace Elastic.Apm.Report.Serialization
{
	internal static class SerializationUtils
	{
		internal static string TrimToLength(string input, int maxLength)
		{
			input.ThrowIfArgumentNull(nameof(input));

			if (input.Length <= maxLength) return input;

			if (maxLength <= 5) return input.Substring(0, maxLength);

			return $"{input.Substring(0, maxLength - 3)}...";
		}

		internal static string TrimToPropertyMaxLength(string input) => TrimToLength(input, Consts.PropertyMaxLength);
	}
}
