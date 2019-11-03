using System.Collections.Generic;
using System.Net;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class PayloadFormatterBenchmarks
	{
		private IPayloadFormatter _oldPayloadFormatter;
		private IPayloadFormatter _newPayloadFormatter;
		private object[] _items;

		[Params(1, 10, 100)]
		public int CallAmount;

		[GlobalSetup]
		public void Setup()
		{
			var logger = new PerfTestLogger(LogLevel.Critical);
			var apmAgent = new ApmAgent(new AgentComponents(logger));

			var metadata = new Metadata
			{
				Service = Service.GetDefaultService(apmAgent.ConfigurationReader, logger),
				System = new Api.System { DetectedHostName = Dns.GetHostName() }
			};

			_oldPayloadFormatter = new PayloadFormatterV2(logger, apmAgent.ConfigurationReader, metadata);
			_newPayloadFormatter = new EnhancedPayloadFormatter(apmAgent.ConfigurationReader, metadata);

			var transaction = new Transaction(apmAgent, "transaction", "transaction");
			var span = new Span("span", "type", transaction.Id, transaction.TraceId, transaction, apmAgent.PayloadSender, logger,
				apmAgent.ConfigurationReader, apmAgent.TracerInternal.CurrentExecutionSegmentsContainer);
			var error = new Error(new CapturedException(), transaction, span.Id, logger);
			var metricSet = new MetricSet(TimeUtils.TimestampNow(), new List<MetricSample> { new MetricSample("key", 25.0) });

			_items = new object[4 * CallAmount];
			for (var i = 0; i < CallAmount; i++)
			{
				_items[0 + 4 * i] = transaction;
				_items[1 + 4 * i] = span;
				_items[2 + 4 * i] = error;
				_items[3 + 4 * i] = metricSet;
			}
		}

		[Benchmark]
		public string PayloadFormatterV2() => _oldPayloadFormatter.FormatPayload(_items);

		[Benchmark]
		public string EnhancedPayloadFormatter() => _newPayloadFormatter.FormatPayload(_items);
	}
}
