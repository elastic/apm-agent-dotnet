// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class WebApiTests : TestsBase
	{
		public WebApiTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		// https://github.com/elastic/apm-agent-dotnet/issues/1113
		[AspNetFullFrameworkFact]
		public async Task MultipartData_Should_Not_Throw()
		{
			var pathData = SampleAppUrlPaths.WebApiPage;
			using var request = new HttpRequestMessage(HttpMethod.Post, pathData.Uri);

			using var plainInputTempFile = TempFile.CreateWithContents("this is plain input");
			using var jsonTempFile = TempFile.CreateWithContents("{\"input\":\"this is json input\"}");
			using var multiPartContent = new MultipartFormDataContent
			{
				{ new StreamContent(new FileStream(plainInputTempFile.Path, FileMode.Open, FileAccess.Read)), "plain", "plain" },
				{ new StreamContent(new FileStream(jsonTempFile.Path, FileMode.Open, FileAccess.Read)), "json", "json" },
			};

			request.Content = multiPartContent;
			using var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
			response.IsSuccessStatusCode.Should().BeTrue();
		}
	}
}
