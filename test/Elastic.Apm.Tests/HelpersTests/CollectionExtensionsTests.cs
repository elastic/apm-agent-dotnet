using System.Collections;
using Elastic.Apm.Helpers;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class CollectionExtensionsTests
	{
		[Fact]
		public void IsEmpty_List_test()
		{
			var list = new List<int>();
			list.IsEmpty().Should().BeTrue();
			list.Add(123);
			list.IsEmpty().Should().BeFalse();
			list.Clear();
			list.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_IList_test()
		{
			IList<int> list = new List<int>();
			list.IsEmpty().Should().BeTrue();
			list.Add(123);
			list.IsEmpty().Should().BeFalse();
			list.Clear();
			list.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_not_generic_ICollection_test()
		{
			ICollection list = new List<int>();
			list.IsEmpty().Should().BeTrue();
			((List<int>)list).Add(123);
			list.IsEmpty().Should().BeFalse();
			((List<int>)list).Clear();
			list.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_ICollection_test()
		{
			ICollection<int> list = new List<int>();
			list.IsEmpty().Should().BeTrue();
			list.Add(123);
			list.IsEmpty().Should().BeFalse();
			list.Clear();
			list.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_IReadOnlyCollection_test()
		{
			IReadOnlyCollection<int> list = new List<int>();
			list.IsEmpty().Should().BeTrue();
			((List<int>)list).Add(123);
			list.IsEmpty().Should().BeFalse();
			((List<int>)list).Clear();
			list.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_Dictionary_test()
		{
			var dictionary = new Dictionary<string, int>();
			dictionary.IsEmpty().Should().BeTrue();
			dictionary.Add("123", 123);
			dictionary.IsEmpty().Should().BeFalse();
			dictionary.Clear();
			dictionary.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_IDictionary_test()
		{
			IDictionary<string, int> dictionary = new Dictionary<string, int>();
			dictionary.IsEmpty().Should().BeTrue();
			dictionary.Add("123", 123);
			dictionary.IsEmpty().Should().BeFalse();
			dictionary.Clear();
			dictionary.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_IReadOnlyDictionary_test()
		{
			IReadOnlyDictionary<string, int> dictionary = new Dictionary<string, int>();
			dictionary.IsEmpty().Should().BeTrue();
			((Dictionary<string, int>)dictionary).Add("123", 123);
			dictionary.IsEmpty().Should().BeFalse();
			((Dictionary<string, int>)dictionary).Clear();
			dictionary.IsEmpty().Should().BeTrue();
		}

		[Fact]
		public void IsEmpty_Queue_test()
		{
			var queue = new Queue<string>();
			queue.IsEmpty().Should().BeTrue();
			queue.Enqueue("123");
			queue.IsEmpty().Should().BeFalse();
			queue.Clear();
			queue.IsEmpty().Should().BeTrue();
		}
	}
}
