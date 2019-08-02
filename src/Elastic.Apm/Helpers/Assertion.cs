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
		private static readonly AssertIfEnabled SingletonAssertIfEnabled = new AssertIfEnabled();

		internal static AssertIfEnabled? IfEnabled => IsEnabled ? SingletonAssertIfEnabled : (AssertIfEnabled?)null;

		internal static bool IsEnabled { get; set; } = true;

		internal struct AssertIfEnabled
		{
			internal void That(bool condition, string message)
			{
				if (!condition) throw new AssertionFailedException(message);
			}
		}
	}
}
