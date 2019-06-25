using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class MetadataTests : TestsBase
	{
		private readonly IApmLogger _logger;

		public MetadataTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) =>
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(MetadataTests));

		[AspNetFullFrameworkFact]
		public async Task AspNetVersionTest()
		{
			var pageThatThrows = SampleAppUrlPaths.ThrowsInvalidOperationPage;
			var response = await SendGetRequestToSampleAppAndVerifyResponseStatusCode(pageThatThrows.RelativeUrlPath, pageThatThrows.StatusCode);
			var aspNetVersionFromErrorPage = GetAspNetVersionFromErrorPage(await response.Content.ReadAsStringAsync());

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(pageThatThrows, receivedData);

				receivedData.Metadata.Should().NotBeEmpty();
				foreach (var metadata in receivedData.Metadata)
				{
					metadata.Service.Framework.Name.Should().Be("ASP.NET");
					metadata.Service.Framework.Version.Should().Be(aspNetVersionFromErrorPage);
				}
			});
		}

		private string GetAspNetVersionFromErrorPage(string errorPage)
		{
			//             <hr width=100% size=1 color=silver>
			//
			//             <b>Version Information:</b>&nbsp;Microsoft .NET Framework Version:4.0.30319; ASP.NET Version:4.7.3282.0
			//
			//             </font>
			//
			//     </body>
			// </html>
			// <!--
			// [NullReferenceException]: Object reference not set to an instance of an object.
			//    at Elastic.Apm.AspNetFullFramework.ElasticApmModule.SetServiceInformation(Service service) in ...\ElasticApmModule.cs:line 57
			// ...
			// -->

			var htmlClosingTagIndex = errorPage.LastIndexOf("</html>", StringComparison.OrdinalIgnoreCase);
			var aspNetVersionKey = "ASP.NET Version:";
			var aspNetVersionKeyIndex = errorPage.Substring(0, htmlClosingTagIndex).LastIndexOf(aspNetVersionKey, StringComparison.OrdinalIgnoreCase);
			var errorPageFromAspNetVersion = errorPage.Substring(aspNetVersionKeyIndex + aspNetVersionKey.Length,
				htmlClosingTagIndex - (aspNetVersionKeyIndex + aspNetVersionKey.Length));
			char[] eolChars = { '\n', '\r' };
			var aspNetVersionEol = errorPageFromAspNetVersion.IndexOfAny(eolChars);
			return errorPageFromAspNetVersion.Substring(0, aspNetVersionEol).Trim();
		}


		[AspNetFullFrameworkFact]
		public async Task ServiceRuntimeTest()
		{
			var page = SampleAppUrlPaths.GetDotNetRuntimeDescriptionPage;
			var response = await SendGetRequestToSampleAppAndVerifyResponseStatusCode(page.RelativeUrlPath, page.StatusCode);

			VerifyDataReceivedFromAgent(receivedData =>
			{
				TryVerifyDataReceivedFromAgent(page, receivedData);

				receivedData.Metadata.Should().NotBeEmpty();
				foreach (var metadata in receivedData.Metadata)
				{
					metadata.Service.Runtime.Name.Should().Be(Runtime.DotNetFullFrameworkName);
					var sampleAppDotNetRuntimeDescription =
						response.Headers.GetValues(HomeController.DotNetRuntimeDescriptionHttpHeaderName).Single();
					sampleAppDotNetRuntimeDescription.Should().StartWith(PlatformDetection.DotNetFullFrameworkDescriptionPrefix);
					metadata.Service.Runtime.Version.Should()
						.Be(PlatformDetection.GetDotNetRuntimeVersionFromDescription(
							sampleAppDotNetRuntimeDescription,
							_logger,
							PlatformDetection.DotNetFullFrameworkDescriptionPrefix));
				}
			});
		}
	}
}
