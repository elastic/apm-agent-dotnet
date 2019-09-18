using System;
using System.Diagnostics;
using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests.HelpersTests
{
	internal static class AgentSpinLockTestsExtensions
	{
		internal static AgentSpinLockTests.AcquisitionForTest TryAcquireWithDisposable(this AgentSpinLockTests.ISpinLockForTest spinLockForTest) =>
			new AgentSpinLockTests.AcquisitionForTest(spinLockForTest, spinLockForTest.TryAcquire());
	}

	public class AgentSpinLockTests
	{
		private readonly IApmLogger _logger;

		public AgentSpinLockTests(ITestOutputHelper testOutputHelper) => _logger = new XunitOutputLogger(testOutputHelper);

		internal interface ISpinLockForTest
		{
			void Release();

			bool TryAcquire();
		}

		public static TheoryData AllSpinLockImpls =>
			new TheoryData<ISpinLockForTest>
			{
				new AgentSpinLockForTest(),
				new ThreadUnsafeSpinLockForTest(false),
				new ThreadUnsafeSpinLockForTest(true),
				new NoopSpinLockForTest()
			};

		public static TheoryData ThreadSafeSpinLockImpls => new TheoryData<ISpinLockForTest> { new AgentSpinLockForTest() };

		[Fact]
		public void default_value_is_false()
		{
			var sl = new AgentSpinLock();
			sl.IsAcquired.Should().BeFalse();
		}

		[Fact]
		public void TryAcquire_tests()
		{
			var sl = new AgentSpinLock();

			sl.IsAcquired.Should().BeFalse();
			sl.TryAcquire().Should().BeTrue();
			sl.IsAcquired.Should().BeTrue();
			sl.TryAcquire().Should().BeFalse();
			sl.IsAcquired.Should().BeTrue();
			sl.Release();
			sl.IsAcquired.Should().BeFalse();
			sl.TryAcquire().Should().BeTrue();
			sl.IsAcquired.Should().BeTrue();
		}

		[Fact]
		public void TryAcquireWithDisposable_tests()
		{
			var sl = new AgentSpinLock();
			sl.IsAcquired.Should().BeFalse();
			using (var acq = sl.TryAcquireWithDisposable())
			{
				acq.IsAcquired.Should().BeTrue();
				sl.IsAcquired.Should().BeTrue();
				sl.TryAcquire().Should().BeFalse();
				using (var acq2 = sl.TryAcquireWithDisposable())
				{
					acq2.IsAcquired.Should().BeFalse();
					acq.IsAcquired.Should().BeTrue();
					sl.IsAcquired.Should().BeTrue();
					sl.TryAcquire().Should().BeFalse();
				}
			}
			sl.IsAcquired.Should().BeFalse();
		}

		[Fact]
		public void Release_throws_if_not_acquired()
		{
			var sl = new AgentSpinLock();

			sl.IsAcquired.Should().BeFalse();
			AsAction(() => sl.Release())
				.Should()
				.ThrowExactly<InvalidOperationException>()
				.WithMessage("*release*not*acquired");
		}

		[Theory]
		[MemberData(nameof(ThreadSafeSpinLockImpls))]
//		[MemberData(nameof(AllSpinLockImpls))]
		internal void multiple_threads(ISpinLockForTest spinLock)
		{
			var mutexProtectedVar = 0;
			const int expectedNumberOfIncrementsPerThread = 1000;
			var numberOfThreads = MultiThreadsTestUtils.NumberOfThreadsForTest;
			_logger.Debug()?.Log($"numberOfThreads: {numberOfThreads}");

			var threadResults = MultiThreadsTestUtils.TestOnThreads(numberOfThreads, threadIndex =>
			{
				var numberOfIncrements = 0;

				while (true)
				{
					using (var acq = spinLock.TryAcquireWithDisposable())
					{
						if (!acq.IsAcquired) continue;

						++mutexProtectedVar;
					}

					++numberOfIncrements;
					if (numberOfIncrements == expectedNumberOfIncrementsPerThread) return numberOfIncrements;
				}
			});

			threadResults.ForEach(numberOfIncrements => numberOfIncrements.Should().Be(expectedNumberOfIncrementsPerThread));
			mutexProtectedVar.Should().Be(numberOfThreads * expectedNumberOfIncrementsPerThread);
		}

		[DebuggerDisplay(nameof(IsAcquired) + " = {" + nameof(IsAcquired) + "}")]
		internal readonly struct AcquisitionForTest : IDisposable
		{
			private readonly ISpinLockForTest _spinLockForTest;

			internal AcquisitionForTest(ISpinLockForTest spinLockForTest, bool isAcquired)
			{
				_spinLockForTest = spinLockForTest;
				IsAcquired = isAcquired;
			}

			internal bool IsAcquired { get; }

			public void Dispose()
			{
				if (IsAcquired) _spinLockForTest.Release();
			}
		}

		internal class AgentSpinLockForTest : ISpinLockForTest
		{
			private readonly AgentSpinLock _agentSpinLock = new AgentSpinLock();

			public bool TryAcquire() => _agentSpinLock.TryAcquire();

			public void Release() => _agentSpinLock.Release();
		}

		internal class ThreadUnsafeSpinLockForTest : ISpinLockForTest
		{
			private readonly bool _releaseOnNotAcquiredThrows;
			private bool _isLockHeld;

			internal ThreadUnsafeSpinLockForTest(bool releaseOnNotAcquiredThrows) => _releaseOnNotAcquiredThrows = releaseOnNotAcquiredThrows;

			public bool TryAcquire()
			{
				if (_isLockHeld) return false;

				// Force the worst case scenario giving other thread more chance to change _isLockHeld to true
				// while the current thread will still think that _isLockHeld is false after resuming from Thread.Yield()
				Thread.Yield();

				_isLockHeld = true;
				return true;
			}

			public void Release()
			{
				if (_releaseOnNotAcquiredThrows && !_isLockHeld) throw new InvalidOperationException("Attempt to release lock that is not acquired");

				_isLockHeld = false;
			}
		}

		internal class NoopSpinLockForTest : ISpinLockForTest
		{
			public bool TryAcquire() => true;

			public void Release() { }
		}
	}
}
