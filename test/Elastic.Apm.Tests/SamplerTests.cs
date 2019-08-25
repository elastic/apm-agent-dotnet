using System;
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
			{ 0 },
			{ 0.000000001 },
			{ 0.00123 },
			{ 0.3 },
			{ 0.5 },
			{ 0.75 },
			{ 0.789 },
			{ 0.999999999 },
			{ 1 },
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
		public void DistributionShouldBeUniform(double rate)
		{
			const int total = 1_000_000;
			var startCheckingAfter = Convert.ToInt32(total * 0.1); // i.e., after 10%
			const double allowedDiffInRate = 0.01;

			var sampledCount = 0;
			var noopLogger = new NoopLogger();
			var currentExecutionSegmentsContainer = new NoopCurrentExecutionSegmentsContainer();
			var noopPayloadSender = new NoopPayloadSender();
			var configurationReader = new TestAgentConfigurationReader(noopLogger);
			var sampler = new Sampler(rate);

			total.Repeat(i =>
			{
				var transaction = new Transaction(noopLogger, "test transaction name", "test transaction type", sampler,
					/* distributedTracingData: */ null, noopPayloadSender, configurationReader, currentExecutionSegmentsContainer);
				if (transaction.IsSampled) ++sampledCount;

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
