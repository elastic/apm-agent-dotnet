using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
			var model = _sampleDataContext.SampleTable.Select(item => item.Name).ToList();

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

		/// <summary>
		/// In order to test if relationship between spans is maintained correctly by the agent,
		/// this method has the ability to start a span by passing `captureControllerActionAsSpan` = `true` to it.
		/// In that case spans created by EF Core auto instrumentation in the method body should be sub spans
		/// of the manually created span.
		/// </summary>
		[HttpPost]
		public Task<IActionResult> AddSampleData(IFormCollection formFields)
		{
			var captureControllerActionAsSpanAsString = formFields["captureControllerActionAsSpan"];
			// ReSharper disable once SimplifyConditionalTernaryExpression
			var captureControllerActionAsSpan = string.IsNullOrEmpty(captureControllerActionAsSpanAsString)
				? false
				: bool.Parse(captureControllerActionAsSpanAsString);
			var enteredName = formFields["enteredName"];
			return SafeCaptureSpan<IActionResult>(captureControllerActionAsSpan, "AddSampleData_span_name", "AddSampleData_span_type", async () =>
			{
				if (string.IsNullOrEmpty(enteredName))
					throw new ArgumentNullException(nameof(enteredName));

				_sampleDataContext.SampleTable.Add(
					new SampleData { Name = enteredName });

				await _sampleDataContext.SaveChangesAsync();

				return Redirect("/Home/Index");
			});
		}

		private static Task<T> SafeCaptureSpan<T>(bool captureControllerActionAsSpan, string spanName, string spanType, Func<Task<T>> spanBody)
		{
			if (!captureControllerActionAsSpan || Agent.Tracer.CurrentTransaction == null) return spanBody();

			return (Agent.Tracer.CurrentSpan ?? (IExecutionSegment)Agent.Tracer.CurrentTransaction).CaptureSpan(spanName, spanType, spanBody);
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
				var list = JsonConvert.DeserializeObject<List<string>>(resultInStr);
				return View(list);
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
			var csvDataReader = new CsvDataReader($"Data{Path.DirectorySeparatorChar}HistoricalData");

			var historicalData =
				await Agent.Tracer.CurrentTransaction.CaptureSpan("ReadData", "csvRead", async () => await csvDataReader.GetHistoricalQuotes("ESTC"));

			return View(historicalData);
		}

		public IActionResult Privacy() => View();

		public IActionResult AddSampleData() => View();

		public async Task<IActionResult> FailingOutGoingHttpCall()
		{
			var client = new HttpClient();
			var result = await client.GetAsync("http://dsfklgjdfgkdfg.mmmm");
			Console.WriteLine(result.IsSuccessStatusCode);

			return Ok();
		}

		public IActionResult TriggerError()
		{
			if (Agent.Tracer.CurrentTransaction != null) Agent.Tracer.CurrentTransaction.Tags["foo"] = "bar";
			throw new Exception("This is a test exception!");
		}

		//Used as test for optional route parameters
		public IActionResult Sample(int id) => Ok(id);

		public IActionResult TransactionWithCustomName()
		{
			if (Agent.Tracer.CurrentTransaction != null) Agent.Tracer.CurrentTransaction.Name = "custom";
			return Ok();
		}

		public IActionResult TransactionWithCustomNameUsingRequestInfo()
		{
			if (Agent.Tracer.CurrentTransaction != null)
				Agent.Tracer.CurrentTransaction.Name = $"{HttpContext.Request.Method} {HttpContext.Request.Path}";
			return Ok();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
