// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Feature.Tests
{
	public class PayloadCollector
	{
		private static readonly JsonSerializer Serializer = new();
		private readonly List<Payload> _payloads = new();
		private readonly AutoResetEvent _waitHandle;

		public PayloadCollector() => _waitHandle = new AutoResetEvent(false);

		public IReadOnlyList<Payload> Payloads => _payloads;

		public void ProcessPayload(HttpRequestMessage requestMessage)
		{
			var jObjects = ParseJObjects(requestMessage.Content.ReadAsStringAsync().Result);
			var request = new Payload(jObjects, requestMessage.Headers);
			_payloads.Add(request);
			_waitHandle.Set();
		}

		private static JObject[] ParseJObjects(string json)
		{
			var jObjects = new List<JObject>();
			using var stringReader = new StringReader(json);
			using var jsonReader = new JsonTextReader(stringReader) { SupportMultipleContent = true };
			while (jsonReader.Read())
				jObjects.Add(Serializer.Deserialize<JObject>(jsonReader));

			return jObjects.ToArray();
		}

		public void Wait()
		{
			// wait for the wait handle to be signalled
			var timeout = TimeSpan.FromSeconds(30);
			if (!_waitHandle.WaitOne(timeout))
				throw new Exception($"Did not receive payload within {timeout}");
		}
	}

	public class Payload
	{
		public Payload(JObject[] body, HttpRequestHeaders headers)
		{
			Body = body;
			Headers = headers;
		}

		public HttpRequestHeaders Headers { get; }
		public JObject[] Body { get; }
	}
}
