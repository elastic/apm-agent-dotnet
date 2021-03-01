using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using TechTalk.SpecFlow;
using Xunit;


// SpecFlow isn't designed for parallel test execution, so we just turn it off. More: https://docs.specflow.org/projects/specflow/en/latest/Execution/Parallel-Execution.html
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	[Scope(Feature = "outcome")]
	public class OutcomeSteps
	{
		private ApmAgent _agent;
		private ISpan _lastSpan;
		private ITransaction _lastTransaction;

		[Given(@"^an agent$")]
		public void GetAgent() => _agent = new ApmAgent(new TestAgentComponents());

		[Given(@"an active span")]
		public void GivenAnActiveSpan() => _agent.Tracer.StartTransaction("SampleTransaction", "Test").StartSpan("SampleSpan", "Test");

		[Given(@"user sets span outcome to '(.*)'")]
		public void GivenUserSetsSpanOutcomeTo(string spanOutcome)
		{
			var outcome = ToEnum<Outcome>(spanOutcome);
			_agent.Tracer.CurrentSpan.Outcome = outcome;
		}

		[Given(@"span terminates with outcome '(.*)'")]
		public void GivenSpanTerminatesWithOutcome(string spanOutcome)
		{
			// We don't do anything here. After this step we assert that Span.End() doesn't change the outcome set by the user
			_lastSpan = _agent.Tracer.CurrentSpan;
			_agent.Tracer.CurrentSpan.End();
		}

		[Given(@"an active transaction")]
		public void GivenAnActiveTransaction() => _agent.Tracer.StartTransaction("SampleTransaction", "Test");

		[Given(@"user sets transaction outcome to '(.*)'")]
		public void GivenUserSetsTransactionOutcomeTo(string transactionOutcome)
		{
			var outcome = ToEnum<Outcome>(transactionOutcome);
			_agent.Tracer.CurrentTransaction.Outcome = outcome;
		}

		[Given(@"transaction terminates with outcome '(.*)'")]
		public void GivenTransactionTerminatesWithOutcome(string p0)
		{
			// We don't do anything here. After this step we assert that Transaction.End() doesn't change the outcome set by the user
			_lastTransaction = _agent.Tracer.CurrentTransaction;
			_agent.Tracer.CurrentTransaction.End();
		}

		[Given(@"span terminates with an error")]
		public void GivenSpanTerminatesWithAnError()
		{
			_agent.Tracer.CurrentSpan.CaptureError("foo", "bar", new StackTrace().GetFrames());
			_lastSpan = _agent.Tracer.CurrentSpan;
			_agent.Tracer.CurrentSpan.End();
		}

		[Given(@"span terminates without error")]
		public void GivenSpanTerminatesWithoutError()
		{
			_lastSpan = _agent.Tracer.CurrentSpan;
			_agent.Tracer.CurrentSpan.End();
		}

		[Given(@"transaction terminates with an error")]
		public void GivenTransactionTerminatesWithAnError()
		{
			_agent.Tracer.CurrentTransaction.CaptureError("foo", "bar", new StackTrace().GetFrames());
			_lastTransaction = _agent.Tracer.CurrentTransaction;
			_agent.Tracer.CurrentTransaction.End();
		}

		[Given(@"transaction terminates without error")]
		public void GivenTransactionTerminatesWithoutError()
		{
			_lastTransaction = _agent.Tracer.CurrentTransaction;
			_agent.Tracer.CurrentTransaction.End();
		}

		[Given(@"an HTTP transaction with (.*) response code")]
		public void GivenAnHTTPTransactionWithResponseCode(int responseCode)
		{
			var transaction = _agent.Tracer.StartTransaction("HttpTransaction", "Test");
			transaction.Context.Request = new Request("GET", new Url { Full = "https://elastic.co" });
			transaction.Context.Response = new Response() { StatusCode = responseCode };
			WebRequestTransactionCreator.SetOutcomeForHttpResult(transaction, responseCode);
			_lastTransaction = transaction;
		}

		[Given(@"an HTTP span with (.*) response code")]
		public void GivenAnHTTPSpanWithResponseCode(int responseCode)
		{
			var span = _agent.Tracer.StartTransaction("Foo", "Test").StartSpan("HttpsSpan", "Test");
			span.Context.Http = new Http() { StatusCode = responseCode };
			HttpDiagnosticListenerImplBase<object, object>.SetOutcome(span, responseCode);
			_lastSpan = span;
		}

		[Given(@"a gRPC transaction with '(.*)' status")]
		public void GivenAGRPCTransactionWithStatus(string responseCode)
		{
			var transaction = _agent.Tracer.StartTransaction("GRPCTransaction", "Test");
			transaction.Outcome = GrpcHelper.GrpcServerReturnCodeToOutcome(responseCode);
			_lastTransaction = transaction;
		}

		[Given(@"a gRPC span with '(.*)' status")]
		public void GivenAGRPCSpanWithStatus(string responseCode)
		{
			var span = _agent.Tracer.StartTransaction("GRPCTransaction", "Test").StartSpan("GRPCSpan", "Test");
			span.Outcome = GrpcHelper.GrpcClientReturnCodeToOutcome(responseCode);
			_lastSpan = span;
		}

		[Then(@"span outcome is '(.*)'")]
		public void ThenSpanOutcomeIs(string spanOutcome)
		{
			var outcome = ToEnum<Outcome>(spanOutcome);
			_lastSpan.Outcome.Should().Be(outcome);

		}

		[Then(@"transaction outcome is '(.*)'")]
		public void ThenTransactionOutcomeIs(string transactionOutcome)
		{
			var outcome = ToEnum<Outcome>(transactionOutcome);
			_lastTransaction.Outcome.Should().Be(outcome);
		}

		[Then(@"transaction outcome is ""(.*)""")]
		public void ThenTransactionOutcomeIs2(string outcome) => ThenTransactionOutcomeIs(outcome);


		[Then(@"span outcome is ""(.*)""")]
		public void ThenSpanOutcomeIs2(string outcome) => ThenSpanOutcomeIs(outcome);

		private static T ToEnum<T>(string str)
		{
			var enumType = typeof(T);
			foreach (var name in Enum.GetNames(enumType))
			{
				var enumMemberAttribute = ((EnumMemberAttribute[])enumType.GetField(name).GetCustomAttributes(typeof(EnumMemberAttribute), true)).Single();
				if (enumMemberAttribute.Value == str)
					return (T)Enum.Parse(enumType, name);
			}
			//throw exception or whatever handling you want or
			return default(T);
		}
	}

}
