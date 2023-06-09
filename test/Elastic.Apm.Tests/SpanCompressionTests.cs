// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SpanCompressionTests
	{
		/// <summary>
		/// Db calls with exact match
		/// </summary>
		[Fact]
		public void BasicDbCallsWithExactMatch()
		{
			var spanName = "Select * From Table";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionExactMatchMaxDuration: "5s"))))
				Generate10DbCalls(agent, spanName);

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(1);
			payloadSender.FirstSpan.Composite.Should().NotBeNull();
			payloadSender.FirstSpan.Composite.Count.Should().Be(10);
			payloadSender.FirstSpan.Composite.CompressionStrategy = "exact_match";
			payloadSender.FirstSpan.Name.Should().Be(spanName);
		}

		/// <summary>
		/// Makes sure if no config is set, span compression is enabled
		/// The default changed in https://github.com/elastic/apm-agent-dotnet/issues/1662
		/// </summary>
		[Fact]
		public void EnabledByDefaultOn80()
		{
			var spanName = "Select * From Table";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender)))
			{
				agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
				{
					for (var i = 0; i < 10; i++)
					{
						var name = spanName ?? "Foo" + new Random().Next();
						t.CaptureSpan(name, ApiConstants.TypeDb, (s) =>
						{
							s.Context.Db = new Database() { Type = "mssql", Instance = "01" };
							s.Duration = 1;
						}, ApiConstants.SubtypeMssql, isExitSpan: true);
					}
				});
			}

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(1, $"Spans should be compressed, we expect 1 compressed spans. Current Spans: {string.Join(Environment.NewLine, payloadSender.Spans)}");
			payloadSender.Spans.Where(s => (s as Span).Composite != null).Should().NotBeEmpty();
		}

		/// <summary>
		/// Makes sure that agents connected to older than APM Server 8.0 don't send composite spans
		/// </summary>
		[Fact]
		public void NoCompositeOnPre80Versions()
		{
			var spanName = "Select * From Table";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version710, payloadSender: payloadSender)))
				Generate10DbCalls(agent, spanName, true, 2);

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(10);
			payloadSender.Spans.Where(s => (s as Span).Composite != null).Should().BeNullOrEmpty();
		}

		/// <summary>
		/// Db calls with same kind
		/// </summary>
		[Fact]
		public void BasicDbCallsWithSameKind()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionSameKindMaxDuration: "15s",
						   spanCompressionExactMatchMaxDuration: "100ms", exitSpanMinDuration: "0"))))
				Generate10DbCalls(agent, null, true, 200);

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(1);
			payloadSender.FirstSpan.Composite.Should().NotBeNull();
			payloadSender.FirstSpan.Composite.Count.Should().Be(10);
			payloadSender.FirstSpan.Composite.CompressionStrategy = "same_kind";
			payloadSender.FirstSpan.Name.Should().Be("Calls to mssql/01");
		}

		/// <summary>
		/// Creates 10db spans with exact match, then creates 1 non db span (which breaks compression), then creates 10 db spans again
		/// </summary>
		[Fact]
		public void TwentyDbSpansWithRandomSpanInBetween()
		{
			var spanName = "Select * From Table";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionExactMatchMaxDuration: "5s",
						   exitSpanMinDuration: "0"))))
			{
				agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
				{
					for (var i = 0; i < 10; i++)
					{
						var name = spanName;
						t.CaptureSpan(name, ApiConstants.TypeDb, (s) =>
						{
							s.Context.Db = new Database() { Type = "mssql", Instance = "01" };
						}, ApiConstants.SubtypeMssql, isExitSpan: true);
					}

					t.CaptureSpan("foo", "bar", () => { });

					for (var i = 0; i < 10; i++)
					{
						var name = spanName;
						t.CaptureSpan(name + "2", ApiConstants.TypeDb, (s) =>
						{
							s.Context.Db = new Database() { Type = "mssql", Instance = "01" };
						}, ApiConstants.SubtypeMssql, isExitSpan: true);
					}
				});
			}

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(3);

			payloadSender.FirstSpan.Composite.Should().NotBeNull();
			payloadSender.FirstSpan.Composite.Count.Should().Be(10);
			payloadSender.FirstSpan.Composite.CompressionStrategy = "exact_match";
			payloadSender.FirstSpan.Name.Should().Be(spanName);

			payloadSender.Spans[1].Name.Should().Be("foo");
			(payloadSender.Spans[1] as Span)!.Composite.Should().BeNull();

			(payloadSender.Spans[2] as Span)!.Composite.Should().NotBeNull();
			(payloadSender.Spans[2] as Span)!.Composite.Count.Should().Be(10);
			(payloadSender.Spans[2] as Span)!.Composite.CompressionStrategy = "exact_match";
			payloadSender.Spans[2].Name.Should().Be(spanName + "2");
		}

		[Fact]
		public void CompressionOnParentSpan()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionExactMatchMaxDuration: "5s",
						   exitSpanMinDuration: "0"))))
			{
				agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
				{
					t.CaptureSpan("foo1", "bar", span1 =>
					{
						for (var i = 0; i < 10; i++)
						{
							span1.CaptureSpan("Select * From Table1", ApiConstants.TypeDb, (s) =>
							{
								s.Context.Db = new Database() { Type = "mssql", Instance = "01" };
							}, ApiConstants.SubtypeMssql, isExitSpan: true);
						}

						span1.CaptureSpan("foo2", "randomSpan", () => { });


						for (var i = 0; i < 10; i++)
						{
							span1.CaptureSpan("Select * From Table2", ApiConstants.TypeDb, (s2) =>
							{
								s2.Context.Db = new Database() { Type = "mssql", Instance = "01" };
							}, ApiConstants.SubtypeMssql, isExitSpan: true);
						}
					});
				});
			}

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(4);

			(payloadSender.Spans[3] as Span)!.Composite.Should().BeNull();
			payloadSender.Spans[3].Name.Should().Be("foo1");

			(payloadSender.Spans[0] as Span)!.Composite.Should().NotBeNull();
			(payloadSender.Spans[0] as Span)!.Composite.Count.Should().Be(10);
			(payloadSender.Spans[0] as Span)!.Composite.CompressionStrategy = "exact_match";
			payloadSender.Spans[0].Name.Should().Be("Select * From Table1");

			payloadSender.Spans[1].Name.Should().Be("foo2");
			(payloadSender.Spans[1] as Span)!.Composite.Should().BeNull();

			(payloadSender.Spans[2] as Span)!.Composite.Should().NotBeNull();
			(payloadSender.Spans[2] as Span)!.Composite.Count.Should().Be(10);
			(payloadSender.Spans[2] as Span)!.Composite.CompressionStrategy = "exact_match";
			payloadSender.Spans[2].Name.Should().Be("Select * From Table2");


			payloadSender.Spans[0].ParentId.Should().Be(payloadSender.Spans[3].Id);
			payloadSender.Spans[1].ParentId.Should().Be(payloadSender.Spans[3].Id);
			payloadSender.Spans[1].ParentId.Should().Be(payloadSender.Spans[3].Id);
		}

		/// <summary>
		/// See: https://github.com/elastic/apm-agent-dotnet/issues/1631
		/// Makes sure Span.Composite is empty when there is no compression.
		/// </summary>
		[Fact]
		public void EmptyCompressFieldWithNoCompression()
		{
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
				configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionSameKindMaxDuration: "1ms",
					spanCompressionExactMatchMaxDuration: "1ms", exitSpanMinDuration: "0")));
			agent.Tracer.CaptureTransaction("foo", "bar", (t) =>
			{
				var span1 = t.StartSpan("span", "test", isExitSpan: true);
				span1.Context.Destination = new Destination { Service = new Destination.DestinationService { Resource = "foo" } };
				var span2 = t.StartSpan("span", "test", isExitSpan: true);
				span2.Context.Destination = new Destination { Service = new Destination.DestinationService { Resource = "foo" } };

				span2.Duration = 500;

				span1.End();
				span2.End();
			});

			payloadSender.Spans.Count.Should().Be(2);
			(payloadSender.Spans[0] as Span)!.Composite.Should().BeNull();
			(payloadSender.Spans[1] as Span)!.Composite.Should().BeNull();
		}

		/// <summary>
		/// From https://github.com/elastic/apm-agent-dotnet/issues/1686
		/// Creates a child span which is eligible for compression, but ends after it parent already ended.
		/// The test makes sure we don't compress such spans.
		/// Compressing such spans would mean we'd never send such spans, since the parent ends before we compress those.
		/// </summary>
		[Fact]
		public void CompressEligibleSpanAfterParenEnded()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionExactMatchMaxDuration: "5s",
						   exitSpanMinDuration: "0"))))
			{
				agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
				{
					var parentSpan = t.StartSpan("foo1", "bar");

					parentSpan.End();
					for (var i = 0; i < 10; i++)
					{
						var childSpan = parentSpan.StartSpan("Select * From Table1", ApiConstants.TypeDb, ApiConstants.SubtypeMssql,
							isExitSpan: true);
						childSpan.Context.Db = new Database() { Type = "mssql", Instance = "01" };
						childSpan.End();
					}


				});
			}

			// The manifestation of not implementing issues/1686 is to only have the parent span and skipping the children
			// Which in the test case means only a single span.
			payloadSender.Spans.Count.Should().NotBe(1);

			payloadSender.Spans.Count.Should().Be(11);
			payloadSender.Spans.Where(s =>
			{
				if (s is Span realSpan)
					return realSpan.Composite != null;

				return false;
			}).Should().BeNullOrEmpty();
		}

		private void Generate10DbCalls(IApmAgent agent, string spanName, bool shouldSleep = false, int spanDuration = 10) =>
			agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
			{
				for (var i = 0; i < 10; i++)
				{
					var name = spanName ?? "Foo" + new Random().Next();
					t.CaptureSpan(name, ApiConstants.TypeDb, (s) =>
					{
						s.Context.Db = new Database() { Type = "mssql", Instance = "01" };
						if (shouldSleep)
							Thread.Sleep(spanDuration);
					}, ApiConstants.SubtypeMssql, isExitSpan: true);
				}
			});
	}
}
