// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="AspNetCoreDiagnosticListener" /> and
	/// <see cref="AspNetCoreErrorDiagnosticListener" /> type.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class AspNetCoreDiagnosticListenerTest : IClassFixture<WebApplicationFactory<Startup>>
	{
		private readonly WebApplicationFactory<Startup> _factory;

		public AspNetCoreDiagnosticListenerTest(WebApplicationFactory<Startup> factory) => _factory = factory;

		/// <summary>
		/// Triggers /Home/TriggerError from the sample app
		/// and makes sure that the error is captured.
		/// </summary>
		/// <returns>The error in ASP net core.</returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task TestErrorInAspNetCore(bool useOnlyDiagnosticSource)
		{
			var capturedPayload = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: capturedPayload, configuration: new MockConfiguration(exitSpanMinDuration:"0"))))
			{
				var client = Helper.GetClient(agent, _factory, useOnlyDiagnosticSource);

				try
				{
					await client.GetAsync("/Home/TriggerError");
				}
				catch
				{
					// ignore
				}

				capturedPayload?.WaitForTransactions();
				capturedPayload?.Transactions.Should().ContainSingle();

				capturedPayload?.WaitForErrors();
				capturedPayload?.Errors.Should().NotBeEmpty();

				var error = capturedPayload?.Errors.FirstOrDefault(e => e.Exception.Message == "This is a test exception!") as Error;
				error.Should().NotBeNull();

				error?.Exception.Message.Should().Be("This is a test exception!");
				error?.Exception.Type.Should().Be(typeof(Exception).FullName);
				error?.Exception.Handled.Should().BeFalse();

				var context = error?.Context;
				context?.Request.Url.Full.Should().Be("http://localhost/Home/TriggerError");
				context?.Request.Method.Should().Be(HttpMethod.Get.Method);
			}
		}

		/// <summary>
		/// Triggers a post method which raises an exception (/api/Home/PostError)
		/// and makes sure that the error is captured and that the json body is successfully
		/// retrieved from the HttpRequest
		/// </summary>
		/// <returns>The error in ASP net core.</returns>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task TestJsonBodyRetrievalOnRequestFailureInAspNetCore(bool useOnlyDiagnosticSource)
		{
			var capturedPayload = new MockPayloadSender();
			using (var agent = new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(
				captureBody: ConfigConsts.SupportedValues.CaptureBodyErrors,
				// ReSharper disable once RedundantArgumentDefaultValue
				captureBodyContentTypes: ConfigConsts.DefaultValues.CaptureBodyContentTypes),
				payloadSender: capturedPayload)))
			{
				var client = Helper.GetClient(agent, _factory, useOnlyDiagnosticSource);

				var body = "{\"id\" : \"1\"}";
				await client.PostAsync("api/Home/PostError", new StringContent(body, Encoding.UTF8, "application/json"));

				capturedPayload.Should().NotBeNull();

				capturedPayload.WaitForTransactions();
				capturedPayload.Transactions.Should().ContainSingle();

				capturedPayload.WaitForErrors();
				capturedPayload.Errors.Should().ContainSingle();

				var errorException = capturedPayload.FirstError.Exception;
				errorException?.Message.Should().Be("This is a post method test exception!");
				errorException?.Type.Should().Be(typeof(Exception).FullName);

				var context = capturedPayload.FirstError.Context;
				context?.Request.Url.Full.Should().Be("http://localhost/api/Home/PostError");
				context?.Request.Method.Should().Be(HttpMethod.Post.Method);
				context?.Request.Body.Should().Be(body);
				errorException?.Should().NotBeNull();
				errorException?.Handled.Should().BeFalse();
			}
		}

		[Fact]
		public async Task CaptureDiagnosticAndHostingUnhandledExceptionEvent()
		{
			// Test that this exception does not reappear.
			// System.InvalidCastException: 'Unable to cast object of type
			// '<>f__AnonymousType1`3[ Microsoft.AspNetCore.Http.HttpContext, System.Int64, System.Exception ]' to type
			// '<>f__AnonymousType0`2[ Microsoft.AspNetCore.Http.HttpContext, System.Exception ]'.'

			var capturedPayload = new MockPayloadSender();
			using ( var agent = new ApmAgent(new TestAgentComponents(payloadSender: capturedPayload, configuration: new MockConfiguration(exitSpanMinDuration: "0"))) )
			{
				// Based on Helper GetClient
				var builder = _factory
					.WithWebHostBuilder(n =>
					{
						n.Configure(app =>
						{
							var subs = new IDiagnosticsSubscriber[]
							{
								new AspNetCoreDiagnosticSubscriber()
							};
							agent.Subscribe(subs);

							app.UseWhen(
								ctx => !ctx.Request.Query.ContainsKey("skipUseDeveloperExceptionPage"),
								app =>
								{
									app.UseDeveloperExceptionPage();
								}
							);

							app.UseHttpsRedirection();
							app.UseStaticFiles();
							app.UseCookiePolicy();

							Startup.ConfigureRoutingAndMvc(app);
						});

						n.ConfigureServices(Helper.ConfigureServices);
					});

				var client  = builder.CreateClient();

				// Hits case Microsoft.AspNetCore.Diagnostics.UnhandledException
				// Exception handled by UseDeveloperExceptionPage
				await client.GetAsync("/Home/TriggerError");

				// So far same as TestErrorInAspNetCore
				capturedPayload.WaitForTransactions();
				capturedPayload.Transactions.Should().ContainSingle();

				capturedPayload.Clear();

				// Hits case Microsoft.AspNetCore.Hosting.UnhandledException
				Func<Task> act = async () => await client.GetAsync("/Home/TriggerError?skipUseDeveloperExceptionPage=true");
				( await act.Should().ThrowAsync<Exception>() ).WithMessage("This is a test exception!");

				capturedPayload?.WaitForTransactions();
				capturedPayload?.Transactions.Should().ContainSingle();

				capturedPayload?.WaitForErrors();
				capturedPayload?.Errors.Should().NotBeEmpty();

				var error = capturedPayload?.Errors.FirstOrDefault(e => e.Exception.Message == "This is a test exception!") as Error;
				error.Should().NotBeNull();

				error?.Exception.Message.Should().Be("This is a test exception!");
				error?.Exception.Type.Should().Be(typeof(Exception).FullName);
				error?.Exception.Handled.Should().BeFalse();

				var context = error?.Context;
				context?.Request.Url.Full.Should().Be("http://localhost/Home/TriggerError?skipUseDeveloperExceptionPage=true");
				context?.Request.Method.Should().Be(HttpMethod.Get.Method);
			}
		}
	}
}
