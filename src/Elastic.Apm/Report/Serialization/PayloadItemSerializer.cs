// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Globalization;
using System.IO;
using System.Text;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// Serializes payloads to send to APM server
	/// </summary>
	internal sealed class PayloadItemSerializer
	{
		private readonly JsonSerializer _serializer;

		internal PayloadItemSerializer()
		{
			var settings = new JsonSerializerSettings
			{
				ContractResolver = new ElasticApmContractResolver(),
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.None,
			};

			_serializer = JsonSerializer.CreateDefault(settings);
		}

		public void Serialize(object item, TextWriter writer) => _serializer.Serialize(writer, item);

		/// <summary>
		/// Deserializes an instance of <typeparamref name="T"/> from JSON
		/// </summary>
		/// <param name="json">the JSON</param>
		/// <typeparam name="T">the type to deserialize</typeparam>
		/// <returns>a new instance of <typeparamref name="T"/></returns>
		internal T Deserialize<T>(string json)
		{
			var val = _serializer.Deserialize<T>(new JsonTextReader(new StringReader(json)));
			return val ?? default;
		}

		internal T Deserialize<T>(Stream stream)
		{
			using var sr = new StreamReader(stream);
			using var jsonTextReader = new JsonTextReader(sr);
			var val = _serializer.Deserialize<T>(jsonTextReader);
			return val;
		}

		/// <summary>
		/// Serializes the item to JSON
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		internal string Serialize(object item)
		{
			var builder = new StringBuilder(256);
			using var writer = new StringWriter(builder, CultureInfo.InvariantCulture);
			Serialize(item, writer);
			return builder.ToString();
		}
	}
}
