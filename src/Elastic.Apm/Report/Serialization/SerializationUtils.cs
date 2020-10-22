// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;

namespace Elastic.Apm.Report.Serialization
{
	internal static class SerializationUtils
	{
		/// <summary>
		/// Truncates the string to a given length, if longer than the length
		/// </summary>
		internal static string Truncate(string input, int length = Consts.PropertyMaxLength)
		{
			input.ThrowIfArgumentNull(nameof(input));

			if (input.Length <= length) return input;

			if (length <= 5) return input.Substring(0, length);

			return $"{input.Substring(0, length - 3)}...";
		}
	}
}
