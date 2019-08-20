using System;
using System.IO;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Elastic.Apm.AspNetCore.Extensions;
using Xunit;
using System.Collections.Generic;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="ListExtensionsTests" /> class.
	/// It loads the json config files from the TestConfig folder
	/// </summary>
	public class ListExtensionsTests
	{
		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_valid.json config file and passes it to the agent.
		/// Makes sure that the values from the config file are applied to the agent.
		/// </summary>
		[Fact]
		public void ReadValidConfigsFromAppSettingsJson()
		{
			var list = new List<string>() {"application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };

			list.ContainsLike("application/x-www-form-urlencoded*___").Should().BeTrue();
			list.ContainsLike("application/x-www-form-urlencoded").Should().BeTrue();
			list.ContainsLike("application/x-www-form-").Should().BeFalse();

			list.ContainsLike("text/*___").Should().BeTrue();
			list.ContainsLike("text/").Should().BeTrue();
			list.ContainsLike("txt").Should().BeFalse();

			list.ContainsLike("text/*___").Should().BeTrue();
			list.ContainsLike("text/").Should().BeTrue();
			list.ContainsLike("txt").Should().BeFalse();

			list.ContainsLike("application/json*___").Should().BeTrue();
			list.ContainsLike("application/json").Should().BeTrue();
			list.ContainsLike("application").Should().BeFalse();
		}
	}
}
