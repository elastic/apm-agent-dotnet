// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Helpers
{
	internal class DisposableHelper
	{
		internal const string AnotherCallStillInProgressMsg = "{0} Dispose call while a previous call is still in progress"
			+ " which most likely means a bug in the code - just exiting";

		private volatile int _state = (int)State.BeforeDispose;

		internal bool HasStarted => _state != (int)State.BeforeDispose;

		internal bool DoOnce(IApmLogger loggerArg, string dbgOwnerDesc, Action ownerDispose)
		{
			var logger = loggerArg.Scoped(nameof(DisposableHelper));

			var stateBeforeTry = TryChangeStateAtomically(State.BeforeDispose, State.DuringDispose);
			switch (stateBeforeTry)
			{
				case State.BeforeDispose: break;

				case State.DuringDispose:
					logger.Critical()?.Log(string.Format(AnotherCallStillInProgressMsg, "{DisposableDesc}"), dbgOwnerDesc);
					return false;

				case State.AfterDispose:
					logger.Debug()?.Log("{DisposableDesc} is already disposed - just exiting", dbgOwnerDesc);
					return false;

				default:
					throw new AssertionFailedException(
						$"Unexpected {nameof(stateBeforeTry)} value: {stateBeforeTry} ({(int)stateBeforeTry} as int)");
			}

			logger.Debug()?.Log("Starting to dispose {DisposableDesc}...", dbgOwnerDesc);
			ownerDispose();
			logger.Debug()?.Log("Finished disposing {DisposableDesc}", dbgOwnerDesc);

			stateBeforeTry = TryChangeStateAtomically(State.DuringDispose, State.AfterDispose);
			Assertion.IfEnabled?.That(stateBeforeTry == State.DuringDispose
				, $"Unexpected {nameof(stateBeforeTry)} value: {stateBeforeTry} ({(int)stateBeforeTry} as int)");

			return true;
		}

		private State TryChangeStateAtomically(State fromState, State toState)
		{
			var prevState = Interlocked.CompareExchange(ref _state, (int)toState, (int)fromState);
			return (State)prevState;
		}

		private enum State
		{
			BeforeDispose,
			DuringDispose,
			AfterDispose
		}
	}
}
