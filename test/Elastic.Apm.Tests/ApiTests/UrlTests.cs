// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.ApiTests;

public class UrlTests
{
	[Theory]
	[InlineData("http://localhost:7071/api/SampleHttpTrigger", "localhost", "/api/SampleHttpTrigger", "",
		"HTTP")]
	[InlineData("https://example.com:7071/?foo", "example.com", "/", "foo",
		"HTTP")]
	[InlineData("http://localhost:7071/api/SampleHttpTrigger?foo=bar", "localhost", "/api/SampleHttpTrigger",
		"foo=bar",
		"HTTP")]
	public void Test_FromUri(string uri, string host, string path, string query, string scheme)
	{
		var url = Url.FromUri(new(uri));
		url.Full.Should().Be(uri);
		url.HostName.Should().Be(host);
		url.PathName.Should().Be(path);
		url.Search.Should().Be(query);
		url.Protocol.Should().Be(scheme);
	}

	[Fact]
	public void Test_FromUri_Invalid()
	{
		Url.FromUri(null).Should().Be(null);
		Url.FromUri(new("/foo/bar", UriKind.Relative)).Should().Be(null);
	}
}
