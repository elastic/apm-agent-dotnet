using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using Elastic.Apm;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	/// Note that this application is used by Elastic.Apm.AspNetFullFramework.Tests so changing it might break the tests
	public class HomeController : Controller
	{
		internal const string AboutPageRelativePath = HomePageRelativePath + "/" + nameof(About);
		internal const string ContactPageRelativePath = HomePageRelativePath + "/" + nameof(Contact);
		internal const string ReturnBadRequestPageRelativePath = HomePageRelativePath + "/" + nameof(ReturnBadRequest);
		internal const string CallReturnBadRequestPageRelativePath = HomePageRelativePath + "/" + nameof(CallReturnBadRequest);
		internal const string CustomSpanThrowsInternalMethodName = nameof(CustomSpanThrowsInternal);
		internal const string CustomSpanThrowsMethodName = nameof(CustomSpanThrows);
		internal const string CustomSpanThrowsPageRelativePath = HomePageRelativePath + "/" + nameof(CustomSpanThrows);
		internal const int DummyHttpStatusCode = 599;
		internal const string ExceptionMessage = "For testing purposes";
		internal const string HomePageRelativePath = "Home";
		internal const string TestSpanAction = "test_span_action";
		internal const string TestSpanName = "test_span_name";
		internal const string TestSpanSubtype = "test_span_subtype";
		internal const string TestSpanType = "test_span_type";
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

		internal static void CustomSpanThrowsInternal() => throw new InvalidOperationException(ExceptionMessage);

		public ActionResult CustomSpanThrows()
		{
			Agent.Tracer.CurrentTransaction.CaptureSpan(TestSpanName, TestSpanType, (Action)CustomSpanThrowsInternal, TestSpanSubtype,
				TestSpanAction);
			return null;
		}

		public async Task<ActionResult> ThrowsNameCouldNotBeResolved()
		{
			var result = await new HttpClient().GetAsync("http://dsfklgjdfgkdfg.mmmm");
			Console.WriteLine(result.IsSuccessStatusCode);
			return null;
		}

		public ActionResult ReturnBadRequest() =>
			new HttpStatusCodeResult(HttpStatusCode.BadRequest,
				$"/{nameof(ReturnBadRequest)} always returns 400 (Bad Request) - for testing purposes");

		public async Task<ActionResult> CallReturnBadRequest()
		{
			var response = await new HttpClient().GetAsync(GetUrlForMethod(nameof(ReturnBadRequest)));
			return new HttpStatusCodeResult(DummyHttpStatusCode,
				$"/{nameof(CallReturnBadRequest)} called /{nameof(ReturnBadRequest)} and " +
				$"received HTTP status code {(int)response.StatusCode} ({response.StatusCode})");
		}

		private Uri GetUrlForMethod(string methodName)
		{
			var currentUrl = HttpContext.ApplicationInstance.Request.Url.ToString();
			var homeIndex = currentUrl.LastIndexOf(HomePageRelativePath);
			return new Uri($"{currentUrl.Substring(0, homeIndex + HomePageRelativePath.Length)}/{methodName}");
		}
	}
}
