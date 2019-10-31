using System;

namespace Elastic.Apm.Helpers
{
	/// <summary>
	/// Assertion.IfEnabled?.That(...) is a replacement for System.Diagnostics.Contracts.Contract.Assert for agent's internal
	/// use -
	/// for us to catch bugs in agent's code. It might be problematic to use Contract.Assert because application might have it
	/// disabled or
	/// configured to do some strange things (pop GUI dialog boxes, etc.) so we need a separate runtime assertion facility
	/// that we can control independently of whatever monitored application uses.
	/// </summary>
	internal static class Assertion
	{
		private const AssertionLevel DefaultLevel = AssertionLevel.O_1;
		private static readonly AssertIfEnabled SingletonAssertIfEnabled = new AssertIfEnabled();
		private static readonly Impl ImplSingleton = new Impl(DefaultLevel);


		internal static AssertIfEnabled? If_O_n_LevelEnabled => ImplSingleton.If_O_n_LevelEnabled;

		internal static AssertIfEnabled? IfEnabled => ImplSingleton.IfEnabled;

		internal static bool Is_O_n_LevelEnabled => ImplSingleton.Is_O_n_LevelEnabled;

		internal static bool IsEnabled => ImplSingleton.IsEnabled;

		internal static void DoIfEnabled(Action<AssertIfEnabled> doAction) => ImplSingleton.DoIfEnabled(doAction);

		internal static void DoIf_O_n_LevelEnabled(Action<AssertIfEnabled> doAction) => ImplSingleton.DoIf_O_n_LevelEnabled(doAction);

		internal class Impl
		{
			internal Impl(AssertionLevel initialLevel) => Level = initialLevel;

			// ReSharper disable once InconsistentNaming
			internal AssertIfEnabled? If_O_n_LevelEnabled => Is_O_n_LevelEnabled ? SingletonAssertIfEnabled : (AssertIfEnabled?)null;

			internal AssertIfEnabled? IfEnabled => IsEnabled ? SingletonAssertIfEnabled : (AssertIfEnabled?)null;

			// ReSharper disable once InconsistentNaming
			internal bool Is_O_n_LevelEnabled => Level >= AssertionLevel.O_n;

			internal bool IsEnabled => Level >= AssertionLevel.O_1;

			internal AssertionLevel Level { get; set; }

			internal void DoIfEnabled(Action<AssertIfEnabled> doAction)
			{
				if (IsEnabled) doAction(SingletonAssertIfEnabled);
			}

			internal void DoIf_O_n_LevelEnabled(Action<AssertIfEnabled> doAction)
			{
				if (Is_O_n_LevelEnabled) doAction(SingletonAssertIfEnabled);
			}
		}

		internal struct AssertIfEnabled
		{
			internal void That(bool condition, string message)
			{
				if (!condition) throw new AssertionFailedException(message);
			}
		}
	}
}
