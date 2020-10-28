// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using AspNetFullFrameworkSampleApp.Data;
using Elastic.Apm;
using Elastic.Apm.Api;

namespace AspNetFullFrameworkSampleApp.Controllers
{
	/// Note that this application is used by Elastic.Apm.AspNetFullFramework.Tests so changing it might break the tests
	public class HomeController : ControllerBase
	{
		internal const string HomePageRelativePath = "Home";
		internal const string ConcurrentDbTestPageRelativePath = HomePageRelativePath + "/" + nameof(ConcurrentDbTest);
		internal const string ContactPageRelativePath = HomePageRelativePath + "/" + nameof(Contact);
		internal const string CustomChildSpanThrowsPageRelativePath = HomePageRelativePath + "/" + nameof(CustomChildSpanThrows);
		internal const string AboutPageRelativePath = HomePageRelativePath + "/" + nameof(About);
		internal const string ChildHttpSpanWithResponseForbiddenPath = HomePageRelativePath + "/" + nameof(ChildHttpSpanWithResponseForbidden);
		internal const string CallReturnBadRequestPageRelativePath = HomePageRelativePath + "/" + nameof(CallReturnBadRequest);
		internal const string CustomSpanThrowsPageRelativePath = HomePageRelativePath + "/" + nameof(CustomSpanThrows);
		internal const string DbOperationOutsideTransactionTestPageRelativePath =
			HomePageRelativePath + "/" + nameof(DbOperationOutsideTransactionTest);
		internal const string FailingDbCallTestPageRelativePath = HomePageRelativePath + "/" + nameof(FailingDbCallTest);
		internal const string GenNSpansPageRelativePath = HomePageRelativePath + "/" + nameof(GenNSpans);
		internal const string GetDotNetRuntimeDescriptionPageRelativePath = HomePageRelativePath + "/" + nameof(GetDotNetRuntimeDescription);
		internal const string NotFoundPageRelativePath = HomePageRelativePath + "/" + nameof(NotFound);
		internal const string ReturnBadRequestPageRelativePath = HomePageRelativePath + "/" + nameof(ReturnBadRequest);
		internal const string SimpleDbTestPageRelativePath = HomePageRelativePath + "/" + nameof(SimpleDbTest);
		internal const string ThrowsInvalidOperationPageRelativePath = HomePageRelativePath + "/" + nameof(ThrowsInvalidOperation);
		internal const string ThrowsHttpException404PageRelativePath = HomePageRelativePath + "/" + nameof(ThrowsHttpException404);

		internal const string CaptureControllerActionAsSpanQueryStringKey = "captureControllerActionAsSpan";
		internal const int ConcurrentDbTestNumberOfIterations = 10;
		internal const string ConcurrentDbTestSpanType = "concurrent";

		internal const string ContactSpanPrefix = nameof(Contact);
		internal const string CustomSpanThrowsInternalMethodName = nameof(CustomSpanThrowsInternal);
		internal const int DbOperationOutsideTransactionTestStatusCode = (int)HttpStatusCode.Accepted;

		internal const string DotNetRuntimeDescriptionHttpHeaderName = "DotNetRuntimeDescription";
		internal const int DummyHttpStatusCode = 599;
		internal const string ExceptionMessage = "For testing purposes";

		internal const int FailingDbCallTestStatusCode = (int)HttpStatusCode.OK;

		internal const string NumberOfSpansQueryStringKey = "numberOfSpans";

		internal const string SpanActionSuffix = "_span_action";
		internal const string SpanNameSuffix = "_span_name";
		internal const string SpanSubtypeSuffix = "_span_subtype";
		internal const string SpanTypeSuffix = "_span_type";

		internal const string TestChildSpanPrefix = "test_child";
		internal const string TestSpanPrefix = "test";

		internal static readonly Uri ChildHttpCallToExternalServiceUrl = new Uri("https://elastic.co");
		internal static readonly Uri ChildHttpSpanWithResponseForbiddenUrl = new Uri("https://httpstat.us/403");

		public ActionResult Index() => View();

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		private bool GetCaptureControllerActionAsSpan() => bool.Parse(GetQueryStringValue(CaptureControllerActionAsSpanQueryStringKey, "false"));

