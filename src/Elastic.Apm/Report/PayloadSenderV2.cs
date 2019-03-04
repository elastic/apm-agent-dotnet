using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report
{
	public class PayloadSenderV2 : IPayloadSender
	{
		private IConfigurationReader _configurationReader;
		private IApmLogger _logger;
		private readonly Service _service;

		public void QueueError(IError error) { }

		public void QueuePayload(IPayload payload) { }

		private readonly JsonSerializerSettings _settings;

		private readonly BlockingCollection<object> _eventQueue = new BlockingCollection<object>();

		public void QueueTransaction(ITransaction transaction)
		{
			_eventQueue.Add(transaction);
		}

		public void QueueSpan(ISpan span) => _eventQueue.Add(span);

		public PayloadSenderV2(IApmLogger logger, IConfigurationReader configurationReader, Service service, HttpMessageHandler handler = null)
		{
			_settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None };
			_configurationReader = configurationReader;
			_service = service;
			_logger = logger;
			var t = new Thread(DoWork);
			t.Start();
		}

		private void DoWork()
		{
			var httpClient = new HttpClient()
			{
				BaseAddress = _configurationReader.ServerUrls.First()
			};

			while (true)
			{
				var item = (_eventQueue.Take());

				if (item == null)
					continue;

				var itemJson = JsonConvert.SerializeObject(item, _settings);

				var json = "";
				var metadata = new Metadata { Service = _service };
				var metadataJson = JsonConvert.SerializeObject(metadata, _settings);
				json = "{\"metadata\": " + metadataJson + "}" + "\n";

				switch (item)
				{
					case Transaction t:
						json += "{\"transaction\": " + itemJson + "}";
						break;
					case Span s:
						json += "{\"span\": " + itemJson + "}";
						break;
				}

				Console.WriteLine(json);

				var content = new StringContent(json, Encoding.UTF8, "application/x-ndjson");

				try
				{
					var result = httpClient.PostAsync(Consts.IntakeV2Events, content);
				}
				catch (Exception e)
				{
					_logger.LogDebug($"Failed sending data to the server, {e.GetType()} - {e.Message}");
				}
			}
			// ReSharper disable once FunctionNeverReturns
		}
	}

	class Metadata
	{
		public Service Service { get; set; }
	}

	public static partial class JsonExtensions
	{
		public static void ToNewlineDelimitedJson<T>(Stream stream, IEnumerable<T> items)
		{
			// Let caller dispose the underlying stream
			using (var textWriter = new StreamWriter(stream, new UTF8Encoding(false, true), 1024, true))
			{
				ToNewlineDelimitedJson(textWriter, items);
			}
		}

		public static void ToNewlineDelimitedJson<T>(TextWriter textWriter, IEnumerable<T> items)
		{
			var serializer = JsonSerializer.CreateDefault();

			foreach (var item in items)
			{
				// Formatting.None is the default; I set it here for clarity.
				using (var writer = new JsonTextWriter(textWriter) { Formatting = Formatting.None, CloseOutput = false })
				{
					serializer.Serialize(writer, item);
				}
				// http://specs.okfnlabs.org/ndjson/
				// Each JSON text MUST conform to the [RFC7159] standard and MUST be written to the stream followed by the newline character \n (0x0A).
				// The newline charater MAY be preceeded by a carriage return \r (0x0D). The JSON texts MUST NOT contain newlines or carriage returns.
				textWriter.Write("\n");
			}
		}
	}
}
