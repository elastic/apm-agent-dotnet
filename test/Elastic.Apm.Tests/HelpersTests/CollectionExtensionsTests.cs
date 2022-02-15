// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Helpers;
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
	}
}
