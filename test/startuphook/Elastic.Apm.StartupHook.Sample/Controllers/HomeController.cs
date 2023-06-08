using System;
using System.Diagnostics;
using Elastic.Apm.StartupHook.Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Elastic.Apm.StartupHook.Sample.Controllers
{
	public class HomeController : Controller
	{
		// ReSharper disable once NotAccessedField.Local
		private readonly ILogger<HomeController> _logger;

		public HomeController(ILogger<HomeController> logger) => _logger = logger;

		public IActionResult Index() => View();

		public IActionResult Privacy() => View();

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

		public IActionResult Exception() => throw new Exception("Exception thrown from controller action");
	}
}
