using System;
using System.Diagnostics;
using System.Threading;

namespace Elastic.Apm.Helpers
{
	/// <summary>
	/// Credit:
	/// https://stackoverflow.com/questions/6661055/using-interlocked-compareexchange-operation-on-a-bool-value/18027246
	/// </summary>
	internal class AgentSpinLock
	{
		private const int FalseValueAsInt = 0;
		private const int TrueValueAsInt = 1;

		private volatile int _isAcquiredBoolValueAsInt = FalseValueAsInt;

		internal bool IsAcquired => IntToBool(_isAcquiredBoolValueAsInt);

		/// <summary>
		/// Attempts to acquire this lock
		/// </summary>
		/// <returns>
		/// true - if the attempt was successful
		/// false - otherwise
		/// </returns>
		internal bool TryAcquire()
		{
			var originalValueAsInt =
				Interlocked.CompareExchange(ref _isAcquiredBoolValueAsInt, /* to: */ TrueValueAsInt, /* from: */ FalseValueAsInt);
			return originalValueAsInt == FalseValueAsInt;
		}

		internal Acquisition TryAcquireWithDisposable() => new Acquisition(this, TryAcquire());

		/// <summary>
		/// Releases this lock
		/// </summary>
		/// <exception cref="System.InvalidOperationException">If this lock is not currently in acquired state</exception>
		internal void Release()
		{
			if (!IsAcquired) throw new InvalidOperationException("Attempt to release lock that is not acquired");

			_isAcquiredBoolValueAsInt = FalseValueAsInt;
		}

		private static bool IntToBool(int i) => i == TrueValueAsInt;

		[DebuggerDisplay(nameof(IsAcquired) + " = {" + nameof(IsAcquired) + "}")]
		internal readonly struct Acquisition : IDisposable
		{
			private readonly AgentSpinLock _agentSpinLock;

			internal Acquisition(AgentSpinLock agentSpinLock, bool isAcquired)
			{
				_agentSpinLock = agentSpinLock;
				IsAcquired = isAcquired;
			}

			internal bool IsAcquired { get; }

			public void Dispose()
			{
				if (IsAcquired) _agentSpinLock.Release();
			}
		}
	}
}
