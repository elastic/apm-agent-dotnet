// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.ApiTests
{
	public class ExitSpanTests
	{
		[Fact]
		public void TestNonExitSpan()
		{

			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.StartSpan("foo", "bar").End();
			});

			payloadSender.FirstSpan.Context.Destination.Should().BeNull();
		}

		[Fact]
		public void SimpleManualExitSpan()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			agent.Tracer.CaptureTransaction("foo", "bar", t =>
			{
				t.StartSpan("foo", "bar", isExitSpan: true).End();
			});

			payloadSender.FirstSpan.Context.Destination.Should().NotBeNull();
			payloadSender.FirstSpan.Context.Destination.Service.Resource.Should().Be("bar");
		}
	}
}
