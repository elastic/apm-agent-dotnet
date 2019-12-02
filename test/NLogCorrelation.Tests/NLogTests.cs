using Elastic.Apm;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using NLog;
using NLog.Targets;
using Xunit;

namespace NLogCorrelation.Tests
{
	public class NLogTests
	{
		/// <summary>
		/// Creates 1 simple transaction and makes sure that the log line created within the transaction has
		/// the transaction and trace ids, and logs prior to and after the transaction do not have those.
		/// </summary>
		[Fact]
		public void NLogWithTransaction()
		{
			Agent.Setup(new AgentComponents(payloadSender: new NoopPayloadSender()));
			Agent.SetLogCorrelation(new Elastic.Apm.NLog.NLogCorrelation());

			var target = new MemoryTarget();
			target.Layout = "${mdlc:item=Trace.Id}|${mdlc:item=Transaction.Id}|${message}";

			NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, LogLevel.Debug);
			var logger = LogManager.GetLogger("Example");

			logger.Debug("PreTransaction");

			string traceId = null;
			string transactionId = null;

			Agent.Tracer.CaptureTransaction("TestTransaction", "Test", t =>
			{
				traceId = t.TraceId;
				transactionId = t.Id;
				logger.Debug("InTransaction");
			});

			logger.Debug("PostTransaction");

			target.Logs.Count.Should().Be(3);

			target.Logs[0].Should().Be("||PreTransaction");
			target.Logs[1].Should().Be($"{traceId}|{transactionId}|InTransaction");
			target.Logs[2].Should().Be("||PostTransaction");
		}
	}
}
