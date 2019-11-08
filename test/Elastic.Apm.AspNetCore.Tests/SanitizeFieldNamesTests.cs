using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Tests.Factories;
using Elastic.Apm.AspNetCore.Tests.Fakes;
using Elastic.Apm.AspNetCore.Tests.Helpers;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using WebApiSample;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Sends HTTP requests and makes sure the agent sanitizes the HTTP headers and the request body according to the
	/// sanitizeFieldNames setting.
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class SanitizeFieldNamesTests : IClassFixture<CustomWebApplicationFactory<FakeAspNetCoreSampleAppStartup>>
	{
		private readonly WebApplicationFactory<Startup> _factory;

		public SanitizeFieldNamesTests(WebApplicationFactory<Startup> factory) => _factory = factory;

		private static ApmAgent GetAgent(string sanitizeFieldNames = null)
		{
			var logger = new TestLogger();
			var configSnapshot = sanitizeFieldNames == null ? new MockConfigSnapshot(logger, captureBody: "all") : new MockConfigSnapshot(logger, captureBody: "all", sanitizeFieldNames: sanitizeFieldNames);
			return new ApmAgent(new TestAgentComponents(logger, configSnapshot, new SerializerMockPayloadSender(configSnapshot), new CurrentExecutionSegmentsContainer(logger)));
		}

		/// <summary>
		/// Sends an HTTP GET with the given headers and makes sure all of them are sanitized.
		/// This test applies custom values to the SanitizeFieldNames setting
		/// </summary>
		[InlineData("*mySecurityHeader", new[] { "abcmySecurityHeader", "amySecurityHeader", "mySecurityHeader" })]
		[InlineData("mySecurityHeader*", new[] { "mySecurityHeaderAbc", "mySecurityHeaderAbc1", "mySecurityHeader" })]
		[InlineData("*mySecurityHeader*", new[] { "AbcmySecurityHeaderAbc", "amySecurityHeaderA", "mySecurityHeader" })]
		[InlineData("mysecurityheader", new[] { "mySECURITYHeader" })]
		[Theory]
		public async Task CustomSanitizeFieldNameSettingWithHeaders(string sanitizeFieldNames, string[] headerNames)
		{
			using (var agent = GetAgent(sanitizeFieldNames))
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				foreach (var header in headerNames) client.DefaultRequestHeaders.Add(header, "123");

				using (await client.GetAsync("/Home/SimplePage"))
				{
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();

					foreach (var header in headerNames) capturedPayload.FirstTransaction.Context.Request.Headers[header].Should().Be("[REDACTED]");
				}
			}
		}

		/// <summary>
		/// Sends an HTTP GET with the given headers and makes sure all of them are sanitized.
		/// This test applies custom values to the SanitizeFieldNames setting. It also turns on case sensitivity.
		/// </summary>
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
		public async Task CustomSanitizeFieldNameSettingWithCaseSensitivityWithHeaders(string sanitizeFieldNames, string headerName, bool shouldBeSanitized)
		{
			using (var agent = GetAgent(sanitizeFieldNames))
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				const string headerValue = "123";
				client.DefaultRequestHeaders.Add(headerName, headerValue);

				using (await client.GetAsync("/Home/SimplePage"))
				{
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be(shouldBeSanitized ? "[REDACTED]" : headerValue);
				}
			}
		}

		/// <summary>
		/// Tests the default SanitizeFieldNames - It sends an HTTP GET with the given headers and makes sure all
		/// of them are sanitized.
		/// </summary>
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
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				client.DefaultRequestHeaders.Add(headerName, "123");
				using (await client.GetAsync("/Home/SimplePage"))
				{
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers[headerName].Should().Be("[REDACTED]");
				}
			}
		}

		/// <summary>
		/// ASP.NET Core seems to rewrite the name of these headers (so <code>authorization</code> becomes <code>Authorization</code>).
		/// Our "by default case insensitivity" still works, the only difference is that if we send a header with name
		/// <code>authorization</code> it'll be captured as <code>Authorization</code> (capital letter).
		///
		/// Otherwise same as <see cref="DefaultsWithHeaders"/>.
		/// </summary>
		/// <param name="headerName">The original header name sent in the HTTP GET</param>
		/// <param name="returnedHeaderName">The header name (with capital letter) seen on the request in ASP.NET Core</param>
		[InlineData("authorization", "Authorization")]
		[InlineData("set-cookie", "Set-Cookie")]
		[Theory]
		public async Task DefaultsWithKnownHeaders(string headerName, string returnedHeaderName)
		{
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				client.DefaultRequestHeaders.Add(headerName, "123");
				using (await client.GetAsync("/Home/SimplePage"))
				{
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers.Should().NotBeNull();
					capturedPayload.FirstTransaction.Context.Request.Headers[returnedHeaderName].Should().Be("[REDACTED]");
				}
			}
		}

		/// <summary>
		/// Sends an HTTP post with content type application/x-www-form-urlencoded
		/// Makes sure that fields in the body are sanitized accordingly
		/// </summary>
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
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				var nvc = new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Input1", "test1"),
					new KeyValuePair<string, string>(formName, "test2")
				};

				var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };

				using (var res = await client.SendAsync(req))
				{
					res.IsSuccessStatusCode.Should().BeTrue();
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Errors.Should().BeNullOrEmpty();
					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Request.Body.Should().Be($"Input1=test1&{formName}=[REDACTED]");
				}
			}
		}

		[InlineData("password", true)]
		[InlineData("pwd", true)]
		[InlineData("Input", false)]
		[Theory]
		public async Task DefaultWithRequestBodySingleValueNoError(string formName, bool shouldBeSanitized)
		{
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				var nvc = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(formName, "test") };

				var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
				using (var res = await client.SendAsync(req))
				{
					res.IsSuccessStatusCode.Should().BeTrue();
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Errors.Should().BeNullOrEmpty();
					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Request.Body.Should().Be(shouldBeSanitized ? $"{formName}=[REDACTED]" : $"{formName}=test");
				}
			}
		}

		/// <summary>
		/// Same as <see cref="DefaultWithRequestBodyNoError"/> except this time the request ends up with an error
		/// </summary>
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
			using (var agent = GetAgent())
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				var nvc = new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Input1", "test1"),
					new KeyValuePair<string, string>(formName, "test2")
				};

				var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/PostError") { Content = new FormUrlEncodedContent(nvc) };

				try
				{
					using (var res = await client.SendAsync(req))
					{
						res.IsSuccessStatusCode.Should().BeFalse();
						var capturedPayload = (MockPayloadSender)agent.PayloadSender;

						capturedPayload.Transactions.Should().ContainSingle();
						capturedPayload.FirstError.Should().NotBeNull();
						capturedPayload.FirstTransaction.Context.Request.Body.Should().Be($"Input1=test1&{formName}=[REDACTED]");
					}
				}
				catch
				{
					// exception is fine, it doesn't really matter what happens with the call, important is the captured body, which we assert on later.
				}
			}
		}

		/// <summary>
		/// Applies custom values to sanitizeFieldNames and makes sure that the request body is sanitized accordingly.
		/// </summary>
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
			using (var agent = GetAgent(sanitizeFieldNames))
			using (var client = TestHelper.GetClient(_factory, agent))
			{
				var nvc = new List<KeyValuePair<string, string>>
				{
					new KeyValuePair<string, string>("Input1", "test1"),
					new KeyValuePair<string, string>(formName, "test2")
				};

				var req = new HttpRequestMessage(HttpMethod.Post, "api/Home/Post") { Content = new FormUrlEncodedContent(nvc) };
				using (var res = await client.SendAsync(req))
				{
					res.IsSuccessStatusCode.Should().BeTrue();
					var capturedPayload = (MockPayloadSender)agent.PayloadSender;

					capturedPayload.Errors.Should().BeNullOrEmpty();
					capturedPayload.Transactions.Should().ContainSingle();
					capturedPayload.FirstTransaction.Context.Request.Body.Should().Be(shouldBeSanitized ? $"Input1=test1&{formName}=[REDACTED]" : $"Input1=test1&{formName}=test2");
				}
			}
		}
	}
}
