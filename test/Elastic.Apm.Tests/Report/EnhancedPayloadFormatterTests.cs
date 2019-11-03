using System;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Elastic.Apm.Tests.Report
{
	public class EnhancedPayloadFormatterTests
	{
		private readonly EnhancedPayloadFormatter _formatter;
		private readonly ApmAgent _agent;

		public EnhancedPayloadFormatterTests()
		{
			var logger = new NoopLogger();
			_formatter = new EnhancedPayloadFormatter(new MockConfigSnapshot(), new Metadata());
			_agent = new ApmAgent(new AgentComponents(logger));
		}

		[Fact]
		public void FormatPayload_ShouldCorrectlyFormatTransaction()
		{
			// Arrange
			var transaction = new Model.Transaction(_agent, "transaction", "transaction");

			// Act
			var result = _formatter.FormatPayload(new object[] { transaction });

			// Assert
			var resultLines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

			resultLines.Length.Should().Be(2);
			resultLines[1].Should().Be(JsonConvert.SerializeObject(new {transaction}, _formatter.Settings));
		}
	}
}