		public async Task<ActionResult> ChildHttpSpanWithResponseForbidden()
		{
			//see https://github.com/elastic/apm-agent-dotnet/issues/443
			var httpClient = new HttpClient();
			var res = await httpClient.GetAsync(ChildHttpSpanWithResponseForbiddenUrl);
			var retVal = new ContentResult { Content = res.IsSuccessStatusCode.ToString() };
			return retVal;
		}

		public Task<ActionResult> Contact()
		{
			var httpClient = new HttpClient();

			return SafeCaptureSpan<ActionResult>($"{ContactSpanPrefix}{SpanNameSuffix}", $"{ContactSpanPrefix}{SpanTypeSuffix}", async () =>
			{
				var callToThisAppUrl =
					new Uri(HttpContext.ApplicationInstance.Request.Url.ToString().Replace(ContactPageRelativePath, AboutPageRelativePath));
				var responseFromLocalHost = await GetContentFromUrl(callToThisAppUrl);
				var callToExternalServiceUrl = ChildHttpCallToExternalServiceUrl;
				var responseFromElasticCo = await GetContentFromUrl(callToExternalServiceUrl);

				ViewBag.Message =
					"Your contact page. " +
					$" Response code from `{callToThisAppUrl}' is {responseFromLocalHost.StatusCode}. " +
					$" Response code from `{callToExternalServiceUrl}' is {responseFromElasticCo.StatusCode}.";

				return View();
			}, $"{ContactSpanPrefix}{SpanSubtypeSuffix}", $"{ContactSpanPrefix}{SpanActionSuffix}", GetCaptureControllerActionAsSpan());

			async Task<HttpResponseMessage> GetContentFromUrl(Uri urlToGet)
			{
				Console.WriteLine($"Getting `{urlToGet}'...");
				var response = await httpClient.GetAsync(urlToGet);
				Console.WriteLine($"Response status code from `{urlToGet}' - {response.StatusCode}");
				return response;
			}
		}

		public ActionResult Sample(int id) => Content(id.ToString());

		public ActionResult NotFound() => HttpNotFound();

		internal static async Task<ActionResult> CustomSpanThrowsInternal()
		{
			await Task.Delay(1);
			throw new InvalidOperationException(ExceptionMessage);
		}

		private static Task<T> SafeCaptureSpan<T>(string spanName, string spanType, Func<Task<T>> func, string subType = null, string action = null,
			bool doCaptureSpan = true
		)
		{
			if (!doCaptureSpan || Agent.Tracer.CurrentTransaction == null) return func();

			var currentExecutionSegment = Agent.Tracer.CurrentSpan ?? (IExecutionSegment)Agent.Tracer.CurrentTransaction;
			return currentExecutionSegment.CaptureSpan(spanName, spanType, func, subType, action);
		}

		public Task<ActionResult> CustomSpanThrows() =>
			SafeCaptureSpan($"{TestSpanPrefix}{SpanNameSuffix}", $"{TestSpanPrefix}{SpanTypeSuffix}", CustomSpanThrowsInternal,
				$"{TestSpanPrefix}{SpanSubtypeSuffix}", $"{TestSpanPrefix}{SpanActionSuffix}");

		public Task<ActionResult> CustomChildSpanThrows() =>
			SafeCaptureSpan($"{TestSpanPrefix}{SpanNameSuffix}", $"{TestSpanPrefix}{SpanTypeSuffix}",
				() => SafeCaptureSpan($"{TestChildSpanPrefix}{SpanNameSuffix}", $"{TestChildSpanPrefix}{SpanTypeSuffix}",
					CustomSpanThrowsInternal, $"{TestChildSpanPrefix}{SpanSubtypeSuffix}", $"{TestChildSpanPrefix}{SpanActionSuffix}"),
				$"{TestSpanPrefix}{SpanSubtypeSuffix}", $"{TestSpanPrefix}{SpanActionSuffix}");

		public async Task<ActionResult> ThrowsNameCouldNotBeResolved()
		{
			var result = await new HttpClient().GetAsync("http://dsfklgjdfgkdfg.mmmm");
			Console.WriteLine(result.IsSuccessStatusCode);
			return null;
		}

