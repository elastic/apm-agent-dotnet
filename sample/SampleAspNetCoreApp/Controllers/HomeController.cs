using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
			//TODO: Show this on the real UI
			foreach (var item in _sampleDataContext.Users) Console.WriteLine(item.Name);

			try
			{
				//TODO: turn this into a more realistic sample
				var httpClient = new HttpClient();
				var responseMsg = await httpClient.GetAsync("https://elastic.co");
				var responseStr = await responseMsg.Content.ReadAsStringAsync();
				Console.WriteLine(responseStr.Length);
			}
			catch
			{
				Console.WriteLine("Failed HTTP GET elastic.co");
			}

			return View();
		}

		[HttpPost]
		public async Task<IActionResult> AddNewUser(string enteredName)
		{
			_sampleDataContext.Users.Add(
				new User
				{
					Name = "TestName"
				});

			//TODO: get the real data
			await _sampleDataContext.SaveChangesAsync();

			return View();
		}

		public IActionResult About()
		{
			ViewData["Message"] = "Your application description page.";

			return View();
		}

		public IActionResult Contact()
		{
			ViewData["Message"] = "Your contact page.";

			return View();
		}

		public IActionResult Privacy() => View();

		public IActionResult AddNewUser() => View();

		public async Task<IActionResult> FailingOutGoingHttpCall()
		{
			var client = new HttpClient();
			var result = await client.GetAsync("http://dsfklgjdfgkdfg.mmmm");
			Console.WriteLine(result.IsSuccessStatusCode);

			return View();
		}

		public IActionResult TriggerError()
		{
			throw new Exception("This is a test exception!");

			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
