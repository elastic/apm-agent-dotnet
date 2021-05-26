// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.DistributedTracing;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.DistributedTracing
{
	public class DistributedTracingTests
	{
		[Fact]
		public void Valid_TraceParent_Recorded_Should_Be_Parsed()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var res = TraceContext.TryExtractTracingData(traceParent);
			res.Should().NotBeNull();
			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeTrue();
			res.HasTraceState.Should().BeFalse();

			//try also with flag options C6
			const string traceParent2 = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-E7";
			var res2 = TraceContext.TryExtractTracingData(traceParent2);
			res2.FlagRecorded.Should().BeTrue();
		}

		[Fact]
		public void Valid_TraceParent_Recorded_And_TraceState_Should_Be_Parsed()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";
			var traceState = new[]
			{
				"es=s:0.5555",
				"aa=foo:bar:baz",
				"bb=quux"
			};

			var res = TraceContext.TryExtractTracingData(traceParent, string.Join(",", traceState));
			res.Should().NotBeNull();
			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeTrue();

			res.HasTraceState.Should().BeTrue();
			res.TraceState.SampleRate.Should().Be(0.5555);
			res.TraceState.ToTextHeader().Should().Be("es=s:0.5555,aa=foo:bar:baz,bb=quux");
		}

		[Fact]
		public void ParseValidTraceParentNotRecorded()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";

			var res = TraceContext.TryExtractTracingData(traceParent);

			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeFalse();


			//try also with flag options C6
			const string traceParent2 = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-C6";
			var res2 = TraceContext.TryExtractTracingData(traceParent2);
			res2.FlagRecorded.Should().BeFalse();
		}

		[Fact]
		public void ValidateTraceParentWithFutureVersion()
		{
			const string traceParent = "99-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			//Best attempt, even if it's a future version we still try to read the traceId and parentId
			var res = TraceContext.TryExtractTracingData(traceParent);
			res.Should().NotBeNull();
			res.TraceId.Should().Be("0af7651916cd43dd8448eb211c80319c");
			res.ParentId.Should().Be("b7ad6b7169203331");
			res.FlagRecorded.Should().BeTrue();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidLength()
		{
			const string traceParent = "99-af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"; //TraceId is 1 char shorter than expected
			var res = TraceContext.TryExtractTracingData(traceParent);
			res.Should().BeNull();
		}

		[Fact]
		public void ValidateTraceParentWithInvalidTraceIdLength()
		{
			const string
				traceParent =
					"00-0af7651916cd43dd8448eb211c80319ca-7ad6b7169203331-01"; //TraceId is 1 char longer than expected, and parentId is 1 char longer
			var res = TraceContext.TryExtractTracingData(traceParent);
			res.Should().BeNull();
		}

		/// <summary>
		/// Currently we use a non-standard header name - awaiting trace context to became a standard.
		/// this will be renamed in the future.
		/// </summary>
		[Fact]
		public void TraceParentHeaderName() =>
			TraceContext.TraceParentHeaderNamePrefixed.Should().Be("elastic-apm-traceparent");
	}
}
