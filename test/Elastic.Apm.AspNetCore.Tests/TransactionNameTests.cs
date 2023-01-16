// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SampleAspNetCoreApp;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the transaction name in ASP.NET Core.
	/// Specifically: optional parameters in the route should be templated in the Transaction.Name.
	/// E.g. url localhost/user/info/1 should get have Transaction.Name GET user/info {id}
	/// </summary>
	[Collection("DiagnosticListenerTest")]
	public class TransactionNameTests : IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly MockPayloadSender _payloadSender = new MockPayloadSender();

		public TransactionNameTests(WebApplicationFactory<Startup> factory, ITestOutputHelper testOutputHelper)
		{
			_factory = factory;

			var logger = new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(testOutputHelper))
			{
				LogLevelSwitch = { Level = LogLevel.Trace }
			};

			_agent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender,
				logger: logger,
				// _agent needs to share CurrentExecutionSegmentsContainer with Agent.Instance
				// because the sample application used by the tests (SampleAspNetCoreApp) uses Agent.Instance.Tracer.CurrentTransaction/CurrentSpan
				currentExecutionSegmentsContainer: Agent.Instance.TracerInternal.CurrentExecutionSegmentsContainer));
			HostBuilderExtensions.UpdateServiceInformation(_agent.Service);
		}

		/// <summary>
		/// Calls a URL that maps to a route with optional parameter (id).
		/// Makes sure the Transaction.Name contains "{id}" instead of the value.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task OptionalRouteParameter(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("home/sample/3");
			await httpClient.GetAsync("home/sample/2");

			_payloadSender.Transactions.Should().OnlyContain(n => n.Name.Equals("GET home/sample {id}", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Calls a URL and sets custom transaction name.
		/// Makes sure the Transaction.Name can be set to custom name.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task CustomTransactionName(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("home/TransactionWithCustomName");

			_payloadSender.WaitForTransactions();
			_payloadSender.Transactions.Should().OnlyContain(n => n.Name == "custom");
		}

		/// <summary>
		/// Calls a URL that sets custom transaction name by using $"{HttpContext.Request.Method} {HttpContext.Request.Path}"
		/// Makes sure the Transaction.Name is $"{HttpContext.Request.Method} {HttpContext.Request.Path}" and the agent does not
		/// change that.
		/// See: https://github.com/elastic/apm-agent-dotnet/pull/258#discussion_r291025014
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task CustomTransactionNameWithNameUsingRequestInfo(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			if (httpClient != null)
			{
				await httpClient.GetAsync("home/TransactionWithCustomNameUsingRequestInfo");

				_payloadSender.WaitForTransactions();
				_payloadSender?.Transactions?.Should()?.OnlyContain(n => n.Name == "GET /home/TransactionWithCustomNameUsingRequestInfo");
			}
		}

		/// <summary>
		/// Calls a URL that would map to a route with optional parameter (id), but does not pass the id value.
		/// Makes sure no template value is captured in Transaction.Name.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task OptionalRouteParameterWithNull(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("home/sample");

			_payloadSender.WaitForTransactions();
			_payloadSender.Transactions.Should().OnlyContain(n => n.Name.Equals("GET home/sample", StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Tests a URL that maps to a route with default values. Calls "/", which maps to "home/index".
		/// Makes sure "Home/Index" is in the Transaction.Name.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task DefaultRouteParameterValues(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("/");

			_payloadSender.WaitForTransactions();
			_payloadSender.FirstTransaction.Name.Should().Be("GET Home/Index");
			_payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("http://localhost/");
		}

		/// <summary>
		/// Tests a URL that maps to an area route with an area route value and default values. Calls "/MyArea", which maps to "MyArea/Home/Index".
		/// Makes sure "GET MyArea/Home/Index" is the Transaction.Name.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task Name_Should_Be_Area_Controller_Action_When_Mvc_Area_Controller_Action(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("/MyArea");

			_payloadSender.WaitForTransactions();
			_payloadSender.FirstTransaction.Name.Should().Be("GET MyArea/Home/Index");
			_payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("http://localhost/MyArea");
		}

		/// <summary>
		/// Tests a URL that maps to an explicit area route with default values. Calls "/MyOtherArea", which maps to "MyOtherArea/Home/Index".
		/// Makes sure "GET MyOtherArea/Home/Index" is the Transaction.Name.
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task Name_Should_Be_Area_Controller_Action_When_Mvc_Area_Controller_Action_With_Area_Route(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("/MyOtherArea");

			_payloadSender.WaitForTransactions();
			_payloadSender.FirstTransaction.Name.Should().Be("GET MyOtherArea/Home/Index");
			_payloadSender.FirstTransaction.Context.Request.Url.Full.Should().Be("http://localhost/MyOtherArea");
		}

		/// <summary>
		/// Calls a URL that maps to no route and causes a 404
		/// </summary>
		[InlineData("home/doesnotexist", true)]
		[InlineData("home/doesnotexist", false)]
		[InlineData("files/doesnotexist/somefile", true)]
		[InlineData("files/doesnotexist/somefile", false)]
		[Theory]
		public async Task NotFoundRoute_ShouldBe_Aggregatable(string url, bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync(url);

			_payloadSender.WaitForTransactions();
			_payloadSender.Transactions.Should().OnlyContain(n => n.Name.Equals("GET unknown route", StringComparison.OrdinalIgnoreCase));
			_payloadSender.Transactions.Should().HaveCount(1);
		}

		/// <summary>
		/// See https://github.com/elastic/apm-agent-dotnet/issues/1533
		/// If a URL matches a route, but the controller method returns e.g. HTTP 404,
		/// the method name should be the real route and not "unknown route".
		/// </summary>
		[Theory]
		[MemberData(nameof(MemberData.TestWithDiagnosticSourceOnly), MemberType = typeof(MemberData))]
		public async Task Http404WithValidRoute(bool diagnosticSourceOnly)
		{
			var httpClient = Helper.GetClient(_agent, _factory, diagnosticSourceOnly);
			await httpClient.GetAsync("api/Home/ReturnNotFound/42");

			_payloadSender.WaitForTransactions();
			_payloadSender.Transactions.Should().HaveCount(1);
			_payloadSender.FirstTransaction.Name.Should().Be("GET Home/ReturnNotFound {id}");

			_payloadSender.FirstTransaction.Context.Response.StatusCode.Should().Be(404);
		}

		public void Dispose()
		{
			_agent?.Dispose();
			_factory?.Dispose();
		}
	}
}
