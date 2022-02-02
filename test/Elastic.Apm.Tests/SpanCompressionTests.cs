// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Elastic.Apm.Model;

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
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
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
		/// Makes sure if no config is set, span compression is disabled
		/// </summary>
		[Fact]
		public void DisabledByDefault()
		{
			var spanName = "Select * From Table";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) Generate10DbCalls(agent, spanName);

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(10);
			payloadSender.Spans.Where(s => (s as Span).Composite != null).Should().BeEmpty();
		}

		/// <summary>
		/// Db calls with same kind
		/// </summary>
		[Fact]
		public void BasicDbCallsWithSameKind()
		{
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionSameKindMaxDuration: "5s"))))
				Generate10DbCalls(agent, null);

			payloadSender.Transactions.Should().HaveCount(1);
			payloadSender.Spans.Should().HaveCount(1);
			payloadSender.FirstSpan.Composite.Should().NotBeNull();
			payloadSender.FirstSpan.Composite.Count.Should().Be(10);
			payloadSender.FirstSpan.Composite.CompressionStrategy = "same_kind";
			payloadSender.FirstSpan.Name.Should().Be("Calls to mssql");
		}

		/// <summary>
		/// Creates 10db spans with exact match, then creates 1 non db span (which breaks compression), then creates 10 db spans again
		/// </summary>
		[Fact]
		public void TwentyDbSpansWithRandomSpanInBetween()
		{
			var spanName = "Select * From Table";
			var payloadSender = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionExactMatchMaxDuration: "5s"))))
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
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
					   configuration: new MockConfiguration(spanCompressionEnabled: "true", spanCompressionExactMatchMaxDuration: "5s"))))
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

		private void Generate10DbCalls(IApmAgent agent, string spanName, bool shouldSleep = false, int spanDuration = 10) =>
			agent.Tracer.CaptureTransaction("Foo", "Bar", t =>
			{
				var random = new Random();
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
