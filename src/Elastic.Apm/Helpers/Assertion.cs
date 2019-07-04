namespace Elastic.Apm.Helpers
{
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
