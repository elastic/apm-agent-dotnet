// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests.Soap
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class SoapRequestTests : TestsBase
	{
		public SoapRequestTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper) { }

		/// <summary>
		/// Tests that the reading of the input stream to get the action name for a SOAP 1.2 request
		/// does not cause an exception to be thrown when the framework deserializes the input stream
		/// to parse the parameters for the web method.
		/// </summary>
		[AspNetFullFrameworkFact]
		public async Task Name_Should_Should_Not_Throw_Exception_When_Asmx_Soap12_Request()
		{
			var pathData = SampleAppUrlPaths.CallSoapServiceProtocolV12;
			var action = "Input";

			var input = @"This is the input";
			var request = new HttpRequestMessage(HttpMethod.Post, pathData.Uri)
			{
				Content = new StringContent($@"<?xml version=""1.0"" encoding=""utf-8""?>
				<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
					<soap12:Body>
					<{action} xmlns=""http://tempuri.org/"">
						<input>{input}</input>
					</{action}>
					</soap12:Body>
				</soap12:Envelope>", Encoding.UTF8, "application/soap+xml")
			};

			request.Headers.Accept.Clear();
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

			var response = await HttpClient.SendAsync(request);
			response.IsSuccessStatusCode.Should().BeTrue();

			var responseText = await response.Content.ReadAsStringAsync();
			responseText.Should().Contain(input);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(1);
				var transaction = receivedData.Transactions.First();
				transaction.Name.Should().Be($"POST {pathData.Uri.AbsolutePath} {action}");
			});
		}
	}
}
