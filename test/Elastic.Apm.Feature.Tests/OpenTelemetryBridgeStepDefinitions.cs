using System;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Elastic.Apm.Feature.Tests
{
	[Binding]
	public class OpenTelemetryBridgeStepDefinitions
	{
		private readonly ScenarioContext _scenarioContext;

		public OpenTelemetryBridgeStepDefinitions(ScenarioContext scenarioContext) => _scenarioContext = scenarioContext;

		[Given(@"an agent")]
		[Scope(Feature = "OpenTelemetry bridge")]
		public void GivenAnAgent()
		{
			var mockPaylodSender = new MockPayloadSender();
			_scenarioContext.Add("payloadSender", mockPaylodSender);
			using (var agent = new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(enableOpenTelemetryBridge: "true"),
				apmServerInfo: MockApmServerInfo.Version716, payloadSender: mockPaylodSender)))
			{
				_scenarioContext.Add("agent", agent);
			}
		}

		[Given(@"OTel span is created with remote context as parent")]
		public void GivenOTelSpanIsCreatedWithRemoteContextAsParent()
		{
			var traceId = ActivityTraceId.CreateRandom();
			var parentSpanId = ActivitySpanId.CreateRandom();
			var src = new ActivitySource("Test");

			_scenarioContext.Add("traceId", traceId);
			_scenarioContext.Add("parentSpanId", parentSpanId);

			src.StartActivity("foo", ActivityKind.Internal, new ActivityContext(traceId, parentSpanId, ActivityTraceFlags.Recorded)).Stop();
		}


		[Then(@"Elastic bridged object is a transaction")]
		public void ThenElasticBridgedObjectIsATransaction()
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			payloadSender.FirstTransaction.Should().NotBeNull();
			payloadSender.Spans.Should().BeNullOrEmpty();
		}

		[Then(@"Elastic bridged transaction has remote context as parent")]
		public void ThenElasticBridgedTransactionHasRemoteContextAsParent()
		{
			var parentSpanId = _scenarioContext.Get<ActivitySpanId>("parentSpanId");
			var traceId = _scenarioContext.Get<ActivityTraceId>("traceId");
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			payloadSender.FirstTransaction.Should().NotBeNull();

			payloadSender.FirstTransaction.Should().NotBeNull();
			payloadSender.FirstTransaction.ParentId.Should().Be(parentSpanId.ToString());
			payloadSender.FirstTransaction.TraceId.Should().Be(traceId.ToString());
		}

		[Given(@"OTel span is created without parent")]
		public void GivenOTelSpanIsCreatedWithoutParent()
		{
			var src = new ActivitySource("Test");
			src.StartActivity("foo");
		}

		[Given(@"OTel span ends")]
		public void GivenOTelSpanEnds() => Activity.Current.Stop();

		[Then(@"Elastic bridged transaction is a root transaction")]
		public void ThenElasticBridgedTransactionIsARootTransaction()
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			payloadSender.FirstTransaction.Should().NotBeNull();
			payloadSender.FirstTransaction.ParentId.Should().BeNullOrEmpty();
		}

		[Then(@"Elastic bridged transaction outcome is ""([^""]*)""")]
		public void ThenElasticBridgedTransactionOutcomeIs(string outcome)
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			payloadSender.FirstTransaction.Outcome.ToString().ToLower().Should().Be(outcome);
		}

		[Given(@"OTel span is created with local context as parent")]
		public void GivenOTelSpanIsCreatedWithLocalContextAsParent()
		{
			var agent = _scenarioContext.Get<ApmAgent>("agent");
			var transaction = agent.Tracer.StartTransaction("foo", "bar");
			_scenarioContext.Add("elasticTransaction", transaction);

			var src = new ActivitySource("Test");
			src.StartActivity("foo");
		}

		[Then(@"Elastic bridged object is a span")]
		public void ThenElasticBridgedObjectIsASpan()
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			payloadSender.FirstSpan.Should().NotBeNull();
		}

		[Then(@"Elastic bridged span has local context as parent")]
		public void ThenElasticBridgedSpanHasLocalContextAsParent()
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			var transaction = _scenarioContext.Get<ITransaction>("elasticTransaction");
			payloadSender.FirstSpan.ParentId.Should().Be(transaction.Id);
		}

		[Then(@"Elastic bridged span outcome is ""([^""]*)""")]
		public void ThenElasticBridgedSpanOutcomeIs(string outcome)
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			payloadSender.FirstSpan.Outcome.ToString().ToLower().Should().Be(outcome);
		}


		[Given(@"an active transaction")]
		[Scope(Feature = "OpenTelemetry bridge")]
		public void GivenAnActiveTransaction()
		{
			var agent = _scenarioContext.Get<ApmAgent>("agent");
			var transaction = agent.Tracer.StartTransaction("foo", "bar");
			_scenarioContext.Add("elasticTransaction", transaction);
		}

		[Given(@"OTel span is created with kind ""([^""]*)""")]
		public void OTelSpanIsCreatedWithKind(string kind)
		{
			var activityKind = Enum.Parse<ActivityKind>(kind, true);
			var src = new ActivitySource("Test");
			src.StartActivity("foo", activityKind);
		}

		[Then(@"Elastic bridged span OTel kind is ""([^""]*)""")]
		public void ThenElasticBridgedSpanOTelKindIs(string kind)
		{
	

			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			(payloadSender.FirstSpan as Span).Otel.SpanKind.ToLower().Should().Be(kind.ToLower());
		}

		[Then(@"Elastic bridged span type is ""([^""]*)""")]
		public void ThenElasticBridgedSpanTypeIs(string type)
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			(payloadSender.FirstSpan as Span).Type.Should().Be(type);
		}

		[Then(@"Elastic bridged span subtype is ""([^""]*)""")]
		public void ThenElasticBridgedSpanSubtypeIs(string subtype)
		{
			var payloadSender = _scenarioContext.Get<MockPayloadSender>("payloadSender");
			if(string.IsNullOrEmpty(subtype))
				(payloadSender.FirstSpan as Span).Subtype.Should().BeNullOrEmpty();
			else
				(payloadSender.FirstSpan as Span).Subtype.Should().Be(subtype);
		}

		[Then(@"Elastic bridged transaction OTel kind is ""([^""]*)""")]
		public void ThenElasticBridgedTransactionOTelKindIs(string iNTERNAL)
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged transaction type is '([^']*)'")]
		public void ThenElasticBridgedTransactionTypeIs(string unknown)
		{
			throw new PendingStepException();
		}

		//[Given(@"OTel span is created with kind '([^']*)'")]
		//public void GivenOTelSpanIsCreatedWithKind(string sERVER)
		//{
		//	throw new PendingStepException();
		//}

		[Given(@"OTel span status set to ""([^""]*)""")]
		public void GivenOTelSpanStatusSetTo(string unset)
		{
			throw new PendingStepException();
		}

		[Given(@"OTel span is created with kind '([^']*)'")]
		public void GivenOTelSpanIsCreatedWithKind(string iNTERNAL)
		{
			throw new PendingStepException();
		}

		[Given(@"OTel span has following attributes")]
		public void GivenOTelSpanHasFollowingAttributes(Table table)
		{
			throw new PendingStepException();
		}

		//[Then(@"Elastic bridged transaction type is ""([^""]*)""")]
		//public void ThenElasticBridgedTransactionTypeIs(string request)
		//{
		//	throw new PendingStepException();
		//}

		//[Given(@"OTel span is created with kind '([^']*)'")]
		//public void GivenOTelSpanIsCreatedWithKind(string cLIENT)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span type is '([^']*)'")]
		//public void ThenElasticBridgedSpanTypeIs(string external)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span subtype is '([^']*)'")]
		//public void ThenElasticBridgedSpanSubtypeIs(string http)
		//{
		//	throw new PendingStepException();
		//}

		[Then(@"Elastic bridged span OTel attributes are copied as-is")]
		public void ThenElasticBridgedSpanOTelAttributesAreCopiedAs_Is()
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged span destination resource is set to ""([^""]*)""")]
		public void ThenElasticBridgedSpanDestinationResourceIsSetTo(string p0)
		{
			throw new PendingStepException();
		}

		//[Then(@"Elastic bridged span type is '([^']*)'")]
		//public void ThenElasticBridgedSpanTypeIs(string db)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span subtype is ""([^""]*)""")]
		//public void ThenElasticBridgedSpanSubtypeIs(string mysql)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span destination resource is set to ""([^""]*)""")]
		//public void ThenElasticBridgedSpanDestinationResourceIsSetTo(string mysql)
		//{
		//	throw new PendingStepException();
		//}

		//[Given(@"OTel span is created with kind '([^']*)'")]
		//public void GivenOTelSpanIsCreatedWithKind(string cONSUMER)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged transaction type is '([^']*)'")]
		//public void ThenElasticBridgedTransactionTypeIs(string messaging)
		//{
		//	throw new PendingStepException();
		//}

		//[Given(@"OTel span is created with kind '([^']*)'")]
		//public void GivenOTelSpanIsCreatedWithKind(string pRODUCER)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span type is '([^']*)'")]
		//public void ThenElasticBridgedSpanTypeIs(string messaging)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span subtype is ""([^""]*)""")]
		//public void ThenElasticBridgedSpanSubtypeIs(string rabbitmq)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span subtype is ""([^""]*)""")]
		//public void ThenElasticBridgedSpanSubtypeIs(string grpc)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged span destination resource is set to ""([^""]*)""")]
		//public void ThenElasticBridgedSpanDestinationResourceIsSetTo(string grpc)
		//{
		//	throw new PendingStepException();
		//}

		//[Then(@"Elastic bridged transaction type is '([^']*)'")]
		//public void ThenElasticBridgedTransactionTypeIs(string request)
		//{
		//	throw new PendingStepException();
		//}
	}
}
