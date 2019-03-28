using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SampleAspNetCoreApp.Data;
using SampleAspNetCoreApp.Models;

namespace SampleAspNetCoreApp.Controllers
{
	public class HomeController : Controller
	{
		private readonly SampleDataContext _sampleDataContext;

		public HomeController(SampleDataContext sampleDataContext) => _sampleDataContext = sampleDataContext;

		public async Task<IActionResult> Index()
		{
			_sampleDataContext.Database.Migrate();
			var model = _sampleDataContext.Users.Select(item => item.Name).ToList();

			try
			{
				var httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.Add("User-Agent", "APM-Sample-App");
				var responseMsg = await httpClient.GetAsync("https://api.github.com/repos/elastic/apm-agent-dotnet");
				var responseStr = await responseMsg.Content.ReadAsStringAsync();
				ViewData["stargazers_count"] = JObject.Parse(responseStr)["stargazers_count"];
			}
			catch
			{
				Console.WriteLine("Failed HTTP GET elastic.co");
			}

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> AddNewUser([FromForm] string enteredName)
		{
			if (string.IsNullOrEmpty(enteredName))
				throw new ArgumentNullException(nameof(enteredName));

			_sampleDataContext.Users.Add(
				new User
				{
					Name = enteredName
				});

			await _sampleDataContext.SaveChangesAsync();

			return Redirect("/Home/Index");
		}

		public IActionResult SimplePage()
		{
			Response.Headers.Add("X-Additional-Header", "For-Elastic-Apm-Agent");
			return View();
		}

		public async Task<IActionResult> DistributedTracingMiniSample()
		{
			var httpClient = new HttpClient();

			try
			{
				var retVal = await httpClient.GetAsync("http://localhost:5050/api/values");

				var resultInStr = await retVal.Content.ReadAsStringAsync();
				var list = resultInStr.Split(',');
				return View(list.ToList());
			}
			catch (Exception e)
			{
				ViewData["Fail"] =
					$"Failed calling http://localhost:5050/api/values, make sure the WebApiSample listens on localhost:5050, {e.Message}";
				return View();
			}
		}

		public async Task<IActionResult> ChartPage()
		{
			var csvDataReader = new CsvDataReader($"Data{System.IO.Path.DirectorySeparatorChar}HistoricalData");

			var historicalData =
				await Agent.Tracer.CurrentTransaction.CaptureSpan("ReadData", "csvRead", async () => await csvDataReader.GetHistoricalQuotes("ESTC"));

			return View(historicalData);
		}

		public IActionResult Privacy() => View();

		public IActionResult AddNewUser() => View();

		public async Task<IActionResult> FailingOutGoingHttpCall()
		{
			var client = new HttpClient();
			var result = await client.GetAsync("http://dsfklgjdfgkdfg.mmmm");
			Console.WriteLine(result.IsSuccessStatusCode);

			return Ok();
		}

		public IActionResult TriggerError()
		{
			Agent.Tracer.CurrentTransaction.Tags["foo"] = "bar";
			throw new Exception("This is a test exception!");
		}


		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
