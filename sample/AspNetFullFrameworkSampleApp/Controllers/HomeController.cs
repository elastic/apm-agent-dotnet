using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index() => View();

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public async Task<ActionResult> Contact()
		{
			var httpClient = new HttpClient();

			var localHostUrl = new Uri(HttpContext.ApplicationInstance.Request.Url.ToString().Replace("/Home/Contact", "/Home/About"));
			var responseFromLocalHost = await GetContentFromUrl(localHostUrl);
			var elasticCoUrl = new Uri("https://elastic.co");
			var responseFromElasticCo = await GetContentFromUrl(elasticCoUrl);

			ViewBag.Message =
				$"Your contact page. " +
				$" Response code from `{localHostUrl}' is {responseFromLocalHost.StatusCode}. " +
				$" Response code from `{elasticCoUrl}' is {responseFromElasticCo.StatusCode}.";

			return View();

			async Task<HttpResponseMessage> GetContentFromUrl(Uri urlToGet)
			{
				Console.WriteLine($"Getting `{urlToGet}'...");
				var response = await httpClient.GetAsync(urlToGet);
				Console.WriteLine($"Response status code from `{urlToGet}' - {response.StatusCode}");
				return response;
			}
		}
	}
}
