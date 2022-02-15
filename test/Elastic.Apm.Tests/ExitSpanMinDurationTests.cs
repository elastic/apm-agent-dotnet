// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Threading;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class ExitSpanMinDurationTests
{
	[Fact]
	public void FastExitSpanTest()
	{
		var payloadSender = new MockPayloadSender();
		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			configuration: new MockConfiguration(exitSpanMinDuration: "100ms")));
		agent.Tracer.CaptureTransaction("foo", "bar", t =>
		{
			t.CaptureSpan("span1", "test", () => Thread.Sleep(150), isExitSpan:true);
			t.CaptureSpan("span2", "test", () => { }, isExitSpan:true);
			t.CaptureSpan("span3", "test", () => Thread.Sleep(150), isExitSpan:true);
			//Fast, but not exit span
			t.CaptureSpan("span4", "test", () => { });

		});

		payloadSender.Transactions.Should().HaveCount(1);
		payloadSender.Spans.Should().HaveCount(3);

		payloadSender.FirstTransaction.DroppedSpanStats.Should().NotBeEmpty();
		payloadSender.FirstTransaction.DroppedSpanStats.First().DestinationServiceResource.Should().Be("test");
		payloadSender.FirstTransaction.DroppedSpanStats.First().DurationCount.Should().Be(1);

		payloadSender.Spans[0].Name.Should().Be("span1");
		payloadSender.Spans[1].Name.Should().Be("span3");
		payloadSender.Spans[2].Name.Should().Be("span4");

	}
}
