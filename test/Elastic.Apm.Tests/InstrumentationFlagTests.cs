using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class InstrumentationFlagTests
	{
		/// <summary>
		/// Makes sure that <see cref="Transaction.StartSpanInternal"/> passes the instrumentation flag to the created span
		/// </summary>
		[Fact]
		public void SpanOnTransactionWithSpecificInstrumentationFlag()
		{
			using var agent = new ApmAgent(new TestAgentComponents());
			var transaction = agent.TracerInternal.StartTransactionInternal("test", "test");

			var span1 = transaction.StartSpanInternal("span", "test", instrumentationFlag: InstrumentationFlag.SqlClient);

			span1.InstrumentationFlag.Should().Be(InstrumentationFlag.SqlClient);
			span1.End();
			transaction.End();
		}

		/// <summary>
		/// Makes sure that <see cref="Span.StartSpanInternal"/> passes the instrumentation flag to the created child span
		/// </summary>
		[Fact]
		public void ChildSpanWithSpecificInstrumentationFlag()
		{
			using var agent = new ApmAgent(new TestAgentComponents());
			var transaction = agent.TracerInternal.StartTransactionInternal("test", "test");

			var span1 = transaction.StartSpanInternal("span", "test", instrumentationFlag: InstrumentationFlag.AspNetCore);
			span1.InstrumentationFlag.Should().Be(InstrumentationFlag.AspNetCore);
			var span2 = span1.StartSpanInternal("span", "test", instrumentationFlag: InstrumentationFlag.EfClassic);
			span2.InstrumentationFlag.Should().Be(InstrumentationFlag.EfClassic);
			span2.End();
			span1.End();
			transaction.End();
		}
	}
}
