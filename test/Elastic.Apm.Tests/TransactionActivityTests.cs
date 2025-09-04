// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class TransactionActivityTests
{
	[Fact]
	public void TransactionActivity_ShouldNotBeNull_WhenOpenTelemetryBridgeEnabled()
	{
		var agent = new ApmAgent(new TestAgentComponents(configuration:
			new MockConfiguration(openTelemetryBridgeEnabled: "true"), apmServerInfo: MockApmServerInfo.Version80));

		var transaction = (Transaction)agent.Tracer.StartTransaction("name", "type");

		transaction.Activity.Should().NotBeNull();
	}

	[Fact]
	public void TransactionActivity_ShouldNotBeNull_WhenOpenTelemetryBridgeDisabled()
	{
		var agent = new ApmAgent(new TestAgentComponents(configuration:
			new MockConfiguration(openTelemetryBridgeEnabled: "false"), apmServerInfo: MockApmServerInfo.Version80));

		var transaction = (Transaction)agent.Tracer.StartTransaction("name", "type");

		transaction.Activity.Should().NotBeNull();
	}

	[Fact]
	public void TransactionActivity_ShouldBeNull_WhenOpenTelemetryBridgeDisabled_AndIgnoreActivity()
	{
		var agent = new ApmAgent(new TestAgentComponents(configuration:
			new MockConfiguration(openTelemetryBridgeEnabled: "false"), apmServerInfo: MockApmServerInfo.Version80));

		var transaction = (Transaction)agent.Tracer.StartTransaction("name", "type", ignoreActivity: true);

		transaction.Activity.Should().BeNull();
	}

	[Fact]
	public void TransactionActivity_ShouldNotBeNull_AndMatchCurrent_WhenCurrentActivityPassedToTransaction()
	{
		var agent = new ApmAgent(new TestAgentComponents(configuration:
			new MockConfiguration(openTelemetryBridgeEnabled: "false")));

		var activity = new Activity("TEST");

		var transaction = new Transaction(agent.Logger, "dummy_name", "dumm_type", new Sampler(1.0), /* distributedTracingData: */ null,
				agent.PayloadSender, new MockConfiguration(openTelemetryBridgeEnabled: "true"), agent.TracerInternal.CurrentExecutionSegmentsContainer,
				MockApmServerInfo.Version80, null, current: activity);

		transaction.Activity.Should().NotBeNull();
		transaction.Activity.Id.Should().Be(activity.Id);
	}
}
