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
		private AbstractLogger _logger;

		public void QueueError(IError error) { }

		public void QueuePayload(IPayload payload) { }

		private readonly JsonSerializerSettings _settings;

		private BlockingCollection<ITransaction> _transactions = new BlockingCollection<ITransaction>();

		public void QueueTransaction(ITransaction transaction)
		{
			_transactions.Add(transaction);
		}

		public PayloadSenderV2(AbstractLogger logger, IConfigurationReader configurationReader, HttpMessageHandler handler = null)
		{
			_settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None};
			_configurationReader = configurationReader;
			_logger = logger;
			Thread t = new Thread(DoWork);
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

				var item = (_transactions.Take()  as Transaction);

				if(item == null)
					continue;

				var metadata = new Metadata { Service = item.Service };

				var metadataJson = JsonConvert.SerializeObject(metadata, _settings);
				var serviceJson = JsonConvert.SerializeObject(item, _settings);

//				Console.WriteLine(json);
//
//				var sb = new StringBuilder();
//				using (var textWriter = new StringWriter(sb))
//				{
//					JsonExtensions.ToNewlineDelimitedJson(textWriter, new List<String> {json} );
//				}
//
//				var ndjson = sb.ToString();

				var json = "{\"metadata\": " + metadataJson + "}" + "\n" + "{\"transaction\": " + serviceJson + "}";
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
