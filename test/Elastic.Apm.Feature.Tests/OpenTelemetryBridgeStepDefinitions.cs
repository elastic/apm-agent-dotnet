using System;
using System.Diagnostics;
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
			using (var agent = new ApmAgent(new TestAgentComponents(configuration: new MockConfiguration(enableOpenTelemetryBridge: "true"),
				apmServerInfo: MockApmServerInfo.Version716)))
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

			src.StartActivity("foo", ActivityKind.Internal, new ActivityContext(traceId, parentSpanId, ActivityTraceFlags.Recorded));
		}


		[Then(@"Elastic bridged object is a transaction")]
		public void ThenElasticBridgedObjectIsATransaction()
		{
			var tracer = (_scenarioContext["agent"] as ApmAgent).Tracer;
			tracer.CurrentTransaction.Should().NotBeNull();
			tracer.CurrentSpan.Should().BeNull();
		}

		[Then(@"Elastic bridged transaction has remote context as parent")]
		public void ThenElasticBridgedTransactionHasRemoteContextAsParent()
		{
			var parentSpanId = _scenarioContext.Get<ActivitySpanId>("parentSpanId");
			var traceId = _scenarioContext.Get<ActivityTraceId>("traceId");
			var tracer = (_scenarioContext["agent"] as ApmAgent).Tracer;
			tracer.CurrentTransaction.Should().NotBeNull();
			tracer.CurrentTransaction.ParentId.Should().Be(parentSpanId.ToString());
			tracer.CurrentTransaction.TraceId.Should().Be(traceId.ToString());
		}

		[Given(@"OTel span is created without parent")]
		public void GivenOTelSpanIsCreatedWithoutParent()
		{
			throw new PendingStepException();
		}

		[Given(@"OTel span ends")]
		public void GivenOTelSpanEnds()
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged transaction is a root transaction")]
		public void ThenElasticBridgedTransactionIsARootTransaction()
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged transaction outcome is ""([^""]*)""")]
		public void ThenElasticBridgedTransactionOutcomeIs(string unknown)
		{
			throw new PendingStepException();
		}

		[Given(@"OTel span is created with local context as parent")]
		public void GivenOTelSpanIsCreatedWithLocalContextAsParent()
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged object is a span")]
		public void ThenElasticBridgedObjectIsASpan()
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged span has local context as parent")]
		public void ThenElasticBridgedSpanHasLocalContextAsParent()
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged span outcome is ""([^""]*)""")]
		public void ThenElasticBridgedSpanOutcomeIs(string unknown)
		{
			throw new PendingStepException();
		}

		[Given(@"an active transaction")]
		[Scope(Feature = "otel_bridge")]
		public void GivenAnActiveTransaction()
		{
			throw new PendingStepException();
		}

		//[Given(@"OTel span is created with kind ""([^""]*)""")]
		//public void GivenOTelSpanIsCreatedWithKind(string iNTERNAL)
		//{
		//	throw new PendingStepException();
		//}

		[Then(@"Elastic bridged span OTel kind is ""([^""]*)""")]
		public void ThenElasticBridgedSpanOTelKindIs(string iNTERNAL)
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged span type is ""([^""]*)""")]
		public void ThenElasticBridgedSpanTypeIs(string app)
		{
			throw new PendingStepException();
		}

		[Then(@"Elastic bridged span subtype is ""([^""]*)""")]
		public void ThenElasticBridgedSpanSubtypeIs(string @internal)
		{
			throw new PendingStepException();
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
