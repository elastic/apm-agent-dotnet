using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			return View();
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public async Task<ActionResult> Contact()
		{
			var httpClient = new HttpClient();
			Console.WriteLine("Getting `https://elastic.co'...");
			var httpCallResponse = await httpClient.GetAsync("https://elastic.co");
			Console.WriteLine($"Response status code from `https://elastic.co' - {httpCallResponse.StatusCode}");
			ViewBag.Message = $"Your contact page. Response code from https://elastic.co is {httpCallResponse.StatusCode}.";

			return View();
		}
	}
}
