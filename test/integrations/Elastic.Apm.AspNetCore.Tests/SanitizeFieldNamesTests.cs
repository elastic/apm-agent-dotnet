// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Sends HTTP requests and makes sure the agent sanitizes the HTTP headers and the request body according to the
	/// sanitizeFieldNames setting.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class SanitizeFieldNamesTests : IClassFixture<WebApplicationFactory<Startup>>
	{
		public enum Tests
		{
			CustomSanitizeFieldNameSettingWithHeaders,
			CustomSanitizeFieldNameSettingWithCaseSensitivityWithHeaders,
			DefaultsWithHeaders,
			DefaultsWithKnownHeaders,
			DefaultWithRequestBodyNoError,
			DefaultWithRequestBodySingleValueNoError,
			DefaultWithRequestBodyWithError,
			CustomWithRequestBodyNoError
		}

		private readonly WebApplicationFactory<Startup> _factory;
		private readonly IApmLogger _logger;

		public SanitizeFieldNamesTests(WebApplicationFactory<Startup> factory)
		{
			_logger = new TestLogger();
			_factory = factory;
		}

		private ApmAgent _agent;
		private MockPayloadSender _capturedPayload;
		private HttpClient _client;

		public static IEnumerable<object[]> GetData(Tests test)
		{
			var testData = new List<object[]>();

			// Create test data based on test name
			switch (test)
			{
				case Tests.CustomSanitizeFieldNameSettingWithHeaders:
					testData.Add(new object[] { "*mySecurityHeader", new[] { "abcmySecurityHeader", "amySecurityHeader", "mySecurityHeader" } });
					testData.Add(new object[] { "mySecurityHeader*", new[] { "mySecurityHeaderAbc", "mySecurityHeaderAbc1", "mySecurityHeader" } });
					testData.Add(new object[] { "*mySecurityHeader*", new[] { "AbcmySecurityHeaderAbc", "amySecurityHeaderA", "mySecurityHeader" } });
					testData.Add(new object[] { "mySecurityHeader", new[] { "mySECURITYHeader" } });
					break;
				case Tests.CustomSanitizeFieldNameSettingWithCaseSensitivityWithHeaders:
					testData.Add(new object[] { "mysecurityheader", "mySECURITYHeader", true });
					testData.Add(new object[] { "(?-i)mysecurityheader", "mySECURITYHeader", false });
					testData.Add(new object[] { "(?-i)mySECURITYheader", "mySECURITYheader", true });
					testData.Add(new object[] { "(?-i)*mySECURITYheader", "TestmySECURITYheader", true });
					testData.Add(new object[] { "(?-i)*mySECURITYheader", "TestmysecURITYheader", false });
					testData.Add(new object[] { "(?-i)mySECURITYheader*", "mySECURITYheaderTest", true });
					testData.Add(new object[] { "(?-i)mySECURITYheader*", "mysecURITYheaderTest", false });
					testData.Add(new object[] { "(?-i)*mySECURITYheader*", "TestmySECURITYheaderTest", true });
					testData.Add(new object[] { "(?-i)*mySECURITYheader*", "TestmysecURITYheaderTest", false });
					break;
				case Tests.DefaultsWithHeaders:
					testData.Add(new object[] { "password" });
					testData.Add(new object[] { "pwd" });
					testData.Add(new object[] { "passwd" });
					testData.Add(new object[] { "secret" });
					testData.Add(new object[] { "secretkey" }); //*key
					testData.Add(new object[] { "usertokensecret" }); //*token*
					testData.Add(new object[] { "usersessionid" }); //*session
					testData.Add(new object[] { "secretcreditcard" }); //*credit*
					testData.Add(new object[] { "creditcardnumber" }); //*card
					testData.Add(new object[] { "X-Ms-Client-Principal" }); //*principal*
					testData.Add(new object[] { "Ms-Client-Principal-Id" }); //*principal*
					testData.Add(new object[] { "X-Ms-Client-Principal-Idp" }); //*principal*
					testData.Add(new object[] { "X-Ms-Client-Principal-Name" }); //*principal*
					break;
				case Tests.DefaultsWithKnownHeaders:
					testData.Add(new object[] { "authorization", "Authorization" }); // *auth*
					testData.Add(new object[] { "authority", "authority" }); // *auth*
					testData.Add(new object[] { "auth", "auth" }); // *auth*
					testData.Add(new object[] { "set-cookie", "Set-Cookie" });
					break;
				case Tests.DefaultWithRequestBodyNoError:
					testData.Add(new object[] { "password" });
					testData.Add(new object[] { "pwd" });
					testData.Add(new object[] { "passwd" });
					testData.Add(new object[] { "secret" });
					testData.Add(new object[] { "secretkey" }); //*key
					testData.Add(new object[] { "usertokensecret" }); //*token*
					testData.Add(new object[] { "usersessionid" }); //*session
					testData.Add(new object[] { "secretcreditcard" }); //*credit*
					testData.Add(new object[] { "creditcardnumber" }); //*card
					break;
				case Tests.DefaultWithRequestBodySingleValueNoError:
					testData.Add(new object[] { "password", true });
					testData.Add(new object[] { "pwd", true });
					testData.Add(new object[] { "Input", false });
					break;
				case Tests.DefaultWithRequestBodyWithError:
					testData.Add(new object[] { "password" });
					testData.Add(new object[] { "pwd" });
					testData.Add(new object[] { "secret" });
					testData.Add(new object[] { "secretkey" }); //*key
					testData.Add(new object[] { "usertokensecret" }); //*token*
					testData.Add(new object[] { "usersessionid" }); //*session
					testData.Add(new object[] { "secretcreditcard" }); //*credit*
					testData.Add(new object[] { "creditcardnumber" }); //*card
					break;
				case Tests.CustomWithRequestBodyNoError:
					testData.Add(new object[] { "mysecurityheader", "mySECURITYHeader", true });
					testData.Add(new object[] { "(?-i)mysecurityheader", "mySECURITYHeader", false });
					testData.Add(new object[] { "(?-i)mySECURITYheader", "mySECURITYheader", true });
					testData.Add(new object[] { "(?-i)*mySECURITYheader", "TestmySECURITYheader", true });
					testData.Add(new object[] { "(?-i)*mySECURITYheader", "TestmysecURITYheader", false });
					testData.Add(new object[] { "(?-i)mySECURITYheader*", "mySECURITYheaderTest", true });
					testData.Add(new object[] { "(?-i)mySECURITYheader*", "mysecURITYheaderTest", false });
					testData.Add(new object[] { "(?-i)*mySECURITYheader*", "TestmySECURITYheaderTest", true });
					testData.Add(new object[] { "(?-i)*mySECURITYheader*", "TestmysecURITYheaderTest", false });
					break;
			}
			return testData;
		}

		private void CreateAgent(string sanitizeFieldNames = null)
		{
			var configSnapshot = sanitizeFieldNames == null
				? new MockConfiguration(_logger, captureBody: "all")
				: new MockConfiguration(_logger, captureBody: "all", sanitizeFieldNames: sanitizeFieldNames);

			_capturedPayload = new MockPayloadSender();

			var agentComponents = new TestAgentComponents(
				_logger,
				configSnapshot,
				_capturedPayload,
				new CurrentExecutionSegmentsContainer());

			_agent = new ApmAgent(agentComponents);
			_client = Helper.GetClient(_agent, _factory);
		}

		/// <summary>
		/// Sends an HTTP GET with the given headers and makes sure all of them are sanitized.
		/// This test applies custom values to the SanitizeFieldNames setting
		/// </summary>
		/// <param name="sanitizeFieldNames"></param>
		/// <param name="headerNames"></param>
		/// <returns></returns>
		[MemberData(nameof(GetData), Tests.CustomSanitizeFieldNameSettingWithHeaders)]
		[Theory]
		public async Task CustomSanitizeFieldNameSettingWithHeaders(string sanitizeFieldNames, string[] headerNames)
		{
			CreateAgent(sanitizeFieldNames);

			foreach (var header in headerNames)
				_client.DefaultRequestHeaders.Add(header, "123");

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();

			foreach (var header in headerNames)
				_capturedPayload.FirstTransaction.Context.Request.Headers[header].Should().Be(Apm.Consts.Redacted);
		}

		/// <summary>
		/// Sends a request to an app with default configs and makes sure the `foo` HTTP header is captured.
		/// Then updates the config with `sanitizeFieldNames=foo` and sends another request and makes sure header `foo` is
		/// redacted.
		/// </summary>
		/// <param name="useDiagnosticSourceOnly"></param>
		/// <returns></returns>
		[Fact]
		public async Task ChangeSanitizeFieldNamesAfterStart()
		{
			var startConfigSnapshot = new MockConfiguration(new NoopLogger());
			_capturedPayload = new MockPayloadSender();

			var agentComponents = new TestAgentComponents(
				_logger,
				startConfigSnapshot, _capturedPayload,
				new CurrentExecutionSegmentsContainer());

			_agent = new ApmAgent(agentComponents);
			_client = Helper.GetClient(_agent, _factory);

			_client.DefaultRequestHeaders.Add("foo", "bar");
			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();

			_capturedPayload.FirstTransaction.Context.Request.Headers["foo"].Should().Be("bar");

			_capturedPayload.Clear();

			//change config to sanitize headers with "foo"
			var updateConfigSnapshot = new MockConfiguration(
				new NoopLogger()
				, sanitizeFieldNames: "foo"
			);

			_agent.ConfigurationStore.CurrentSnapshot = updateConfigSnapshot;

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();

			_capturedPayload.FirstTransaction.Context.Request.Headers["foo"].Should().Be(Apm.Consts.Redacted);
		}

		///// <summary>
		///// Sends an HTTP GET with the given headers and makes sure all of them are sanitized.
		///// This test applies custom values to the SanitizeFieldNames setting. It also turns on case sensitivity.
		///// </summary>
		///// <param name="sanitizeFieldNames"></param>
		///// <param name="headerName"></param>
		///// <param name="shouldBeSanitized"></param>
		[Theory]
		[MemberData(nameof(GetData), Tests.CustomSanitizeFieldNameSettingWithCaseSensitivityWithHeaders)]
		public async Task CustomSanitizeFieldNameSettingWithCaseSensitivityWithHeaders(string sanitizeFieldNames, string headerName,
			bool shouldBeSanitized
		)
		{
			CreateAgent(sanitizeFieldNames);
			const string headerValue = "123";

			_client.DefaultRequestHeaders.Add(headerName, headerValue);

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be(shouldBeSanitized ? Apm.Consts.Redacted : headerValue);
		}

		///// <summary>
		///// Tests the default SanitizeFieldNames -
		///// It sends an HTTP GET with the given headers and makes sure all of them are
		///// sanitized.
		///// </summary>
		///// <param name="headerName"></param>
		///// <returns></returns>
		[Theory]
		[MemberData(nameof(GetData), Tests.DefaultsWithHeaders)]
		public async Task DefaultsWithHeaders(string headerName)
		{
			CreateAgent();
			_client.DefaultRequestHeaders.Add(headerName, "123");
			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be(Apm.Consts.Redacted);
		}

		/// <summary>
		/// Asserts that context on error is sanitized in case of HTTP calls.
		/// </summary>
		/// <param name="headerName"></param>
		[Theory]
		[MemberData(nameof(GetData), Tests.DefaultsWithHeaders)]
		public async Task SanitizeHeadersOnError(string headerName)
		{
			CreateAgent();
			_client.DefaultRequestHeaders.Add(headerName, "123");
			await _client.GetAsync("/Home/TriggerError");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be(Apm.Consts.Redacted);

			_capturedPayload.WaitForErrors();
			_capturedPayload.Errors.Should().NotBeEmpty();
			_capturedPayload.FirstError.Context.Should().NotBeNull();
			_capturedPayload.FirstError.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstError.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstError.Context.Request.Headers[headerName].Should().Be(Apm.Consts.Redacted);
		}

		///// <summary>
		///// ASP.NET Core seems to rewrite the name of these headers (so <code>authorization</code> becomes <code>Authorization</code>).
		///// Our "by default case insensitivity" still works, the only difference is that if we send a header with name
		///// <code>authorization</code> it'll be captured as <code>Authorization</code> (capital letter).
		/////
		///// Otherwise same as <see cref="DefaultsWithHeaders"/>.
		/////
		///// </summary>
		///// <param name="headerName">The original header name sent in the HTTP GET</param>
		///// <param name="returnedHeaderName">The header name (with capital letter) seen on the request in ASP.NET Core</param>
		///// <returns></returns>
		[MemberData(nameof(GetData), Tests.DefaultsWithKnownHeaders)]
		[Theory]
		public async Task DefaultsWithKnownHeaders(string headerName, string returnedHeaderName)
		{
			CreateAgent();
			_client.DefaultRequestHeaders.Add(headerName, "123");
			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers[returnedHeaderName].Should().Be(Apm.Consts.Redacted);
		}


		///// <summary>
		///// Sends an HTTP post with content type application/x-www-form-urlencoded
		///// Makes sure that fields in the body are sanitized accordingly
		///// </summary>
		///// <param name="formName"></param>
		///// <returns></returns>
		[MemberData(nameof(GetData), Tests.DefaultWithRequestBodyNoError)]
		[TheoryRequiresMvcTestingFix]
		public async Task DefaultWithRequestBodyNoError(string formName)
		{
			CreateAgent();

			var nvc = new List<KeyValuePair<string, string>>
			{
				new("Input1", "test1"), new(formName, "test2")
			};

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
			var res = await _client.SendAsync(req);

			res.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.WaitForErrors(TimeSpan.FromSeconds(1));
			_capturedPayload.Errors.Should().BeNullOrEmpty();

			_capturedPayload.FirstTransaction.Context.Request.Body.Should().Be($"Input1=test1&{formName}=[REDACTED]");
		}

		[MemberData(nameof(GetData), Tests.DefaultWithRequestBodySingleValueNoError)]
		[TheoryRequiresMvcTestingFix]
		public async Task DefaultWithRequestBodySingleValueNoError(string formName, bool shouldBeSanitized)
		{
			CreateAgent();

			var nvc = new List<KeyValuePair<string, string>> { new(formName, "test") };

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
			var res = await _client.SendAsync(req);

			res.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForErrors(TimeSpan.FromSeconds(5));
			_capturedPayload.Errors.Should().BeNullOrEmpty();
			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.FirstTransaction.Context.Request.Body.Should().Be(shouldBeSanitized ? $"{formName}=[REDACTED]" : $"{formName}=test");
		}

		///// <summary>
		///// Same as <see cref="DefaultWithRequestBodyNoError"/> except this time the request ends up with an error
		///// </summary>
		///// <param name="formName"></param>
		///// <returns></returns>
		[MemberData(nameof(GetData), Tests.DefaultWithRequestBodyWithError)]
		[TheoryRequiresMvcTestingFix]
		public async Task DefaultWithRequestBodyWithError(string formName)
		{
			CreateAgent();

			var nvc = new List<KeyValuePair<string, string>>
			{
				new("Input1", "test1"), new(formName, "test2")
			};

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/PostError") { Content = new FormUrlEncodedContent(nvc) };

			try
			{
				var res = await _client.SendAsync(req);
				res.IsSuccessStatusCode.Should().BeFalse();
			}
			catch
			{
				// exception is fine, it doesn't really matter what happens with the call, important is the captured body, which we assert on later.
			}

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.WaitForErrors();
			_capturedPayload.FirstError.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Body.Should().Be($"Input1=test1&{formName}=[REDACTED]");
		}

		///// <summary>
		///// Applies custom values to sanitizeFieldNames and makes sure that the request body is sanitized accordingly.
		///// </summary>
		///// <param name="sanitizeFieldNames"></param>
		///// <param name="formName"></param>
		///// <param name="shouldBeSanitized"></param>
		///// <returns></returns>
		[MemberData(nameof(GetData), Tests.CustomWithRequestBodyNoError)]
		[TheoryRequiresMvcTestingFix]
		public async Task CustomWithRequestBodyNoError(string sanitizeFieldNames, string formName, bool shouldBeSanitized)
		{
			CreateAgent(sanitizeFieldNames);

			var nvc = new List<KeyValuePair<string, string>>
			{
				new("Input1", "test1"), new(formName, "test2")
			};

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
			var res = await _client.SendAsync(req);

			res.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.WaitForErrors(TimeSpan.FromSeconds(1));
			_capturedPayload.Errors.Should().BeNullOrEmpty();

			_capturedPayload.FirstTransaction.Context.Request.Body.Should()
				.Be(shouldBeSanitized ? $"Input1=test1&{formName}=[REDACTED]" : $"Input1=test1&{formName}=test2");
		}

		[Fact]
		public async Task RedactCookieHeaders()
		{
			CreateAgent("set-cookie, mysecurecookie");

			await _client.GetAsync("/Home/CookiePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Response.Should().NotBeNull();

			_capturedPayload.FirstTransaction.Context.Response.Headers["Set-Cookie"].Should().Be(Apm.Consts.Redacted);

			_capturedPayload.Clear();

			// On this second request, the client should include the `Cookie` header on the request, containing our two cookies
			await _client.GetAsync("/Home/CookiePage");

			_capturedPayload.WaitForTransactions();
			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Response.Should().NotBeNull();

			_capturedPayload.FirstTransaction.Context.Request.Headers["Cookie"].Should().Be($"MySecureCookie={Apm.Consts.Redacted}; SafeCookie=123");
		}
	}
}
