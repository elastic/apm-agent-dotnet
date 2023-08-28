// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.DistributedTracing;

public class BaggageTests
{
	private readonly ActivitySource _activitySource = new(nameof(BaggageTests));

	public BaggageTests()
	{
		var activityListener = new ActivityListener
		{
			ShouldListenTo = _ => true,
			SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
		};

		ActivitySource.AddActivityListener(activityListener);
	}

	/// <summary>
	/// Writes 1 baggage with default agent settings and creates a transaction, a span, and an error.
	/// Asserts that the baggage is captured on 1) transaction, 2) span, and 3) error.
	/// </summary>
	[Fact]
	public void CaptureBaggageWithDefaultConfig()
	{
		var payloadSender = new MockPayloadSender();
		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true")));

		RunSample(agent);

		payloadSender.FirstTransaction.Should().NotBeNull();
		payloadSender.FirstTransaction.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("foo", "bar"));

		payloadSender.FirstSpan.Should().NotBeNull();
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("foo", "bar"));

		payloadSender.FirstError.Should().NotBeNull();
		payloadSender.FirstError.Context.InternalLabels.Should().NotBeNull();
		payloadSender.FirstError.Context.InternalLabels.Value.Should().Contain(new KeyValuePair<string, string>("foo", "bar"));
	}

	/// <summary>
	/// Sets baggageToAttachOn[*] configs to `foo` and writes 2 baggage items.
	/// Asserts that only baggage with key `foo` is captured, and the other baggage is not captured.
	/// </summary>
	[Fact]
	public void CaptureBaggageWithNonDefaultConfig()
	{
		var payloadSender = new MockPayloadSender();
		using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender,
			configuration: new MockConfiguration(openTelemetryBridgeEnabled: "true", baggageToAttach: "foo")));

		_activitySource.StartActivity("Activity1")?.AddBaggage("key1", "value1");
		RunSample(agent);

		payloadSender.FirstTransaction.Should().NotBeNull();
		payloadSender.FirstTransaction.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("foo", "bar"));
		payloadSender.FirstTransaction.Otel.Attributes.Should().NotContainKey("key1");

		payloadSender.FirstSpan.Should().NotBeNull();
		payloadSender.FirstSpan.Otel.Attributes.Should().Contain(new KeyValuePair<string, object>("foo", "bar"));
		payloadSender.FirstSpan.Otel.Attributes.Should().NotContainKey("key1");

		payloadSender.FirstError.Should().NotBeNull();
		payloadSender.FirstError.Context.InternalLabels.Should().NotBeNull();
		payloadSender.FirstError.Context.InternalLabels.Value.Should().Contain(new KeyValuePair<string, string>("foo", "bar"));
		payloadSender.FirstError.Context.InternalLabels.Value.Should().NotContainKey("key1");
	}

	private void RunSample(ApmAgent agent)
	{
		_activitySource.StartActivity("MyActivity")?.AddBaggage("foo", "bar");
		agent.Tracer.CaptureTransaction("Test", "Test", t =>
		{
			t.CaptureSpan("test", "test", s =>
			{
				s.CaptureError("Sample Error", "just a test", new StackTrace().GetFrames());
			});
		});
	}
}
