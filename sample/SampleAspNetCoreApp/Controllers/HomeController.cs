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

		private bool GetCaptureControllerActionAsSpanFromQueryString()
		{
			const string captureControllerActionAsSpanQueryStringKey = "captureControllerActionAsSpan";
			var captureControllerActionAsSpanQueryStringValues = HttpContext.Request.Query[captureControllerActionAsSpanQueryStringKey];
			if (captureControllerActionAsSpanQueryStringValues.Count > 1)
			{
				throw new ArgumentException($"{captureControllerActionAsSpanQueryStringKey} query string key should have at most one value" +
					$", instead it's values: {captureControllerActionAsSpanQueryStringValues}",
					captureControllerActionAsSpanQueryStringKey);
			}

			// ReSharper disable once SimplifyConditionalTernaryExpression
			return captureControllerActionAsSpanQueryStringValues.Count == 0 ? false : bool.Parse(captureControllerActionAsSpanQueryStringValues[0]);
		}

		public Task<IActionResult> Index() =>
			SafeCaptureSpan<IActionResult>(GetCaptureControllerActionAsSpanFromQueryString(),
				"Index_span_name", "Index_span_type", async () =>
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
				});

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

		public string TraceId() => Activity.Current.TraceId.ToString();

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
				// ReSharper disable once StringLiteralTypo
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
			if (Agent.Tracer.CurrentTransaction != null) Agent.Tracer.CurrentTransaction.Labels["foo"] = "bar";
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

		public const string PostResponseBody = "somevalue";

		[HttpPost]
		[Route("api/Home/Post")]
		public ActionResult<string> Post() => PostResponseBody;

		[HttpPost]
		[Route("api/Home/PostError")]
		public ActionResult<string> PostError() => throw new Exception("This is a post method test exception!");

		/// <summary>
		/// Test for: https://github.com/elastic/apm-agent-dotnet/issues/460
		/// </summary>
		/// <param name="filter">A parameter with a little bit more complex data type coming through the request body</param>
		/// <returns>HTTP200 if the parameter is available in the method (aka not <code>null</code>), HTTP500 otherwise </returns>
		[HttpPost("api/Home/Send")]
		public IActionResult Send([FromBody] BaseReportFilter<SendMessageFilter> filter) => filter == null ? StatusCode(500) : Ok();
	}

	public class BaseReportFilter<T>
	{
		public T ReportFilter { get; set; }
	}

	public class SendMessageFilter
	{
		public string Body { get; set; }
		public string MediaType { get; set; }
		public List<string> Recipients { get; set; }
		public string SenderApplicationCode { get; set; }
	}
}
