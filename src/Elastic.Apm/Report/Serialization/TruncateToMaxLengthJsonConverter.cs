// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class TruncateToMaxLengthJsonConverter : JsonConverter<string>
	{
		private readonly int _maxLength;

		// ReSharper disable once UnusedMember.Global
		public TruncateToMaxLengthJsonConverter() : this(Consts.PropertyMaxLength) { }

		// ReSharper disable once MemberCanBePrivate.Global
		public TruncateToMaxLengthJsonConverter(int maxLength) => _maxLength = maxLength;

		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) =>
			writer.WriteValue(SerializationUtils.TrimToLength(value, _maxLength));

		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) =>
			reader.Value as string;
	}
}
