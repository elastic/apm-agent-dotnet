using System.Linq;
using Elastic.Apm;
using Elastic.Apm.SerilogEnricher;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Serilog;
using Serilog.Sinks.InMemory;
using Xunit;

namespace LogCorrelation.Tests
{
	public class SerilogTests
	{
		/// <summary>
		/// Creates 1 simple transaction and makes sure that the log line created within the transaction has
		/// the transaction and trace ids, and logs prior to and after the transaction do not have those.
		/// </summary>
		[Fact]
		public void SerilogEnricherWithTransaction()
		{
			Agent.Setup(new AgentComponents(payloadSender: new NoopPayloadSender()));
			var logger = new LoggerConfiguration()
				.Enrich.WithElasticApmTraceId()
				.WriteTo.InMemory()
				.CreateLogger();

			string traceId = null;
			string transactionId = null;

			logger.Information("Line1");

			Agent.Tracer.CaptureTransaction("Test", "Test", ((t) =>
			{
				traceId = t.TraceId;
				transactionId = t.Id;
				logger.Information("Line2");
			}));

			logger.Information("Line2");

			InMemorySink.Instance
				.LogEvents.Should()
				.HaveCount(3);

			InMemorySink.Instance
				.LogEvents.ElementAt(0)
				.Properties.Should()
				.BeEmpty();

			InMemorySink.Instance
				.LogEvents.ElementAt(1)
				.Properties["TraceId"]
				.ToString()
				.Should()
				.Be($"\"{traceId}\"");

			InMemorySink.Instance
				.LogEvents.ElementAt(1)
				.Properties["TransactionId"]
				.ToString()
				.Should()
				.Be($"\"{transactionId}\"");

			InMemorySink.Instance
				.LogEvents.ElementAt(2)
				.Properties.Should()
				.BeEmpty();
		}
	}
}
