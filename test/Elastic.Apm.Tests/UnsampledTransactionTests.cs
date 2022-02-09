// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class UnsampledTransactionTests
{
	/// <summary>
	/// Sets sample rate to `0` (do not sample anything) and makes sure that no transaction is sent on APM Server v8.0+
	/// </summary>
	[Fact]
	public void UnsampledTransactionWithoutSpansOnV8OrNewer()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
				   configuration: new MockConfiguration(transactionSampleRate: "0"))))
			agent.Tracer.CaptureTransaction("foo", "bar", () => { });

		payloadSender.WaitForTransactions(TimeSpan.FromMilliseconds(100));
		payloadSender.Transactions.Should().BeNullOrEmpty();
	}

	/// <summary>
	/// Sets sample rate to `0` (do not sample anything) and makes sure that an unsampled transaction is sent on APM Server versions pre 8.0
	/// </summary>
	[Fact]
	public void UnsampledTransactionWithoutSpansOnPreV8()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version716, payloadSender: payloadSender,
				   configuration: new MockConfiguration(transactionSampleRate: "0"))))
			agent.Tracer.CaptureTransaction("foo", "bar", () => { });

		payloadSender.WaitForTransactions(TimeSpan.FromMilliseconds(100));
		payloadSender.Transactions.Should().NotBeNullOrEmpty();
	}

	/// <summary>
	/// Sets sample rate to `0` (do not sample anything) and makes sure that no transaction ans spans are sent on APM Server v8.0+
	/// </summary>
	[Fact]
	public void UnsampledTransactionWithSpansOnV8OrNewer()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
				   configuration: new MockConfiguration(transactionSampleRate: "0"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", (t) =>
			{
				t.CaptureSpan("span1", "testSpan", (s1) => s1.CaptureSpan("span2", "testSpan", () => { }));
				t.CaptureSpan("span3", "testSpan", () => { });
			});
		}

		payloadSender.WaitForTransactions(TimeSpan.FromMilliseconds(100));
		payloadSender.Transactions.Should().BeNullOrEmpty();
		payloadSender.Spans.Should().BeNullOrEmpty();
	}


	/// <summary>
	/// Sets sample rate to `0` (do not sample anything) and makes sure that no transaction ans spans (including compressed spans) are sent on APM Server v8.0+
	/// </summary>
	[Fact]
	public void UnsampledTransactionWithCompressedSpanOnV8OrNewer()
	{
		var payloadSender = new MockPayloadSender();
		using (var agent = new ApmAgent(new TestAgentComponents(apmServerInfo: MockApmServerInfo.Version80, payloadSender: payloadSender,
				   configuration: new MockConfiguration(transactionSampleRate: "0", exitSpanMinDuration:"0"))))
		{
			agent.Tracer.CaptureTransaction("foo", "bar", (t) =>
			{
				for (var i = 0; i < 10; i++)
				{
					var name =  "Foo" + new Random().Next();
					t.CaptureSpan(name, ApiConstants.TypeDb, (s) =>
					{
						s.Context.Db = new Database() { Type = "mssql", Instance = "01" };
					}, ApiConstants.SubtypeMssql, isExitSpan: true);
				}
			});
		}

		payloadSender.WaitForTransactions(TimeSpan.FromMilliseconds(100));
		payloadSender.Transactions.Should().BeNullOrEmpty();
		payloadSender.Spans.Should().BeNullOrEmpty();
	}
}
