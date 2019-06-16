using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	/// Note that this application is used by Elastic.Apm.AspNetFullFramework.Tests so changing it might break the tests
	public class HomeController : Controller
	{
		internal static readonly Uri ChildHttpCallToExternalServiceUrl = new Uri("https://elastic.co");
		internal const string HomePageRelativePath = "Home";
		internal const string ContactPageRelativePath = HomePageRelativePath + "/" + nameof(Contact);
		internal const string AboutPageRelativePath = HomePageRelativePath + "/" + nameof(About);

		public ActionResult Index() => View();

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public async Task<ActionResult> Contact()
		{
			var httpClient = new HttpClient();

			var callToThisAppUrl = new Uri(HttpContext.ApplicationInstance.Request.Url.ToString().Replace(ContactPageRelativePath, AboutPageRelativePath));
			var responseFromLocalHost = await GetContentFromUrl(callToThisAppUrl);
			var callToExternalServiceUrl = ChildHttpCallToExternalServiceUrl;
			var responseFromElasticCo = await GetContentFromUrl(callToExternalServiceUrl);

			ViewBag.Message =
				$"Your contact page. " +
				$" Response code from `{callToThisAppUrl}' is {responseFromLocalHost.StatusCode}. " +
				$" Response code from `{callToExternalServiceUrl}' is {responseFromElasticCo.StatusCode}.";

			return View();

			async Task<HttpResponseMessage> GetContentFromUrl(Uri urlToGet)
			{
				Console.WriteLine($"Getting `{urlToGet}'...");
				var response = await httpClient.GetAsync(urlToGet);
				Console.WriteLine($"Response status code from `{urlToGet}' - {response.StatusCode}");
				return response;
			}
		}

		public ActionResult ThrowsInvalidOperationException()
		{
			throw new InvalidOperationException("For testing purposes");
		}
	}
}
