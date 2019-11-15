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

			var res = TraceParent.TryExtractTraceparent(traceParent);
			res.Should().NotBeNull();
			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeTrue();

			//try also with flag options C6
			const string traceParent2 = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-E7";
			var res2 = TraceParent.TryExtractTraceparent(traceParent2);
			res2.FlagRecorded.Should().BeTrue();
		}

		[Fact]
		public void ParseValidTraceParentNotRecorded()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";

			var res = TraceParent.TryExtractTraceparent(traceParent);

			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeFalse();


			//try also with flag options C6
			const string traceParent2 = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-C6";
			var res2 = TraceParent.TryExtractTraceparent(traceParent2);
			res2.FlagRecorded.Should().BeFalse();
		}

		[Fact]
		public void ValidateTraceParentWithFutureVersion()
		{
			const string traceParent = "99-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			//Best attempt, even if it's a future version we still try to read the traceId and parentId
			var res = TraceParent.TryExtractTraceparent(traceParent);
			res.Should().NotBeNull();
			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeTrue();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidLength()
		{
			const string traceParent = "99-af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"; //TraceId is 1 char shorter than expected
			var res = TraceParent.TryExtractTraceparent(traceParent);
			res.Should().BeNull();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidTraceIdLength()
		{
			const string
				traceParent =
					"00-0af7651916cd43dd8448eb211c80319ca-7ad6b7169203331-01"; //TraceId is 1 char longer than expected, and parentId is 1 char longer
			var res = TraceParent.TryExtractTraceparent(traceParent);
			res.Should().BeNull();
		}

		/// <summary>
		/// Currently we use a non-standard header name - awaiting trace context to became a standard.
		/// this will be renamed in the future.
		/// </summary>
		[Fact]
		public void TraceParentHeaderName() =>
			TraceParent.TraceParentHeaderName.Should().Be("elastic-apm-traceparent");
	}
}
