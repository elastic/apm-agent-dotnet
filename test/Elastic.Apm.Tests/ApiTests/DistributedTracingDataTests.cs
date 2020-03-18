using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

using static Elastic.Apm.Tests.HelpersTests.DistributedTracingDataHelper;

namespace Elastic.Apm.Tests.ApiTests
{
	/// <summary>
	/// Tests the DistributedTracingData parameter on the Public Agent API
	/// </summary>
	public class DistributedTracingDataTests
	{
		private const string TestTransaction = "TestTransaction";
		private const string UnitTest = "UnitTest";

		/// <summary>
		/// Passes a valid trace context to <see cref="Tracer.StartTransaction" />.
		/// Makes sure the agent continued the trace.
		/// </summary>
		[Fact]
		public void ValidDistributedTracingDataTest()
		{
			var payloadSender1 = new MockPayloadSender();
			var payloadSender2 = new MockPayloadSender();

			string traceId, parentId;
			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender1)))
			{
				var transaction = agent.Tracer.StartTransaction(TestTransaction, UnitTest);
				Thread.Sleep(50);
				traceId = transaction.TraceId;
				parentId = transaction.Id;
				transaction.End();
			}

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender2)))
			{
				var transaction =
					agent.Tracer.StartTransaction(TestTransaction, UnitTest, BuildDistributedTracingData(traceId, parentId, ValidTraceFlags));
				Thread.Sleep(50);
				transaction.End();
			}

			payloadSender2.FirstTransaction.TraceId.Should().Be(payloadSender1.FirstTransaction.TraceId);
			payloadSender1.FirstTransaction.ParentId.Should().BeNullOrWhiteSpace();
			payloadSender2.FirstTransaction.ParentId.Should().Be(payloadSender1.FirstTransaction.Id);
		}

		/// <summary>
		/// Passes different invalid (TraceId, ParentId) combination to <see cref="Tracer.StartTransaction" />
		/// Makes sure a new trace was created, so the created transaction has a new TraceId and no ParentId
		/// </summary>
		/// <param name="traceId"></param>
		/// <param name="parentId"></param>
		/// <param name="traceFlags"></param>
		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public void InvalidDistributedTracingDataTest(string traceId, string parentId, string traceFlags)
		{
			var payloadSender1 = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender1)))
			{
				var transaction =
					agent.Tracer.StartTransaction(TestTransaction, UnitTest,  BuildDistributedTracingData(traceId, parentId, traceFlags));
				transaction.End();
			}

			payloadSender1.FirstTransaction.TraceId.Should().NotBe(traceId);
			payloadSender1.FirstTransaction.ParentId.Should().BeNullOrWhiteSpace();
		}

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Action, DistributedTracingData)" /> method with a
		/// valid
		/// DistributedTracingData parameter
		/// </summary>
		[Fact]
		public void DistributedTracingDataWithSimpleAction_Valid() =>
			AssertValidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction,
				UnitTest, () => { WaitHelpers.SleepMinimum(); },  BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public void DistributedTracingDataWithSimpleAction_Invalid(string traceId, string parentId, string traceFlags) =>
			AssertInvalidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction,
				UnitTest, () => { WaitHelpers.SleepMinimum(); },  BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);


		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction(string,string,Action{ITransaction}, DistributedTracingData)" /> method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public void DistributedTracingDataWitSimpleActionWithParameter_Valid() =>
			AssertValidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction,
				UnitTest, t => { WaitHelpers.SleepMinimum(); },  BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public void DistributedTracingDataWitSimpleActionWithParameter_Invalid(string traceId, string parentId, string traceFlags) =>
			AssertInvalidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction,
				UnitTest, t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
				}, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction{T}(string,string,Func{ITransaction,T}, DistributedTracingData)" />
		/// method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnTypeAndParameter_Valid() =>
			AssertValidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
					return 42;
				}, BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public void SimpleActionWithReturnTypeAndParameter_Invalid(string traceId, string parentId, string traceFlags) =>
			AssertInvalidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				t =>
				{
					t.Should().NotBeNull();
					WaitHelpers.SleepMinimum();
					return 42;
				}, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,Func{ITransaction,T}, DistributedTracingData)" /> method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public void SimpleActionWithReturnType_Valid() =>
			AssertValidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction, UnitTest, () =>
			{
				WaitHelpers.SleepMinimum();
				return 42;
			}, BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public void SimpleActionWithReturnType_Invalid(string traceId, string parentId, string traceFlags) =>
			AssertInvalidDistributedTracingData(agent => agent.Tracer.CaptureTransaction(TestTransaction, UnitTest, () =>
			{
				WaitHelpers.SleepMinimum();
				return 42;
			}, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction(string,string,Func{Task}, DistributedTracingData)" /> method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public async Task AsyncTask_Valid() =>
			await AssertValidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async () => { await WaitHelpers.DelayMinimum(); }, BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public async Task AsyncTask_Invalid(string traceId, string parentId, string traceFlags) =>
			await AssertInvalidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async () => { await WaitHelpers.DelayMinimum(); }, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction(string,string,Func{ITransaction,Task}, DistributedTracingData)" />
		/// method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithParameter_Valid() =>
			await AssertValidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
				}, BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public async Task AsyncTaskWithParameter_Invalid(string traceId, string parentId, string traceFlags) =>
			await AssertInvalidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
				}, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		/// <summary>
		/// Tests the
		/// <see cref="Tracer.CaptureTransaction{T}(string,string,Func{ITransaction,Task{T}}, DistributedTracingData)" />
		/// method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnTypeAndParameter_Valid() =>
			await AssertValidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
					return 42;
				}, BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public async Task AsyncTaskWithReturnTypeAndParameter_Invalid(string traceId, string parentId, string traceFlags) =>
			await AssertInvalidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async t =>
				{
					t.Should().NotBeNull();
					await WaitHelpers.DelayMinimum();
					return 42;
				}, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		/// <summary>
		/// Tests the <see cref="Tracer.CaptureTransaction{T}(string,string,Func{Task{T}}, DistributedTracingData)" />
		/// method
		/// with valid DistributedTracingData parameter.
		/// </summary>
		[Fact]
		public async Task AsyncTaskWithReturnType_Valid() =>
			await AssertValidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async () =>
				{
					await WaitHelpers.DelayMinimum();
					return 42;
				}, BuildDistributedTracingData(ValidTraceId, ValidParentId, ValidTraceFlags)));

		[Theory]
		[ClassData(typeof(InvalidDistributedTracingDataData))]
		public async Task AsyncTaskWithReturnType_Invalid(string traceId, string parentId, string traceFlags) =>
			await AssertInvalidDistributedTracingData(async agent => await agent.Tracer.CaptureTransaction(TestTransaction, UnitTest,
				async () =>
				{
					await WaitHelpers.DelayMinimum();
					return 42;
				}, BuildDistributedTracingData(traceId, parentId, traceFlags)), traceId);

		private static void AssertValidDistributedTracingData(Action<IApmAgent> transactionCreator)
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) transactionCreator(agent);

			payloadSender.FirstTransaction.TraceId.Should().Be(ValidTraceId);
			payloadSender.FirstTransaction.ParentId.Should().Be(ValidParentId);
		}

		private static void AssertInvalidDistributedTracingData(Action<IApmAgent> transactionCreator, string traceId)
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) transactionCreator(agent);

			payloadSender.FirstTransaction.TraceId.Should().NotBe(traceId);
			payloadSender.FirstTransaction.ParentId.Should().BeNullOrWhiteSpace();
		}

		private static async Task AssertValidDistributedTracingData(Func<IApmAgent, Task> transactionCreator)
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) await transactionCreator(agent);

			payloadSender.FirstTransaction.TraceId.Should().Be(ValidTraceId);
			payloadSender.FirstTransaction.ParentId.Should().Be(ValidParentId);
		}

		private static async Task AssertInvalidDistributedTracingData(Func<IApmAgent, Task> transactionCreator, string traceId)
		{
			var payloadSender = new MockPayloadSender();

			using (var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender))) await transactionCreator(agent);

			payloadSender.FirstTransaction.TraceId.Should().NotBe(traceId);
			payloadSender.FirstTransaction.ParentId.Should().BeNullOrWhiteSpace();
		}

		private class InvalidDistributedTracingDataData : IEnumerable<object[]>
		{
			public IEnumerator<object[]> GetEnumerator()
			{
				yield return new object[] { "", "", "" };
				yield return new object[] { null, null, null };
				yield return new object[] { "aaa", "bbb", "ccc" };
				yield return new object[] { "null", "5ec5de4fdae36f4c", "01" };
				yield return new object[] { "005a66g3c2fb9591a0e53d322df6c3e2", "null", "01" };
				yield return new object[] { "00000000000000000000000000000000", "0000000000000000", "00" };
				yield return new object[] { "005a66g3c2fb9591a0e53d322df6c3e2", "5ec5de4fdae36f4c", "01" }; //1 non-hex in TraceId
				yield return new object[] { "005a66a3c2fb9591a0e53d322df6c3e2", "5ec5de4fdaei6f4c", "01" }; //1 non-hex in ParentId
				yield return new object[] { "005a6663c2fb9591a0e53d322d6c3e2", "5ec5de4fdae36f4c", "01" }; //Trace Id 1 shorter than expected
				yield return new object[] { "005a6663c2fb9591a0e53d322df6c3e2", "5ec5defdae36f4c", "01" }; //Parent Id 1 shorter than expected
				yield return new object[] { "005a6663c2fb95291a0e53d322df6c3e2", "5ec5de4fdae36f4c", "01" }; //Trace Id 1 longer than expected
				yield return new object[] { "005a6663c2fb9591a0e53d322df6c3e2", "56ec5de4fdae36f4c", "01" }; //ParentId Id 1 longer than expected
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}
