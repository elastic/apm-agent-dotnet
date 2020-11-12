// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Globalization;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SamplerTests
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public static TheoryData RateVariantsToTest => new TheoryData<double>
		{
			0,
			0.0001,
			0.0123,
			0.3,
			0.5,
			0.75,
			0.789,
			0.9999,
			1
		};

		[Theory]
		[MemberData(nameof(RateVariantsToTest))]
		public void ConstantSampler(double rate)
		{
			var sampler = new Sampler(rate);
			switch (rate)
			{
				case 0:
					sampler.Constant.Should().BeFalse();
					break;
				case 1:
					sampler.Constant.Should().BeTrue();
					break;
				default:
					sampler.Constant.Should().Be(null);
					break;
			}

			sampler.Constant?.Let(c => 10.Repeat(() =>
			{
				var randomBytes = new byte[8];
				RandomGenerator.GenerateRandomBytes(randomBytes);
				sampler.DecideIfToSample(randomBytes).Should().Be(c);
			}));
		}

		[Theory]
		[MemberData(nameof(RateVariantsToTest))]
		public void SampleRateShouldBeSetOnTransactionAndSpan(double rate)
		{
			var mockPayloadSender = new MockPayloadSender();
			using var agent =
				new ApmAgent(new TestAgentComponents(payloadSender: mockPayloadSender,
					config: new MockConfigSnapshot(transactionSampleRate: rate.ToString(CultureInfo.InvariantCulture))));
			agent.Tracer.CaptureTransaction("TestTransaction", "test", t =>
			{
				var transaction = t as Transaction;
				transaction.Should().NotBeNull();

				if (transaction!.IsSampled)
					transaction.SampleRate.Should().Be(rate);
				else
					transaction.SampleRate.Should().Be(0);

				t.CaptureSpan("TestSpan", "Test", s =>
				{
					var span = s as Span;
					span.Should().NotBeNull();

					if (span!.IsSampled)
						span.SampleRate.Should().Be(rate);
					else
						span.SampleRate.Should().Be(0);
				});
			});

			if (mockPayloadSender.FirstTransaction.IsSampled)
			{
				mockPayloadSender.FirstTransaction.SampleRate.Should().Be(rate);
				mockPayloadSender.FirstSpan.SampleRate.Should().Be(rate);
			}
			else
				mockPayloadSender.FirstTransaction.SampleRate.Should().Be(0);
		}

		[Theory]
		[MemberData(nameof(RateVariantsToTest))]
		public void DistributionShouldBeUniform(double rate)
		{
			const int total = 1_000_000;
			var startCheckingAfter = Convert.ToInt32(total * 0.1); // i.e., after 10%
			const double allowedDiffInRate = 0.01;

			var sampledCount = 0;
			var noopLogger = new NoopLogger();
			var currentExecutionSegmentsContainer = new NoopCurrentExecutionSegmentsContainer();
			var noopPayloadSender = new NoopPayloadSender();
			var configurationReader = new MockConfigSnapshot(noopLogger);
			var sampler = new Sampler(rate);

			total.Repeat(i =>
			{
				// reset current activity, otherwise all transactions share the same traceid which influences the sampling decision
				Activity.Current = null;

				var transaction = new Transaction(noopLogger, "test transaction name", "test transaction type", sampler,
					/* distributedTracingData: */ null, noopPayloadSender, configurationReader, currentExecutionSegmentsContainer, new MockApmServerInfo());
				if (transaction.IsSampled) ++sampledCount;

				// ReSharper disable once InvertIf
				if (i + 1 >= startCheckingAfter)
				{
					var actualRate = (double)sampledCount / (i + 1);
					var diffInRate = actualRate - rate;
					Assert.True(Math.Abs(diffInRate) <= allowedDiffInRate,
						"Abs(diffInRate) should be <= allowedDiffInRate. " +
						$"diffInRate: {diffInRate}, allowedDiffInRate: {allowedDiffInRate}, " +
						$"i: {i}, " +
						$"actual rate: {actualRate}, expected rate: {rate}, " +
						$"actual sampled count: {sampledCount}, expected sampled count: {Convert.ToInt32((i + 1) * rate)}"
					);
				}
			});
		}

		[Theory]
		[MemberData(nameof(RateVariantsToTest))]
		public void SamplingDecisionDependsOnlyOnInput(double rate)
		{
			var sampler = new Sampler(rate);
			var randomBytes = new byte[8];
			RandomGenerator.GenerateRandomBytes(randomBytes);
			var firstIsSampled = sampler.DecideIfToSample(randomBytes);
			10.Repeat(() => sampler.DecideIfToSample(randomBytes).Should().Be(firstIsSampled));
		}

		[Theory]
		[InlineData(0.0001, 0.0001)]
		[InlineData(0.00001, 0.0001)]
		[InlineData(0.000001, 0.0001)]
		[InlineData(0.55554, 0.5555)]
		[InlineData(0.55555, 0.5556)]
		[InlineData(0.55556, 0.5556)]
		public void Rate_Precision_Should_Be_Rounded_To_Four_Decimal_Places(double rate, double expectedRate)
		{
			var sampler = new Sampler(rate);
			sampler.ToString().Should().Be($"Sampler{{ rate: {expectedRate.ToString(CultureInfo.InvariantCulture)} }}");
			sampler.Rate.Should().Be(expectedRate);
		}

		[Theory]
		[MemberData(nameof(RateVariantsToTest))]
		public void ExceptionThrownIfTooFewRandomBytes(double rate)
		{
			var sampler = new Sampler(rate);
			10.Repeat(bytesArrayLength =>
			{
				var bytes = new byte[bytesArrayLength];
				try
				{
					sampler.DecideIfToSample(bytes);
				}
				catch (ArgumentException)
				{
					bytes.Length.Should().BeLessThan(8);
				}
			});
		}
	}
}
