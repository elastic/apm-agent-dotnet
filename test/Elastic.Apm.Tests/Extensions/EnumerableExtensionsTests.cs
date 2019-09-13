using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Extensions
{
	public class EnumerableExtensionsTests
	{
		public static TheoryData EnumerablesToTest => new TheoryData<IEnumerable<object>, object[]>
		{
			{ Array.Empty<object>(), Array.Empty<object>() },
			{ Enumerable.Range(0, 0).Select(i => (object)i), Array.Empty<object>() },
			{ Enumerable.Range(0, 5).Select(i => (object)(i * i)), new object[] { 0, 1, 4, 9, 16, 25 } },
		};

		[Theory]
		[MemberData(nameof(EnumerablesToTest))]
		public void ForEach_test(IEnumerable<object> sourceEnumerable, object[] expectedElements)
		{
			var i = 0;
			sourceEnumerable.ForEach(x =>
			{
				x.Should().Be(expectedElements[i]);
				++i;
			});
		}

		[Theory]
		[MemberData(nameof(EnumerablesToTest))]
		public void ForEachIndexed_test(IEnumerable<object> sourceEnumerable, object[] expectedElements)
		{
			var expectedIndex = 0;
			sourceEnumerable.ForEachIndexed((x, i) =>
			{
				i.Should().Be(expectedIndex);
				x.Should().Be(expectedElements[i]);
				++expectedIndex;
			});
		}
	}
}
