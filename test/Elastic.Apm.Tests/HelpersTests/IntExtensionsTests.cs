// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Tests.TestHelpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class IntExtensionsTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(31)]
		public void TestRepeat(int repeatCount)
		{
			var counter = 0;
			// ReSharper disable once AccessToModifiedClosure
			repeatCount.Repeat(() => { ++counter; });
			counter.Should().Be(repeatCount);

			counter = 0;
			repeatCount.Repeat(i =>
			{
				i.Should().Be(counter);
				++counter;
			});
			counter.Should().Be(repeatCount);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(31)]
		public async Task TestRepeatAsync(int repeatCount)
		{
			var counter = 0;
			await repeatCount.Repeat(async _ =>
			{
				await Task.Delay(1);
				++counter;
			});
			counter.Should().Be(repeatCount);

			counter = 0;
			await repeatCount.Repeat(async i =>
			{
				await Task.Delay(1);
				i.Should().Be(counter);
				++counter;
			});
			counter.Should().Be(repeatCount);
		}
	}
}
