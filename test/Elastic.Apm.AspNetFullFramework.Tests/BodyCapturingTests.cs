// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class BodyCapturingTests : TestsBase
	{
		public BodyCapturingTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper)
		{
		}

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public Task TextData_RequestBody_IsCaptured(string httpMethod)
			=> Assert_TextData_RequestBody_IsCaptured(httpMethod, SampleAppUrlPaths.HomePage);

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public Task TextData_RequestBody_IsCaptured_OnError(string httpMethod)
			=> Assert_TextData_RequestBody_IsCaptured(httpMethod, SampleAppUrlPaths.CustomSpanThrowsExceptionPage);

		private async Task Assert_TextData_RequestBody_IsCaptured(string httpMethod, SampleAppUrlPathData pathData)
		{
			var textBody = "just plain text";
			var content = new StringContent(textBody, Encoding.UTF8, "text/plain");

			await SendRequestToSampleAppAndVerifyResponse(new HttpMethod(httpMethod), pathData.Uri, pathData.StatusCode, httpContent: content);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Context.Request.Body.Should().Be(textBody);
			});
		}

		[AspNetFullFrameworkFact]
		public async Task TextData_RequestBody_IsCaptured_And_Truncated()
		{
			var pathData = SampleAppUrlPaths.HomePage;
			var textFragment = "just plain text ... ";
			var sb = new StringBuilder();
			for (var i = 0; i < RequestBodyStreamHelper.RequestBodyMaxLength; i++)
			{
				sb.Append(textFragment);
			}
			var content = new StringContent(sb.ToString(), Encoding.UTF8, "text/plain");

			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode, httpContent: content);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				var body = transaction.Context.Request.Body.ToString();
				body.Should().Contain(textFragment);
				body.Length.Should().Be(RequestBodyStreamHelper.RequestBodyMaxLength);
			});
		}

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public Task JsonData_RequestBody_IsCaptured(string httpMethod)
			=> Assert_JsonData_RequestBody_IsCaptured(httpMethod, SampleAppUrlPaths.HomePage);

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public Task JsonData_RequestBody_IsCaptured_OnError(string httpMethod)
			=> Assert_JsonData_RequestBody_IsCaptured(httpMethod, SampleAppUrlPaths.CustomSpanThrowsExceptionPage);

		private async Task Assert_JsonData_RequestBody_IsCaptured(string httpMethod, SampleAppUrlPathData pathData)
		{
			var jsonBody = "{ 'foo' : 'bar', 'number' : 42 }";
			var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

			await SendRequestToSampleAppAndVerifyResponse(new HttpMethod(httpMethod), pathData.Uri, pathData.StatusCode, httpContent: content);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Context.Request.Body.Should().Be(jsonBody);
			});
		}

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public Task FormData_RequestBody_IsCaptured(string httpMethod)
			=> Assert_FormData_RequestBody_IsCaptured(httpMethod, SampleAppUrlPaths.HomePage);

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public Task FormData_RequestBody_IsCaptured_OnError(string httpMethod)
			=> Assert_FormData_RequestBody_IsCaptured(httpMethod, SampleAppUrlPaths.CustomSpanThrowsExceptionPage);

		private async Task Assert_FormData_RequestBody_IsCaptured(string httpMethod, SampleAppUrlPathData pathData)
		{
			var content = new FormUrlEncodedContent(new Dictionary<string, string>()
			{
				["foo"] = "bar",
				["number"] = "23",
			});

			await SendRequestToSampleAppAndVerifyResponse(new HttpMethod(httpMethod), pathData.Uri, pathData.StatusCode, httpContent: content);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Context.Request.Body.Should().Be("foo=bar&number=23");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task FormData_RequestBody_IsCaptured_And_Sanitized()
		{
			var pathData = SampleAppUrlPaths.HomePage;
			var content = new FormUrlEncodedContent(new Dictionary<string, string>()
			{
				["foo"] = "bar",
				["password"] = "abcd1234",
				["number"] = "23",
			});

			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode, httpContent: content);
			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Context.Request.Body.Should().Be("foo=bar&password=[REDACTED]&number=23");
			});
		}

		[AspNetFullFrameworkTheory]
		[InlineData("GET")]
		[InlineData("POST")]
		public async Task MultipartFormData_RequestBody_IsCaptured(string httpMethod)
		{
			using (var tempFile = new TempFile())
			{
				var pathData = SampleAppUrlPaths.HomePage;

				var data = $"just some test data in the file";
				var bytes = Encoding.UTF8.GetBytes(data);
				using (var stream = new FileStream(tempFile.Path, FileMode.OpenOrCreate, FileAccess.Write))
				{
					stream.Write(bytes);
				}
				var content = new MultipartFormDataContent
				{
					{ new StreamContent(new FileStream(tempFile.Path, FileMode.Open, FileAccess.Read)), "file", "file" }
				};
				await SendRequestToSampleAppAndVerifyResponse(new HttpMethod(httpMethod), pathData.Uri, pathData.StatusCode, httpContent: content);
				await WaitAndCustomVerifyReceivedData(receivedData =>
				{
					receivedData.Transactions.Count.Should().Be(1);
					var transaction = receivedData.Transactions.Single();
					transaction.Context.Request.Body.ToString().Should().Contain(data);
				});
			}
		}
	}
}
