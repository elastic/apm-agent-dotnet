// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Apm.Helpers;


namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// Truncates a string to a given length
	/// </summary>
	internal class TruncateJsonConverter : JsonConverter<string>
	{
		public int MaxLength { get; }

		// ReSharper disable once UnusedMember.Global
		public TruncateJsonConverter() : this(Consts.PropertyMaxLength) { }

		// ReSharper disable once MemberCanBePrivate.Global
		public TruncateJsonConverter(int maxLength) => MaxLength = maxLength;

		public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
			writer.WriteStringValue(value.Truncate(MaxLength));

		public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			reader.GetString();


	}
}
