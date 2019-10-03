using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal class NoopXunitOutputHelper : ITestOutputHelper
	{
		public void WriteLine(string line) { }

		public void WriteLine(string format, params object[] args) { }
	}
}
