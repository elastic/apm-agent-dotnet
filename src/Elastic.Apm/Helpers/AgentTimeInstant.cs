// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elastic.Apm.Helpers
{
	[DebuggerDisplay("{" + nameof(_elapsedSinceTimerStarted) + "}")]
	internal readonly struct AgentTimeInstant
	{
		private readonly IAgentTimer _sourceAgentTimer;
		private readonly TimeSpan _elapsedSinceTimerStarted;

		internal AgentTimeInstant(IAgentTimer sourceAgentTimer, TimeSpan elapsedSinceTimerStarted)
		{
			_sourceAgentTimer = sourceAgentTimer;
			_elapsedSinceTimerStarted = elapsedSinceTimerStarted;
		}

		public override bool Equals(object otherObj)
		{
			if (otherObj is AgentTimeInstant other) return Equals(other);

			return false;
		}

		public bool Equals(AgentTimeInstant other) =>
			_sourceAgentTimer == other._sourceAgentTimer && _elapsedSinceTimerStarted == other._elapsedSinceTimerStarted;

		public override int GetHashCode() => _elapsedSinceTimerStarted.GetHashCode();

		public static AgentTimeInstant operator +(AgentTimeInstant i, TimeSpan t) =>
			new AgentTimeInstant(i._sourceAgentTimer, i._elapsedSinceTimerStarted + t);

		public static AgentTimeInstant operator -(AgentTimeInstant i, TimeSpan t) =>
			new AgentTimeInstant(i._sourceAgentTimer, i._elapsedSinceTimerStarted - t);

		public static TimeSpan operator -(AgentTimeInstant i1, AgentTimeInstant i2)
		{
			VerifyInstantsAreCompatible(i1, i2);
			return i1._elapsedSinceTimerStarted - i2._elapsedSinceTimerStarted;
		}

		public static bool operator ==(AgentTimeInstant i1, AgentTimeInstant i2) => i1.Equals(i2);

		public static bool operator !=(AgentTimeInstant i1, AgentTimeInstant i2) => !i1.Equals(i2);

		public static bool operator <(AgentTimeInstant i1, AgentTimeInstant i2)
		{
			VerifyInstantsAreCompatible(i1, i2);
			return i1._elapsedSinceTimerStarted < i2._elapsedSinceTimerStarted;
		}

		public static bool operator <=(AgentTimeInstant i1, AgentTimeInstant i2)
		{
			VerifyInstantsAreCompatible(i1, i2);
			return i1._elapsedSinceTimerStarted <= i2._elapsedSinceTimerStarted;
		}

		public static bool operator >(AgentTimeInstant i1, AgentTimeInstant i2)
		{
			VerifyInstantsAreCompatible(i1, i2);
			return i1._elapsedSinceTimerStarted > i2._elapsedSinceTimerStarted;
		}

		public static bool operator >=(AgentTimeInstant i1, AgentTimeInstant i2)
		{
			VerifyInstantsAreCompatible(i1, i2);
			return i1._elapsedSinceTimerStarted >= i2._elapsedSinceTimerStarted;
		}

		private static void VerifyInstantsAreCompatible(AgentTimeInstant i1, AgentTimeInstant i2, [CallerMemberName] string caller = null)
		{
			if (i1.IsCompatibleWith(i2._sourceAgentTimer)) return;

			var opName = caller == null ? "an operation" : $"operation {caller}";
			throw new InvalidOperationException($"It's illegal to perform {opName} on two AgentTimeInstant instances " +
				"that did not originate from the same timer." +
				$" The first AgentTimeInstant: timer: {i1._sourceAgentTimer}, value: {i1._elapsedSinceTimerStarted}." +
				$" The second AgentTimeInstant: timer: {i2._sourceAgentTimer}, value: {i2._elapsedSinceTimerStarted}.");
		}

		internal bool IsCompatibleWith(IAgentTimer otherAgentTimer) => _sourceAgentTimer == otherAgentTimer;

		public override string ToString() => _elapsedSinceTimerStarted.ToString();

		public string ToStringDetailed() => new ToStringBuilder(nameof(AgentTimeInstant))
		{
			{ nameof(_sourceAgentTimer), _sourceAgentTimer }, { nameof(_elapsedSinceTimerStarted), _elapsedSinceTimerStarted }
		}.ToString();
	}
}
