// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class MetadataTests : TestsBase
	{
		private const string ThisClassName = nameof(AspNetFullFramework) + "." + nameof(Tests) + "." + nameof(MetadataTests);

		private readonly IApmLogger _logger;

		public MetadataTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) =>
			_logger = LoggerBase.Scoped(ThisClassName);

		[AspNetFullFrameworkFact]
		public async Task AspNetVersionTest()
		{
			var pageThatThrows = SampleAppUrlPaths.ThrowsInvalidOperationPage;
			var sampleAppResponse = await SendGetRequestToSampleAppAndVerifyResponse(pageThatThrows.Uri, pageThatThrows.StatusCode);
			var aspNetVersionFromErrorPage = GetAspNetVersionFromErrorPage(sampleAppResponse.Content);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(pageThatThrows, receivedData);

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
			var textAfterAspNetVersionKey = errorPage.Substring(aspNetVersionKeyIndex + aspNetVersionKey.Length,
				htmlClosingTagIndex - (aspNetVersionKeyIndex + aspNetVersionKey.Length));
			char[] eolChars = { '\n', '\r' };
			var aspNetVersionEolIndex = textAfterAspNetVersionKey.IndexOfAny(eolChars);
			return textAfterAspNetVersionKey.Substring(0, aspNetVersionEolIndex).Trim();
		}


		[AspNetFullFrameworkFact]
		public async Task ServiceRuntimeTest()
		{
			var page = SampleAppUrlPaths.GetDotNetRuntimeDescriptionPage;
			var sampleAppResponse = await SendGetRequestToSampleAppAndVerifyResponse(page.Uri, page.StatusCode);

			await WaitAndCustomVerifyReceivedData(receivedData =>
			{
				VerifyReceivedDataSharedConstraints(page, receivedData);

				receivedData.Metadata.Should().NotBeEmpty();
				foreach (var metadata in receivedData.Metadata)
				{
					metadata.Service.Runtime.Name.Should().Be(Runtime.DotNetFullFrameworkName);
					var sampleAppDotNetRuntimeDescription =
						sampleAppResponse.Headers.GetValues(HomeController.DotNetRuntimeDescriptionHttpHeaderName).Single();
					sampleAppDotNetRuntimeDescription.Should().StartWith(Runtime.DotNetFullFrameworkName);
					metadata.Service.Runtime.Version.Should()
						.Be(PlatformDetection.GetDotNetRuntimeVersionFromDescription(
							sampleAppDotNetRuntimeDescription,
							_logger, Runtime.DotNetFullFrameworkName));
				}
			});
		}
	}
}
