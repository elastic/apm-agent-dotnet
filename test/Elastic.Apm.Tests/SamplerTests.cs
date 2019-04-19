using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SamplerTests
	{
		// ReSharper disable once MemberCanBePrivate.Global
		public static IEnumerable<object[]> RateVariantsToTest()
		{
			yield return new object[] { 0 };
			yield return new object[] { 0.000000001 };
			yield return new object[] { 0.00123 };
			yield return new object[] { 0.3 };
			yield return new object[] { 0.5 };
			yield return new object[] { 0.75 };
			yield return new object[] { 0.789 };
			yield return new object[] { 0.999999999 };
			yield return new object[] { 1 };
		}

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
			var noopPayloadSender = new NoopPayloadSender();
			var sampler = new Sampler(rate);

			total.Repeat(i =>
			{
				var transaction = new Transaction(noopLogger, "test transaction name", "test transaction type", sampler, null, noopPayloadSender);
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
