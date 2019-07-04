namespace Elastic.Apm.Helpers
{
	internal class Assertion
	{
		private static readonly AssertIfEnabled _singletonAssertIfEnabled = new AssertIfEnabled();

		internal static AssertIfEnabled? IfEnabled => IsEnabled ? _singletonAssertIfEnabled : (AssertIfEnabled?)null;

		internal struct AssertIfEnabled
		{
			internal void That(bool condition, string message)
			{
				if (!condition) throw new AssertionFailedException(message);
			}
		}

		internal static bool IsEnabled { get; set; } = true;
	}
}
