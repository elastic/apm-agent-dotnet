using System.Linq;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class DistributedTracingTests
	{

		[Fact]
		public void ParseValidTraceParentRecorded()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var(parent, traceId, isRecorded) = TraceParent.ParseTraceParentString(traceParent);

			parent.Should().Be("0af7651916cd43dd8448eb211c80319c");
			traceId.Should().Be("b7ad6b7169203331");
			isRecorded.Should().BeTrue();
		}

		[Fact]
		public void ParseValidTraceParentNotRecorded()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";

			var(parent, traceId, isRecorded) = TraceParent.ParseTraceParentString(traceParent);

			parent.Should().Be("0af7651916cd43dd8448eb211c80319c");
			traceId.Should().Be("b7ad6b7169203331");
			isRecorded.Should().BeFalse();
		}

		[Fact]
		public void ValidateValidTraceParent()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var testLogger = new TestLogger();
			var isValid = TraceParent.ValidateTraceParentValue(traceParent, testLogger);
			isValid.Should().BeTrue();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidVersion()
		{
			const string traceParent = "99-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var testLogger = new TestLogger(LogLevel.Warning);
			var isValid = TraceParent.ValidateTraceParentValue(traceParent, testLogger);
			isValid.Should().BeFalse();
			testLogger.Lines.First().Should().Contain("Only version 00 of the traceparent header is supported, but was 99-");
		}

		[Fact]
		public void ValidateTraceParentWithInvalidLength()
		{
			const string traceParent = "99-af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"; //TraceId is 1 char shorter than expected

			var testLogger = new TestLogger(LogLevel.Warning);
			var isValid = TraceParent.ValidateTraceParentValue(traceParent, testLogger);
			isValid.Should().BeFalse();
			testLogger.Lines.First().Should().Contain("Traceparent contains invalid length, expected: 55, got: 54");
		}

		[Fact]
		public void ValidateTraceParentWithInvalidTraceIdLength()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319ca-7ad6b7169203331-01"; //TraceId is 1 char longer than expected, and parentId is 1 char longer

			var testLogger = new TestLogger(LogLevel.Warning);
			var isValid = TraceParent.ValidateTraceParentValue(traceParent, testLogger);
			isValid.Should().BeFalse();
			testLogger.Lines.First().Should().Contain("Invalid traceparent format, got: 00-0af7651916cd43dd8448eb211c80319ca-7ad6b7169203331-01");
		}
	}
}
