using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
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
		private SerializerMockPayloadSender _capturedPayload;
		private HttpClient _client;
		private readonly IApmLogger _logger;
		private readonly WebApplicationFactory<Startup> _factory;
		private ApmAgent _agent;

		public SanitizeFieldNamesTests(WebApplicationFactory<Startup> factory)
		{
			_logger = new TestLogger();
			_factory = factory;
		}

		private void CreateAgent(string sanitizeFieldNames = null)
		{
			var configSnapshot = sanitizeFieldNames == null ? new MockConfigSnapshot(_logger, captureBody: "all") : new MockConfigSnapshot(_logger, captureBody: "all", sanitizeFieldNames: sanitizeFieldNames);
			_capturedPayload = new SerializerMockPayloadSender(configSnapshot);

			var agentComponents = new TestAgentComponents(
				_logger,
				configSnapshot, _capturedPayload,
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
		[InlineData("*mySecurityHeader", new[] { "abcmySecurityHeader", "amySecurityHeader", "mySecurityHeader" })]
		[InlineData("mySecurityHeader*", new[] { "mySecurityHeaderAbc", "mySecurityHeaderAbc1", "mySecurityHeader" })]
		[InlineData("*mySecurityHeader*", new[] { "AbcmySecurityHeaderAbc", "amySecurityHeaderA", "mySecurityHeader" })]
		[InlineData("mysecurityheader", new[] { "mySECURITYHeader" })]
		[Theory]
		public async Task CustomSanitizeFieldNameSettingWithHeaders(string sanitizeFieldNames, string[] headerNames)
		{
			CreateAgent(sanitizeFieldNames);

			foreach (var header in headerNames) _client.DefaultRequestHeaders.Add(header, "123");

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();

			foreach (var header in headerNames)
				_capturedPayload.FirstTransaction.Context.Request.Headers[header].Should().Be("[REDACTED]");
		}

		/// <summary>
		/// Sends an HTTP GET with the given headers and makes sure all of them are sanitized.
		/// This test applies custom values to the SanitizeFieldNames setting. It also turns on case sensitivity.
		/// </summary>
		/// <param name="sanitizeFieldNames"></param>
		/// <param name="headerName"></param>
		/// <param name="shouldBeSanitized"></param>
		/// <returns></returns>
		[InlineData("mysecurityheader", "mySECURITYHeader", true)]
		[InlineData("(?-i)mysecurityheader", "mySECURITYHeader", false)]
		[InlineData("(?-i)mySECURITYheader", "mySECURITYheader", true)]
		[InlineData("(?-i)*mySECURITYheader", "TestmySECURITYheader", true)]
		[InlineData("(?-i)*mySECURITYheader", "TestmysecURITYheader", false)]
		[InlineData("(?-i)mySECURITYheader*", "mySECURITYheaderTest", true)]
		[InlineData("(?-i)mySECURITYheader*", "mysecURITYheaderTest", false)]
		[InlineData("(?-i)*mySECURITYheader*", "TestmySECURITYheaderTest", true)]
		[InlineData("(?-i)*mySECURITYheader*", "TestmysecURITYheaderTest", false)]
		[Theory]
		public async Task CustomSanitizeFieldNameSettingWithCaseSensitivityWithHeaders(string sanitizeFieldNames, string headerName,
			bool shouldBeSanitized
		)
		{
			CreateAgent(sanitizeFieldNames);
			const string headerValue = "123";

			_client.DefaultRequestHeaders.Add(headerName, headerValue);

			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();

			_capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be(shouldBeSanitized ? "[REDACTED]" : headerValue);
		}

		/// <summary>
		/// Tests the default SanitizeFieldNames -
		/// It sends an HTTP GET with the given headers and makes sure all of them are
		/// sanitized.
		/// </summary>
		/// <param name="headerName"></param>
		/// <returns></returns>
		[InlineData("password")]
		[InlineData("pwd")]
		[InlineData("passwd")]
		[InlineData("secret")]
		[InlineData("secretkey")] //*key
		[InlineData("usertokensecret")] //*token*
		[InlineData("usersessionid")] //*session
		[InlineData("secretcreditcard")] //*credit*
		[InlineData("creditcardnumber")] //*card
		[Theory]
		public async Task DefaultsWithHeaders(string headerName)
		{
			CreateAgent();
			_client.DefaultRequestHeaders.Add(headerName, "123");
			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be("[REDACTED]");
		}

		/// <summary>
		/// ASP.NET Core seems to rewrite the name of these headers (so <code>authorization</code> becomes <code>Authorization</code>).
		/// Our "by default case insensitivity" still works, the only difference is that if we send a header with name
		/// <code>authorization</code> it'll be captured as <code>Authorization</code> (capital letter).
		///
		/// Otherwise same as <see cref="DefaultsWithHeaders"/>.
		///
		/// </summary>
		/// <param name="headerName">The original header name sent in the HTTP GET</param>
		/// <param name="returnedHeaderName">The header name (with capital letter) seen on the request in ASP.NET Core</param>
		/// <returns></returns>
		[InlineData("authorization", "Authorization")]
		[InlineData("set-cookie", "Set-Cookie")]
		[Theory]
		public async Task DefaultsWithKnownHeaders(string headerName, string returnedHeaderName)
		{
			CreateAgent();
			_client.DefaultRequestHeaders.Add(headerName, "123");
			await _client.GetAsync("/Home/SimplePage");

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstTransaction.Context.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Headers[returnedHeaderName].Should().Be("[REDACTED]");
		}


		/// <summary>
		/// Sends an HTTP post with content type application/x-www-form-urlencoded
		/// Makes sure that fields in the body are sanitized accordingly
		/// </summary>
		/// <param name="formName"></param>
		/// <returns></returns>
		[InlineData("password")]
		[InlineData("pwd")]
		[InlineData("passwd")]
		[InlineData("secret")]
		[InlineData("secretkey")] //*key
		[InlineData("usertokensecret")] //*token*
		[InlineData("usersessionid")] //*session
		[InlineData("secretcreditcard")] //*credit*
		[InlineData("creditcardnumber")] //*card
		[Theory]
		public async Task DefaultWithRequestBodyNoError(string formName)
		{
			CreateAgent();

			var nvc = new List<KeyValuePair<string, string>>();
			nvc.Add(new KeyValuePair<string, string>("Input1", "test1"));
			nvc.Add(new KeyValuePair<string, string>(formName, "test2"));

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
			var res = await _client.SendAsync(req);

			res.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.Errors.Should().BeNullOrEmpty();
			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.FirstTransaction.Context.Request.Body.Should().Be($"Input1=test1&{formName}=[REDACTED]");
		}

		[InlineData("password", true)]
		[InlineData("pwd", true)]
		[InlineData("Input", false)]
		[Theory]
		public async Task DefaultWithRequestBodySingleValueNoError(string formName, bool shouldBeSanitized)
		{
			CreateAgent();

			var nvc = new List<KeyValuePair<string, string>>();
			nvc.Add(new KeyValuePair<string, string>(formName, "test"));

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
			var res = await _client.SendAsync(req);

			res.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.Errors.Should().BeNullOrEmpty();
			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.FirstTransaction.Context.Request.Body.Should().Be(shouldBeSanitized ? $"{formName}=[REDACTED]" : $"{formName}=test");
		}

		/// <summary>
		/// Same as <see cref="DefaultWithRequestBodyNoError"/> except this time the request ends up with an error
		/// </summary>
		/// <param name="formName"></param>
		/// <returns></returns>
		[InlineData("password")]
		[InlineData("pwd")]
		[InlineData("secret")]
		[InlineData("secretkey")] //*key
		[InlineData("usertokensecret")] //*token*
		[InlineData("usersessionid")] //*session
		[InlineData("secretcreditcard")] //*credit*
		[InlineData("creditcardnumber")] //*card
		[Theory]
		public async Task DefaultWithRequestBodyWithError(string formName)
		{
			CreateAgent();

			var nvc = new List<KeyValuePair<string, string>>();
			nvc.Add(new KeyValuePair<string, string>("Input1", "test1"));
			nvc.Add(new KeyValuePair<string, string>(formName, "test2"));

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

			_capturedPayload.Transactions.Should().ContainSingle();
			_capturedPayload.FirstError.Should().NotBeNull();
			_capturedPayload.FirstTransaction.Context.Request.Body.Should().Be($"Input1=test1&{formName}=[REDACTED]");
		}

		/// <summary>
		/// Applies custom values to sanitizeFieldNames and makes sure that the request body is sanitized accordingly.
		/// </summary>
		/// <param name="sanitizeFieldNames"></param>
		/// <param name="formName"></param>
		/// <param name="shouldBeSanitized"></param>
		/// <returns></returns>
		[InlineData("mysecurityheader", "mySECURITYHeader", true)]
		[InlineData("(?-i)mysecurityheader", "mySECURITYHeader", false)]
		[InlineData("(?-i)mySECURITYheader", "mySECURITYheader", true)]
		[InlineData("(?-i)*mySECURITYheader", "TestmySECURITYheader", true)]
		[InlineData("(?-i)*mySECURITYheader", "TestmysecURITYheader", false)]
		[InlineData("(?-i)mySECURITYheader*", "mySECURITYheaderTest", true)]
		[InlineData("(?-i)mySECURITYheader*", "mysecURITYheaderTest", false)]
		[InlineData("(?-i)*mySECURITYheader*", "TestmySECURITYheaderTest", true)]
		[InlineData("(?-i)*mySECURITYheader*", "TestmysecURITYheaderTest", false)]
		[Theory]
		public async Task CustomWithRequestBodyNoError(string sanitizeFieldNames, string formName, bool shouldBeSanitized)
		{
			CreateAgent(sanitizeFieldNames);

			var nvc = new List<KeyValuePair<string, string>>();
			nvc.Add(new KeyValuePair<string, string>("Input1", "test1"));
			nvc.Add(new KeyValuePair<string, string>(formName, "test2"));

			var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
			var res = await _client.SendAsync(req);

			res.IsSuccessStatusCode.Should().BeTrue();

			_capturedPayload.Errors.Should().BeNullOrEmpty();
			_capturedPayload.Transactions.Should().ContainSingle();

			_capturedPayload.FirstTransaction.Context.Request.Body.Should()
				.Be(shouldBeSanitized ? $"Input1=test1&{formName}=[REDACTED]" : $"Input1=test1&{formName}=test2");
		}
	}
}
