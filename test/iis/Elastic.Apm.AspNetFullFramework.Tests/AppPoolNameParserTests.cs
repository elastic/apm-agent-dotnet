// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	/// <summary>
	/// Unit tests for <see cref="AppPoolNameParser" />. They don't need IIS.
	/// </summary>
	public class AppPoolNameParserTests
	{
		[Fact]
		public void Returns_App_Pool_Name_From_W3wp_Arguments() =>
			AppPoolNameParser.FromCommandLineArgs(new[]
				{
					@"c:\windows\system32\inetsrv\w3wp.exe", "-ap", "test001", "-v", "v4.0", "-l", "webengine4.dll", "-a",
					@"\\.\pipe\iisipm1f8b1e0c", "-h", @"C:\inetpub\temp\apppools\test001\test001.config", "-w", "", "-m", "0", "-t", "20"
				})
				.Should()
				.Be("test001");

		[Fact]
		public void Returns_App_Pool_Name_With_Spaces() =>
			AppPoolNameParser.FromCommandLineArgs(new[] { "w3wp.exe", "-ap", ".NET v4.5 Classic", "-v", "v4.0" })
				.Should()
				.Be(".NET v4.5 Classic");

		[Fact]
		public void Removes_Surrounding_Quotes() =>
			AppPoolNameParser.FromCommandLineArgs(new[] { "w3wp.exe", "-ap", "\"DefaultAppPool\"" })
				.Should()
				.Be("DefaultAppPool");

		[Fact]
		public void Argument_Name_Is_Case_Insensitive() =>
			AppPoolNameParser.FromCommandLineArgs(new[] { "w3wp.exe", "-AP", "test001" })
				.Should()
				.Be("test001");

		[Fact]
		public void Returns_Null_When_Argument_Is_Missing() =>
			AppPoolNameParser.FromCommandLineArgs(new[] { "iisexpress.exe", "/config:applicationhost.config", "/site:WebSite1" })
				.Should()
				.BeNull();

		[Fact]
		public void Returns_Null_When_Ap_Is_Last_Argument() =>
			AppPoolNameParser.FromCommandLineArgs(new[] { "w3wp.exe", "-ap" })
				.Should()
				.BeNull();

		[Fact]
		public void Returns_Null_When_Value_Is_Empty() =>
			AppPoolNameParser.FromCommandLineArgs(new[] { "w3wp.exe", "-ap", "\"\"" })
				.Should()
				.BeNull();

		[Fact]
		public void Returns_Null_For_Null_Or_Empty_Args()
		{
			AppPoolNameParser.FromCommandLineArgs(null).Should().BeNull();
			AppPoolNameParser.FromCommandLineArgs(new string[0]).Should().BeNull();
		}
	}
}