		public ActionResult ThrowsHttpException404()
		{
			var notFound = (int)HttpStatusCode.NotFound;
			throw new HttpException(notFound, $"/{nameof(ThrowsHttpException404)} always returns " +
				$"{notFound} ({HttpStatusCode.NotFound}) - for testing purposes");
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

		public HttpStatusCodeResult SimpleDbTest()
		{
			const string sampleDataName = "simple_DB_test_sample_data_name";

			using (var dbCtx = new SampleDataDbContext())
			{
				dbCtx.Set<SampleData>().Add(new SampleData { Name = sampleDataName });
				dbCtx.SaveChanges();
			}

			using (var dbCtx = new SampleDataDbContext())
			{
				if (dbCtx.Set<SampleData>().Count() != 1)
					throw new InvalidOperationException($"dbCtx.Set<SampleData>().Count(): {dbCtx.Set<SampleData>().Count()}");
				if (dbCtx.Set<SampleData>().First().Name != sampleDataName)
					throw new InvalidOperationException($"dbCtx.Set<SampleData>().First().Name: {dbCtx.Set<SampleData>().First().Name}");
			}

			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		public HttpStatusCodeResult FailingDbCallTest()
		{
			try
			{
				using var dbCtx = new SampleDataDbContext();
				dbCtx.Database.ExecuteSqlCommand("Select * From NonExistingTable");
			}
			catch
			{
				//ignore exception
			}

			return new HttpStatusCodeResult(HttpStatusCode.OK);
		}

		public async Task<ActionResult> ConcurrentDbTest()
		{
			// Spans should overlap (actually alternatively strictly "contain" the corresponding span on the other concurrent branch)
			// [---------------- INSERT A.0 ------------------------] ...    [-- INSERT A.N.before --] [-- INSERT A.N.after --]
			//   [-- INSERT B.0.before --] [-- INSERT B.0.after --]    ... [---------------- INSERT B.N -----------------------]
			const int numberOfConcurrentIterations = ConcurrentDbTestNumberOfIterations;

			// Create table before concurrent inserts
			using (var dbCtx = new SampleDataDbContext())
			{
				if (dbCtx.Set<SampleData>().Count() != 0)
					throw new InvalidOperationException($"dbCtx.Set<SampleData>().Count(): {dbCtx.Set<SampleData>().Count()}");
			}

			var syncBarrier = new Barrier(2);
			DbInterception.Add(new ConcurrentDbTestDbCommandInterceptor(syncBarrier));

			var currentTx = Agent.Tracer.CurrentTransaction;
			await Task.WhenAll(Task.Run(() => ConcurrentSpan("A", currentTx)), Task.Run(() => ConcurrentSpan("B", currentTx)));

			using (var dbCtx = new SampleDataDbContext())
			{
				var sampleDataList = dbCtx.Set<SampleData>().ToList();
				if (sampleDataList.Count != 3 * numberOfConcurrentIterations)
					throw new InvalidOperationException($"sampleDataList.Count: {sampleDataList.Count}");

				for (var i = 0; i < numberOfConcurrentIterations; ++i)
				{
					var (containingPrefix, containedPrefix) = i % 2 == 0 ? ("A", "B") : ("B", "A");
					var expectedNames = new List<string>
					{
						$"{containedPrefix}.{i}.before", $"{containingPrefix}.{i}", $"{containedPrefix}.{i}.after"
					};
					var actualNames = new List<string> { sampleDataList[3 * i].Name, sampleDataList[3 * i + 1].Name, sampleDataList[3 * i + 2].Name };
					if (!actualNames.SequenceEqual(expectedNames))
					{
						throw new InvalidOperationException(
							$"actualNames: {string.Join(", ", actualNames)}, expectedNames: {string.Join(", ", expectedNames)}");
					}
				}
			}

			return new HttpStatusCodeResult(HttpStatusCode.OK);

			void ConcurrentSpan(string branchId, ITransaction tx)
			{
				tx.CaptureSpan(branchId, ConcurrentDbTestSpanType, () =>
				{
					for (var i = 0; i < numberOfConcurrentIterations; ++i)
					{
						var isIndexEven = i % 2 == 0;
						var isContainedSpan = branchId == "B" ? isIndexEven : !isIndexEven;

						var numberOfInserts = isContainedSpan ? 2 : 1;
						for (var j = 0; j < numberOfInserts; ++j)
						{
							if (isContainedSpan) syncBarrier.SignalAndWait();
							using (var dbCtx = new SampleDataDbContext( /* attachedState: */ isContainedSpan))
							{
								var suffix = isContainedSpan ? j == 0 ? ".before" : ".after" : "";
								dbCtx.Set<SampleData>().Add(new SampleData { Name = $"{branchId}.{i}{suffix}" });
								dbCtx.SaveChanges();
							}
							if (isContainedSpan) syncBarrier.SignalAndWait();
						}
					}
				});
			}
		}

		public async Task<ActionResult> DbOperationOutsideTransactionTest()
		{
			const int simpleDbTestExpectedResult = (int)HttpStatusCode.OK;

			// Suppress the flow of AsyncLocal so that SimpleDbTest called below doesn't have current transaction
			ExecutionContext.SuppressFlow();
			var simpleDbTestResult = await Task.Run(SimpleDbTest);

			if (simpleDbTestResult.StatusCode != simpleDbTestExpectedResult)
			{
				throw new InvalidOperationException($"{nameof(SimpleDbTest)}"
					+ $" expected result: {simpleDbTestExpectedResult}"
					+ $" actual result: {simpleDbTestResult.StatusCode}");
			}
			return new HttpStatusCodeResult(HttpStatusCode.Accepted);
		}

		public ActionResult GenNSpans()
		{
			var numberOfSpans = int.Parse(GetQueryStringValue(NumberOfSpansQueryStringKey, /* defaultValue: */ "0"));

			for (var i = 0; i < numberOfSpans; ++i)
				Agent.Tracer.CurrentTransaction.CaptureSpan($"Span_#{i}_name", $"Span_#{i}_type", () => { });

			return new HttpStatusCodeResult(HttpStatusCode.Created);
		}

		private string GetQueryStringValue(string key, string defaultValue)
		{
			var values = HttpContext.Request.QueryString.GetValues(key);
			if (values == null || values.Length == 0) return defaultValue;

			if (values.Length > 1)
				throw new ArgumentException($"{key} should appear in query string at most once", key);

			return values[0];
		}

		private Uri GetUrlForMethod(string methodName)
		{
			var currentUrl = HttpContext.ApplicationInstance.Request.Url.ToString();
			var homeIndex = currentUrl.LastIndexOf(HomePageRelativePath, StringComparison.OrdinalIgnoreCase);
			return new Uri($"{currentUrl.Substring(0, homeIndex + HomePageRelativePath.Length)}/{methodName}");
		}

		private class ConcurrentDbTestDbCommandInterceptor : IDbCommandInterceptor
		{
			private readonly Barrier _syncBarrier;

			internal ConcurrentDbTestDbCommandInterceptor(Barrier syncBarrier) => _syncBarrier = syncBarrier;

			public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext) =>
				CommandStarting(command, interceptionContext);

			public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext) =>
				CommandEnded(command, interceptionContext);

			public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext) =>
				CommandStarting(command, interceptionContext);

			public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext) =>
				CommandEnded(command, interceptionContext);

			public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext) =>
				CommandStarting(command, interceptionContext);

			public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext) =>
				CommandEnded(command, interceptionContext);

			// ReSharper disable once UnusedParameter.Local
			private void CommandStarting<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext) =>
				ProcessCommandEvent(interceptionContext);

			// ReSharper disable once UnusedParameter.Local
			private void CommandEnded<TResult>(DbCommand command, DbCommandInterceptionContext<TResult> interceptionContext) =>
				ProcessCommandEvent(interceptionContext);

			private void ProcessCommandEvent<TResult>(DbCommandInterceptionContext<TResult> interceptionContext)
			{
				if (!(interceptionContext.DbContexts.SingleOrDefault() is SampleDataDbContext sampleDataDbContext)) return;
				if (sampleDataDbContext.AttachedState == null) return;

				var isContainedSpan = (bool)sampleDataDbContext.AttachedState;
				if (isContainedSpan) return;

				// Wait for the first/second contained span's start
				_syncBarrier.SignalAndWait();

				// Wait for the first/second contained span's end
				_syncBarrier.SignalAndWait();
			}
		}
	}
}
