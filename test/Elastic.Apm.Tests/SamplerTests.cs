using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class SamplerTests
	{
		[Theory]
		[InlineData(0.0)]
		[InlineData(1.0)]
		[InlineData(0.3)]
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

			sampler.Constant?.Let(c => 10.Repeat(() => sampler.DecideIfToSample().Should().Be(c)));
		}

		[Theory]
		[InlineData(0.00123)]
		[InlineData(0.5)]
		[InlineData(0.75)]
		public void UniformDistribution(double rate) { }
	}
}
