// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class TransactionNameTests : TestsBase
	{
		public TransactionNameTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper) { }

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Controller_Action_When_Mvc_Controller_Action()
		{
			var pathData = SampleAppUrlPaths.HomePage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET Home/Index");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Area_Controller_Action_When_Mvc_Area_Controller_Action()
		{
			var pathData = SampleAppUrlPaths.MyAreaHomePage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET MyArea/Home/Index");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Controller_Action_When_Mvc_Controller_Action_Returns_404_ActionResult()
		{
			var pathData = SampleAppUrlPaths.NotFoundPage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET Home/NotFound");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Unknown_Route_When_Mvc_Controller_Action_Does_Not_Exist()
		{
			var pathData = SampleAppUrlPaths.PageThatDoesNotExist;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET unknown route");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Controller_Action_When_Mvc_Controller_Action_Throws_HttpException_404()
		{
			var pathData = SampleAppUrlPaths.ThrowsHttpException404PageRelativePath;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET Home/ThrowsHttpException404");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Path_When_Mvc_Controller_Action_Throws_InvalidOperationException()
		{
			var pathData = SampleAppUrlPaths.ThrowsInvalidOperationPage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET Home/ThrowsInvalidOperation");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Controller_When_WebApi_Controller_Action()
		{
			var pathData = SampleAppUrlPaths.WebApiPage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be("GET WebApi");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Path_When_Webforms_Page()
		{
			var pathData = SampleAppUrlPaths.WebformsPage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be($"GET {pathData.Uri.AbsolutePath}");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Path_When_Routed_Webforms_Page()
		{
			var pathData = SampleAppUrlPaths.RoutedWebformsPage;
			await SendGetRequestToSampleAppAndVerifyResponse(pathData.Uri, pathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.Single();
				transaction.Name.Should().Be($"GET {pathData.Uri.AbsolutePath}");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Path_When_Asmx_Soap11_Request()
		{
			var pathData = SampleAppUrlPaths.CallSoapServiceProtocolV11;
			var action = "Ping";

			var request = new HttpRequestMessage(HttpMethod.Post, pathData.Uri)
			{
				Content = new StringContent($@"<?xml version=""1.0"" encoding=""utf-8""?>
                <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                  <soap:Body>
                    <{action} xmlns=""http://tempuri.org/"" />
                  </soap:Body>
                </soap:Envelope>", Encoding.UTF8, "text/xml")
			};

			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
			request.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
			request.Headers.Add("SOAPAction", $"http://tempuri.org/{action}");

			var response = await HttpClient.SendAsync(request);
			response.IsSuccessStatusCode.Should().BeTrue();

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				receivedData.Transactions.First().Name.Should().Be($"POST {pathData.Uri.AbsolutePath} {action}");
			});
		}

		[AspNetFullFrameworkFact]
		public async Task MultiPart_Async_Post_Should_Return_200_OK()
		{
			var pathData = SampleAppUrlPaths.MultiPartPostApiEndpoint;

			var request = new HttpRequestMessage(HttpMethod.Post, pathData.Uri);

			var multiPartContent = new MultipartFormDataContent("TestBoundryParameter");
			var fileStream = System.IO.File.OpenRead("README.md");
			multiPartContent.Add(new StreamContent(fileStream), "README.md", "README.md");
			multiPartContent.Headers.Add("Content-Type", "application/octet-stream");
			request.Content = multiPartContent;

			var response = await HttpClient.SendAsync(request);
			response.IsSuccessStatusCode.Should().BeTrue();
		}

		[AspNetFullFrameworkFact]
		public async Task Name_Should_Be_Path_When_Asmx_Soap12_Request()
		{
			var pathData = SampleAppUrlPaths.CallSoapServiceProtocolV12;
			var action = "Ping";

			var request = new HttpRequestMessage(HttpMethod.Post, pathData.Uri)
			{
				Content = new StringContent($@"<?xml version=""1.0"" encoding=""utf-8""?>
				<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
					<soap12:Body>
					<{action} xmlns=""http://tempuri.org/"" />
					</soap12:Body>
				</soap12:Envelope>", Encoding.UTF8, "application/soap+xml")
			};

			request.Headers.Accept.Clear();
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

			var response = await HttpClient.SendAsync(request);
			response.IsSuccessStatusCode.Should().BeTrue();

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				receivedData.Transactions.First().Name.Should().Be($"POST {pathData.Uri.AbsolutePath} {action}");
			});
		}
	}
}
