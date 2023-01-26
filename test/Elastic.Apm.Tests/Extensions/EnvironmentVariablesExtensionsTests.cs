// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Extensions;

public class EnvironmentVariablesExtensionsTests
{
	private readonly IEnvironmentVariables _environment = new TestEnvironmentVariables { ["FOO_BAR"] = "baz" };

	[Fact]
	public void Test_SafeGetValue()
	{
		_environment.SafeGetValue("FOO_BAR").Should().Be("baz");
		_environment.SafeGetValue("FOO_BAZ").Should().NotBeNull();
		_environment.SafeGetValue("FOO_BAZ").Should().BeEmpty();
	}

	[Fact]
	public void Test_SafeCheckExists()
	{
		_environment.SafeCheckExists("FOO_BAR").Should().BeTrue();
		_environment.SafeCheckExists("FOO_BAZ").Should().BeFalse();
	}

	[Fact]
	public void Test_SafeCheckValue()
	{
		_environment.SafeCheckValue("FOO_BAR", "baz").Should().BeTrue();
		_environment.SafeCheckValue("FOO_BAR", string.Empty).Should().BeFalse();
		_environment.SafeCheckValue("FOO_BAR", null).Should().BeFalse();
		_environment.SafeCheckValue("FOO_BAZ", "baz").Should().BeFalse();
		_environment.SafeCheckValue("FOO_BAR", string.Empty).Should().BeFalse();
		_environment.SafeCheckValue("FOO_BAR", null).Should().BeFalse();
	}
}
