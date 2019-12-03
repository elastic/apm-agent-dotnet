using Elastic.Apm.Api;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	/// <summary>
	/// These tests access the static <see cref="Agent"/> instance.
	/// All other tests should have their own <see cref="ApmAgent"/> instance and not rely on anything static.
	/// Tests accessing the static <see cref="Agent"/> instance cannot run in parallel with tests that also access the static instance.
	/// </summary>
	[TestCaseOrderer("Elastic.Apm.Tests.TestHelpers.PriorityOrderer", "Elastic.Apm.Tests")]
	public class StaticAgentTests
	{
		/// <summary>
		/// Makes sure Agent.IsConfigured only returns true after Setup is called.
		/// </summary>
		//[Fact]
		[Fact, TestPriority(0)]
		public void IsConfigured()
		{
			Agent.IsConfigured.Should().BeFalse();

			Agent.Setup(new AgentComponents());
			Agent.IsConfigured.Should().BeTrue();
		}

		/// <summary>
		/// Registers multiple transaction observers and counts the call that their receive.
		/// </summary>
		[Fact, TestPriority(1)]
		public void TransactionObserverTest()
		{
			Agent.Setup(new TestAgentComponents());
			var transactionObserver = new FakeObserver();

			//register 1 observer
			var isRegistered = Agent.RegisterTransactionObserver(transactionObserver);
			isRegistered.Should().BeTrue();

			Agent.Tracer.CaptureTransaction("Test", "Test", () =>
				transactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(1));

			transactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(2);

			//register another observer
			var secondTransactionObserver = new FakeObserver();
			var isSecondRegistered = Agent.RegisterTransactionObserver(secondTransactionObserver);
			isSecondRegistered.Should().BeTrue();

			Agent.Tracer.CaptureTransaction("Test", "Test", () =>
			{
				transactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(3);
				secondTransactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(1);
			});

			transactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(4);
			secondTransactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(2);

			//clear all observers - none of them will be triggered
			Agent.ClearTransactionObservers();

			Agent.Tracer.CaptureTransaction("Test", "Test", () =>
			{
				transactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(4);
				secondTransactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(2);
			});

			transactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(4);
			secondTransactionObserver.NumberOfActiveTransactionChangedCalled.Should().Be(2);
		}
	}

	internal class FakeObserver : ITransactionObserver
	{
		public int NumberOfActiveTransactionChangedCalled { get; private set; }

		public void ActiveTransactionChanged(ITransaction currentTransaction) => NumberOfActiveTransactionChangedCalled++;
	}
}
