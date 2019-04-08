using Elastic.Apm.DistributedTracing;
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

			var res = TraceParent.TryExtractTraceparent(traceParent, out var traceId, out var parentId, out var traceOptions);
			res.Should().BeTrue();
			traceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			parentId.Should().Be("b7ad6b7169203331");
			TraceParent.IsFlagRecordedActive(traceOptions).Should().BeTrue();

			//try also with flag options C6
			const string traceParent2 = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-E7";
			TraceParent.TryExtractTraceparent(traceParent2, out _, out _, out var traceOptions2);
			TraceParent.IsFlagRecordedActive(traceOptions2).Should().BeTrue();
		}

		[Fact]
		public void ParseValidTraceParentNotRecorded()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";

			TraceParent.TryExtractTraceparent(traceParent, out var traceId, out var parentId, out var traceOptions);

			traceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			parentId.Should().Be("b7ad6b7169203331");
			TraceParent.IsFlagRecordedActive(traceOptions).Should().BeFalse();


			//try also with flag options C6
			const string traceParent2 = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-C6";
			TraceParent.TryExtractTraceparent(traceParent2, out _, out _, out var traceOptions2);
			TraceParent.IsFlagRecordedActive(traceOptions2).Should().BeFalse();
		}

		[Fact]
		public void ValidateTraceParentWithFutureVersion()
		{
			const string traceParent = "99-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			//Best attempt, even if it's a future version we still try to read the traceId and parentId
			var res = TraceParent.TryExtractTraceparent(traceParent, out var traceId, out var parentId, out var traceOptions);
			traceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			parentId.Should().Be("b7ad6b7169203331");
			TraceParent.IsFlagRecordedActive(traceOptions).Should().BeTrue();
			res.Should().BeTrue();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidLength()
		{
			const string traceParent = "99-af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"; //TraceId is 1 char shorter than expected
			var res = TraceParent.TryExtractTraceparent(traceParent, out _, out _, out _);
			res.Should().BeFalse();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidTraceIdLength()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319ca-7ad6b7169203331-01"; //TraceId is 1 char longer than expected, and parentId is 1 char longer
			var res = TraceParent.TryExtractTraceparent(traceParent, out _, out _, out _);
			res.Should().BeFalse();
		}

		[Fact]
		public void TraceParentHeaderName()
		{
			//currently we use a non-standard header name - awaiting trace context to became a standard.
			//this will be renamed in the future.
			TraceParent.TraceParentHeaderName.Should().Be("elastic-apm-traceparent");
		}
	}
}
