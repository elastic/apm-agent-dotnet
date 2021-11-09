using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using RichardSzalay.MockHttp;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.BackendComm.BackendCommUtils.ApmServerEndpoints;
using MockHttpMessageHandler = RichardSzalay.MockHttp.MockHttpMessageHandler;


// SpecFlow isn't designed for parallel test execution, so we just turn it off. More: https://docs.specflow.org/projects/specflow/en/latest/Execution/Parallel-Execution.html
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	public class OutcomeSteps
	{
		private readonly ScenarioContext _scenarioContext;

		public OutcomeSteps(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

		[Given(@"^an agent$")]
		public void GivenAnAgent()
		{
			var output = _scenarioContext.ScenarioContainer.Resolve<ITestOutputHelper>();
			var logger = new XUnitLogger(LogLevel.Trace, output);
			var configuration = new TestConfiguration();
			_scenarioContext.Set(configuration);

			var payloadCollector = new PayloadCollector();
			_scenarioContext.Set(payloadCollector);

			var handler = new MockHttpMessageHandler();
			handler.When(BuildIntakeV2EventsAbsoluteUrl(configuration.ServerUrl).AbsoluteUri)
				.Respond(r =>
				{
					payloadCollector.ProcessPayload(r);
					return new HttpResponseMessage(HttpStatusCode.OK);
				});

			var environmentVariables = new TestEnvironmentVariables();
			_scenarioContext.Set(environmentVariables);
			_scenarioContext.Set(() =>
			{
				var payloadSender = new PayloadSenderV2(
					logger,
					configuration,
					Service.GetDefaultService(configuration, new NoopLogger()),
					new Api.System(),
					MockApmServerInfo.Version710,
					handler,
					environmentVariables: environmentVariables);

				return new ApmAgent(new TestAgentComponents(logger, configuration, payloadSender));
			});
		}

		[Given(@"an active span")]
		public void GivenAnActiveSpan()
		{
			var agent = _scenarioContext.Get<ApmAgent>();
			var span = agent.Tracer.StartTransaction("SampleTransaction", "Test").StartSpan("SampleSpan", "Test");
			_scenarioContext.Set(span);
		}

		[Given(@"user sets span outcome to '(.*)'")]
		public void GivenUserSetsSpanOutcomeTo(Outcome outcome)
		{
			var span = _scenarioContext.Get<ISpan>();
			span.Outcome = outcome;
		}

		[Given(@"span terminates with outcome '(.*)'")]
		public void GivenSpanTerminatesWithOutcome(Outcome outcome)
		{
			// We don't do anything here. After this step we assert that Span.End() doesn't change the outcome set by the user
			var span = _scenarioContext.Get<ISpan>();
			span.End();
		}

		[Given(@"an active transaction")]
		public void GivenAnActiveTransaction()
		{
			var agent = _scenarioContext.Get<ApmAgent>();
			var transaction = agent.Tracer.StartTransaction("SampleTransaction", "Test");
			_scenarioContext.Set(transaction);
		}

		[Given(@"user sets transaction outcome to '(.*)'")]
		public void GivenUserSetsTransactionOutcomeTo(Outcome outcome)
		{
			var transaction = _scenarioContext.Get<ITransaction>();
			transaction.Outcome = outcome;
		}

		[Given(@"transaction terminates with outcome '(.*)'")]
		public void GivenTransactionTerminatesWithOutcome(Outcome outcome)
		{
			// We don't do anything here. After this step we assert that Transaction.End() doesn't change the outcome set by the user
			var transaction = _scenarioContext.Get<ITransaction>();
			transaction.End();
		}

		[Given(@"span terminates with an error")]
		public void GivenSpanTerminatesWithAnError()
		{
			var span = _scenarioContext.Get<ISpan>();
			span.CaptureError("foo", "bar", new StackTrace().GetFrames());
			span.End();
		}

		[Given(@"span terminates without error")]
		public void GivenSpanTerminatesWithoutError()
		{
			var span = _scenarioContext.Get<ISpan>();
			span.End();
		}

		[Given(@"transaction terminates with an error")]
		public void GivenTransactionTerminatesWithAnError()
		{
			var transaction = _scenarioContext.Get<ITransaction>();
			transaction.CaptureError("foo", "bar", new StackTrace().GetFrames());
			transaction.End();
		}

		[Given(@"transaction terminates without error")]
		public void GivenTransactionTerminatesWithoutError()
		{
			var transaction = _scenarioContext.Get<ITransaction>();
			transaction.End();
		}

		[Given(@"an HTTP transaction with (.*) response code")]
		public void GivenAnHTTPTransactionWithResponseCode(int responseCode)
		{
			var agent = _scenarioContext.Get<ApmAgent>();
			var transaction = agent.Tracer.StartTransaction("HttpTransaction", "Test");
			transaction.Context.Request = new Request("GET", new Url { Full = "https://elastic.co" });
			transaction.Context.Response = new Response() { StatusCode = responseCode };
			WebRequestTransactionCreator.SetOutcomeForHttpResult(transaction, responseCode);
			_scenarioContext.Set(transaction);
		}

		[Given(@"an HTTP span with (.*) response code")]
		public void GivenAnHTTPSpanWithResponseCode(int responseCode)
		{
			var agent = _scenarioContext.Get<ApmAgent>();
			var span = agent.Tracer.StartTransaction("Foo", "Test").StartSpan("HttpsSpan", "Test");
			span.Context.Http = new Http() { StatusCode = responseCode };
			HttpDiagnosticListenerImplBase<object, object>.SetOutcome(span, responseCode);
			_scenarioContext.Set(span);
		}

		[Given(@"a gRPC transaction with '(.*)' status")]
		public void GivenAGRPCTransactionWithStatus(string responseCode)
		{
			var agent = _scenarioContext.Get<ApmAgent>();
			var transaction = agent.Tracer.StartTransaction("GRPCTransaction", "Test");
			transaction.Outcome = GrpcHelper.GrpcServerReturnCodeToOutcome(responseCode);
			_scenarioContext.Set(transaction);
		}

		[Given(@"a gRPC span with '(.*)' status")]
		public void GivenAGRPCSpanWithStatus(string responseCode)
		{
			var agent = _scenarioContext.Get<ApmAgent>();
			var span = agent.Tracer.StartTransaction("GRPCTransaction", "Test").StartSpan("GRPCSpan", "Test");
			span.Outcome = GrpcHelper.GrpcClientReturnCodeToOutcome(responseCode);
			_scenarioContext.Set(span);
		}

		[Then(@"span outcome is '(.*)'")]
		public void ThenSpanOutcomeIs(Outcome outcome)
		{
			var span = _scenarioContext.Get<ISpan>();
			span.Outcome.Should().Be(outcome);
		}

		[Then(@"transaction outcome is '(.*)'")]
		public void ThenTransactionOutcomeIs(Outcome outcome)
		{
			var transaction = _scenarioContext.Get<ITransaction>();
			transaction.Outcome.Should().Be(outcome);
		}

		[Then(@"transaction outcome is ""(.*)""")]
		public void ThenTransactionOutcomeIs2(Outcome outcome) => ThenTransactionOutcomeIs(outcome);


		[Then(@"span outcome is ""(.*)""")]
		public void ThenSpanOutcomeIs2(Outcome outcome) => ThenSpanOutcomeIs(outcome);
	}
}
