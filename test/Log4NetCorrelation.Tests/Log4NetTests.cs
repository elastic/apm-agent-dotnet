using Elastic.Apm;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Xunit;

namespace Log4NetCorrelation.Tests
{
	public class Log4NetTests
	{
		/// <summary>
		/// Creates 1 simple transaction and makes sure that the log line created within the transaction has
		/// the transaction and trace ids, and logs prior to and after the transaction do not have those.
		/// </summary>
		[Fact]
		public void Log4NetWithTransaction()
		{
			Agent.Setup(new AgentComponents(payloadSender: new NoopPayloadSender()));
			Agent.SetLogCorrelation(new Elastic.Apm.Log4Net.Log4NetCorrelation());

			var log = LogManager.GetLogger(typeof(Log4NetTests));

			var hierarchy = (Hierarchy)LoggerManager.GetRepository(typeof(Log4NetTests).Assembly);
			var memoryAppender = new MemoryAppender();
			hierarchy.Root.AddAppender(memoryAppender);
			hierarchy.Root.Level = Level.All;
			hierarchy.Configured = true;
			BasicConfigurator.Configure(hierarchy);

			log.Info("PreTransaction");

			string traceId = null;
			string transactionId = null;

			Agent.Tracer.CaptureTransaction("TestTransaction", "Test", t =>
			{
				traceId = t.TraceId;
				transactionId = t.Id;
				log.Info("InTransaction");
			});

			log.Info("PostTransaction.");

			var allEvents = memoryAppender.PopAllEvents();
			allEvents.Length.Should().Be(3);

			allEvents[0].Properties["Trace.Id"].Should().BeNull();
			allEvents[0].Properties["Transaction.Id"].Should().BeNull();

			allEvents[1].Properties["Trace.Id"].Should().Be(traceId);
			allEvents[1].Properties["Transaction.Id"].Should().Be(transactionId);

			allEvents[2].Properties["Trace.Id"].Should().BeNull();
			allEvents[2].Properties["Transaction.Id"].Should().BeNull();
		}
	}
}
