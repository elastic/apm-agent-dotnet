using Elastic.Apm.DistributedTracing;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class DistributedTracingTests
	{

		[Fact]
		public void ParseTraceParent()
		{
			var traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var(parent, traceId) = TraceParent.ParseTraceParentString(traceParent);

			parent.Should().Be("0af7651916cd43dd8448eb211c80319c");
			traceId.Should().Be("b7ad6b7169203331");
		}
	}
}
