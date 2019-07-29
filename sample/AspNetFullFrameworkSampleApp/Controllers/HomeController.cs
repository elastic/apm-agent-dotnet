using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Mvc;
using Elastic.Apm;
using Elastic.Apm.Api;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	/// Note that this application is used by Elastic.Apm.AspNetFullFramework.Tests so changing it might break the tests
	public class HomeController : Controller
	{
		internal const string AboutPageRelativePath = HomePageRelativePath + "/" + nameof(About);
		internal const string CallReturnBadRequestPageRelativePath = HomePageRelativePath + "/" + nameof(CallReturnBadRequest);
		internal const string ContactPageRelativePath = HomePageRelativePath + "/" + nameof(Contact);

		internal const string CustomChildSpanThrowsPageRelativePath = HomePageRelativePath + "/" + nameof(CustomChildSpanThrows);

		internal const string CustomSpanThrowsInternalMethodName = nameof(CustomSpanThrowsInternal);
		internal const string CustomSpanThrowsMethodName = nameof(CustomSpanThrows);
		internal const string CustomSpanThrowsPageRelativePath = HomePageRelativePath + "/" + nameof(CustomSpanThrows);

		internal const string DotNetRuntimeDescriptionHttpHeaderName = "DotNetRuntimeDescription";
		internal const int DummyHttpStatusCode = 599;
		internal const string ExceptionMessage = "For testing purposes";
		internal const string GetDotNetRuntimeDescriptionPageRelativePath = HomePageRelativePath + "/" + nameof(GetDotNetRuntimeDescription);
		internal const string HomePageRelativePath = "Home";
		internal const string ReturnBadRequestPageRelativePath = HomePageRelativePath + "/" + nameof(ReturnBadRequest);
		internal const string TestChildSpanAction = "test_child_span_action";
		internal const string TestChildSpanName = "test_child_span_name";
		internal const string TestChildSpanSubtype = "test_child_span_subtype";
		internal const string TestChildSpanType = "test_child_span_type";
		internal const string TestSpanAction = "test_span_action";
		internal const string TestSpanName = "test_span_name";
		internal const string TestSpanSubtype = "test_span_subtype";
		internal const string TestSpanType = "test_span_type";
		internal const string ThrowsInvalidOperationPageRelativePath = HomePageRelativePath + "/" + nameof(ThrowsInvalidOperation);
		internal static readonly Uri ChildHttpCallToExternalServiceUrl = new Uri("https://elastic.co");

		public ActionResult Index() => View();

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public async Task<ActionResult> Contact()
		{
			var httpClient = new HttpClient();

			var callToThisAppUrl =
				new Uri(HttpContext.ApplicationInstance.Request.Url.ToString().Replace(ContactPageRelativePath, AboutPageRelativePath));
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

		internal static async Task<ActionResult> CustomSpanThrowsInternal()
		{
			await Task.Delay(1);
			throw new InvalidOperationException(ExceptionMessage);
		}

		private static Task<T> SafeCaptureSpan<T>(string spanName, string spanType, Func<Task<T>> func, string subType = null, string action = null)
		{
			if (Agent.Tracer.CurrentTransaction == null) return func();
			var currentExecutionSegment = (Agent.Tracer.CurrentSpan ?? (IExecutionSegment)Agent.Tracer.CurrentTransaction);
			return currentExecutionSegment.CaptureSpan(spanName, spanType, func, subType, action);
		}

		public Task<ActionResult> CustomSpanThrows() =>
			SafeCaptureSpan(TestSpanName, TestSpanType, CustomSpanThrowsInternal, TestSpanSubtype, TestSpanAction);

		public Task<ActionResult> CustomChildSpanThrows() =>
			SafeCaptureSpan(TestSpanName, TestSpanType,
				() => SafeCaptureSpan(TestChildSpanName, TestChildSpanType, CustomSpanThrowsInternal, TestChildSpanSubtype, TestChildSpanAction),
				TestSpanSubtype, TestSpanAction);

		public async Task<ActionResult> ThrowsNameCouldNotBeResolved()
		{
			var result = await new HttpClient().GetAsync("http://dsfklgjdfgkdfg.mmmm");
			Console.WriteLine(result.IsSuccessStatusCode);
			return null;
		}

		public ActionResult ThrowsInvalidOperation()
			=> throw new InvalidOperationException($"/{nameof(ThrowsInvalidOperation)} always returns " +
				$"{(int)HttpStatusCode.InternalServerError} ({HttpStatusCode.InternalServerError}) - for testing purposes");

		public ActionResult ReturnBadRequest() =>
			new HttpStatusCodeResult(HttpStatusCode.BadRequest,
				$"/{nameof(ReturnBadRequest)} always returns {(int)HttpStatusCode.BadRequest} ({HttpStatusCode.BadRequest}) - for testing purposes");

		public async Task<ActionResult> CallReturnBadRequest()
		{
			var response = await new HttpClient().GetAsync(GetUrlForMethod(nameof(ReturnBadRequest)));
			return new HttpStatusCodeResult(DummyHttpStatusCode,
				$"/{nameof(CallReturnBadRequest)} called /{nameof(ReturnBadRequest)} and " +
				$"received HTTP status code {(int)response.StatusCode} ({response.StatusCode})");
		}

		public ActionResult GetDotNetRuntimeDescription()
		{
			HttpContext.Response.Headers.Add(DotNetRuntimeDescriptionHttpHeaderName, RuntimeInformation.FrameworkDescription);
			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		private Uri GetUrlForMethod(string methodName)
		{
			var currentUrl = HttpContext.ApplicationInstance.Request.Url.ToString();
			var homeIndex = currentUrl.LastIndexOf(HomePageRelativePath, StringComparison.OrdinalIgnoreCase);
			return new Uri($"{currentUrl.Substring(0, homeIndex + HomePageRelativePath.Length)}/{methodName}");
		}
	}
}
