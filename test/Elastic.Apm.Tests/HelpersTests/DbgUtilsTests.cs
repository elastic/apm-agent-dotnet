// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class DbgUtilsTests
	{
		[Fact]
		public void CurrentMethodName_test() => DbgUtils.CurrentMethodName().Should().Be(nameof(CurrentMethodName_test));
	}
}
