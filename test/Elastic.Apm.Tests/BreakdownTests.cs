// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class BreakdownTests
	{
		[Fact]
		public void V()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender)))
			{
				Transaction transaction;
				Span span1;
				Span span2;
				agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
				{
					transaction = t as Transaction;
					Thread.Sleep(100);
					t.CaptureSpan("Foo", "Bar", s1 =>
					{
						span1 = s1 as Span;
						Thread.Sleep(100);
						s1.CaptureSpan("Foo", "Bar", s2 =>
						{
							span2 = s2 as Span;
							Thread.Sleep(100);
						});
					});
					Thread.Sleep(100);
				});

				// TODO add asserts

			}
		}
	}
}
