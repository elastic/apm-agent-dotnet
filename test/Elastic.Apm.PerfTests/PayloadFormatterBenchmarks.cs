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
			_newPayloadFormatter = new EnhancedPayloadFormatter(logger, apmAgent.ConfigurationReader, metadata);

			var transaction = new Elastic.Apm.Model.Transaction(apmAgent, "transaction", "transaction");
			var span = new Span("span", "type", transaction.Id, transaction.TraceId, transaction, apmAgent.PayloadSender, logger,
				apmAgent.ConfigurationReader, apmAgent.TracerInternal.CurrentExecutionSegmentsContainer);
			var error = new Error(new CapturedException(), transaction, span.Id, logger);
			var metricSet = new MetricSet(TimeUtils.TimestampNow(), new List<MetricSample> { new MetricSample("key", 25.0) });

			_items = new object[] { transaction, span, error, metricSet };
		}

		[Benchmark]
		public void PayloadFormatterV2()
		{
			for (var i = 0; i <= CallAmount; i++)
			{
				_oldPayloadFormatter.FormatPayload(_items);
			}
		}

		[Benchmark]
		public void EnhancedPayloadFormatter()
		{
			for (var i = 0; i <= CallAmount; i++)
			{
				_newPayloadFormatter.FormatPayload(_items);
			}
		}
	}
}
