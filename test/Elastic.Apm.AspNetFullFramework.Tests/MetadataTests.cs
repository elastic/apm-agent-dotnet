using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection("AspNetFullFrameworkTests")]
	public class MetadataTests : TestsBase
	{
		public MetadataTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

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
	}
}
