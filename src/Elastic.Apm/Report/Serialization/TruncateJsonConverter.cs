// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// Truncates a string to a given length
	/// </summary>
	internal class TruncateJsonConverter : JsonConverter<string>
	{
		private readonly int _maxLength;

		// ReSharper disable once UnusedMember.Global
		public TruncateJsonConverter() : this(Consts.PropertyMaxLength) { }

		// ReSharper disable once MemberCanBePrivate.Global
		public TruncateJsonConverter(int maxLength) => _maxLength = maxLength;

		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) =>
			writer.WriteValue(value.Truncate(_maxLength));

		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) =>
			reader.Value as string;
	}
}
