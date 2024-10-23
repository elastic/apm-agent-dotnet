// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Model;


namespace Elastic.Apm.Report.Serialization;

[JsonSerializable(typeof(IntakeError))]
[JsonSerializable(typeof(IntakeResponse))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

/// <summary>
/// Serializes payloads to send to APM server
/// </summary>
internal sealed class PayloadItemSerializer
{
	public JsonSerializerOptions Settings { get; }

	internal PayloadItemSerializer() =>
		Settings = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false,
			Converters = { new JsonConverterDouble(), new JsonConverterDecimal() },
			TypeInfoResolver = JsonTypeInfoResolver.Combine(SourceGenerationContext.Default, new DefaultJsonTypeInfoResolver
			{
				Modifiers = { j =>
					{
						foreach (var prop in j.Properties)
						{
							var maxLengthAttribute = prop.AttributeProvider.GetCustomAttributes(typeof(MaxLengthAttribute), false).FirstOrDefault() as MaxLengthAttribute;
							if (maxLengthAttribute != null)
								prop.CustomConverter = new TruncateJsonConverter(maxLengthAttribute.Length);

							if (prop.PropertyType == typeof(Lazy<Context>))
								prop.ShouldSerialize = (_, lazy) => (lazy as Lazy<Context>)?.IsValueCreated ?? false;

							else if ((j.Type == typeof(Transaction) || j.Type == typeof(ITransaction)) && prop.Name == "context")
								prop.ShouldSerialize = (t, _) => (t as Transaction)?.ShouldSerializeContext() ?? false;

							else if ((j.Type == typeof(Span) || j.Type == typeof(ISpan)) && prop.Name == "context")
								prop.ShouldSerialize = (s, _) => (s as Span)?.ShouldSerializeContext() ?? false;

							else if ((j.Type == typeof(Error) || j.Type == typeof(IError)) && prop.Name == "context")
								prop.ShouldSerialize = (e, _) => (e as Error)?.ShouldSerializeContext() ?? false;

							else if (j.Type == typeof(Context) && prop.Name == "tags")
								prop.ShouldSerialize = (e, _) => (e as Context)?.ShouldSerializeLabels() ?? false;

							else if (j.Type == typeof(Context) && prop.Name == "custom")
								prop.ShouldSerialize = (e, _) => (e as Context)?.ShouldSerializeCustom() ?? false;

							else if (j.Type == typeof(SpanContext) && prop.Name == "tags")
								prop.ShouldSerialize = (e, _) => (e as SpanContext)?.ShouldSerializeLabels() ?? false;

							else if (j.Type == typeof(Metadata) && prop.Name == "tags")
								prop.ShouldSerialize = (e, _) => (e as Metadata)?.ShouldSerializeLabels() ?? false;

							else if (prop.PropertyType.GetInterfaces().Any(i => i == typeof(IDictionary)))
								prop.ShouldSerialize = (_, dictionary) => ((dictionary as IDictionary)?.Count ?? 0) > 0;

						}
					}
				}
			}),
		};

	public static PayloadItemSerializer Default { get; } = new();

	public JsonTypeInfo GetTypeInfo(Type type) => Settings.TypeInfoResolver!.GetTypeInfo(type, Settings);

	public void Serialize(object item, StreamWriter writer) =>
		JsonSerializer.Serialize(writer.BaseStream, item, item.GetType(), Settings);

	/// <summary>
	/// Deserializes an instance of <typeparamref name="T"/> from JSON
	/// </summary>
	/// <param name="json">the JSON</param>
	/// <typeparam name="T">the type to deserialize</typeparam>
	/// <returns>a new instance of <typeparamref name="T"/></returns>
	internal T Deserialize<T>(string json)
	{
		var val = JsonSerializer.Deserialize<T>(json, Settings);
		return val ?? default;
	}

	internal T Deserialize<T>(Stream stream)
	{
		var val = JsonSerializer.Deserialize<T>(stream, Settings);
		return val;
	}

	/// <summary>
	/// Serializes the item to JSON
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	internal string Serialize(object item)
	{
		var bytes = JsonSerializer.SerializeToUtf8Bytes(item, item.GetType(), Settings);
		return Encoding.UTF8.GetString(bytes);
	}
}
