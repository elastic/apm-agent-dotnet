using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
			{ Enumerable.Range(1, 1).Select(i => (object)(i * i)), new object[] { 1 } },
			{ Enumerable.Range(2, 2).Select(i => (object)(i * i)), new object[] { 4, 9 } },
			{ Enumerable.Range(0, 5).Select(i => (object)(i * i)), new object[] { 0, 1, 4, 9, 16 } },
		};

		[Theory]
		[MemberData(nameof(EnumerablesToTest))]
		public void ForEach_test(IEnumerable<object> sourceEnumerable, object[] expectedElements)
		{
			var expectedIndex = 0;
			sourceEnumerable.ForEach(x =>
			{
				x.Should().Be(expectedElements[expectedIndex]);
				++expectedIndex;
			});
			expectedIndex.Should().Be(expectedElements.Length);
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
			expectedIndex.Should().Be(expectedElements.Length);
		}

		[Theory]
		[MemberData(nameof(EnumerablesToTest))]
		public async Task ForEach_async_test(IEnumerable<object> sourceEnumerable, object[] expectedElements)
		{
			var expectedIndex = 0;
			await sourceEnumerable.ForEach(async x =>
			{
				await Task.Yield();
				x.Should().Be(expectedElements[expectedIndex]);
				++expectedIndex;
			});
			expectedIndex.Should().Be(expectedElements.Length);
		}

		[Theory]
		[MemberData(nameof(EnumerablesToTest))]
		public async Task ForEachIndexed_async_test(IEnumerable<object> sourceEnumerable, object[] expectedElements)
		{
			var expectedIndex = 0;
			await sourceEnumerable.ForEachIndexed(async (x, i) =>
			{
				await Task.Yield();
				i.Should().Be(expectedIndex);
				x.Should().Be(expectedElements[i]);
				++expectedIndex;
			});
			expectedIndex.Should().Be(expectedElements.Length);
		}
	}
}
