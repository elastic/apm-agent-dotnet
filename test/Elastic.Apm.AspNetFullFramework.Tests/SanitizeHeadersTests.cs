using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class SanitizeHeadersTests : TestsBase
	{
		public SanitizeHeadersTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper) { }

		/// <summary>
		/// Makes sure that with the default settings the `pwd` HTTP header is sanitized
		/// </summary>
		/// <param name="sampleAppUrlPathData"></param>
		/// <returns></returns>
		[AspNetFullFrameworkTheory]
		[MemberData(nameof(AllSampleAppUrlPaths))]
		public async Task Test(SampleAppUrlPathData sampleAppUrlPathData)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("pwd", "123");
			var url = Consts.SampleApp.RootUrl + "/" + sampleAppUrlPathData.RelativeUrlPath;
			var response = await httpClient.GetAsync(url);

			response.StatusCode.Should().Be(sampleAppUrlPathData.StatusCode);

			await WaitAndCustomVerifyReceivedData(data =>
			{
				data.Transactions.First().Context.Request.Headers["pwd"].Should().NotBeNull();
				data.Transactions.First().Context.Request.Headers["pwd"].Should().Be("[REDACTED]");
			});
		}
	}
}
