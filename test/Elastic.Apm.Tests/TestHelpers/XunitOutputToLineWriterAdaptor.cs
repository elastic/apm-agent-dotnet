using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class XunitOutputToLineWriterAdaptor : ILineWriter
	{
		private readonly ITestOutputHelper _xUnitOutputHelper;

		public XunitOutputToLineWriterAdaptor(ITestOutputHelper xUnitOutputHelper) => _xUnitOutputHelper = xUnitOutputHelper;

		public void WriteLine(string line) => _xUnitOutputHelper.WriteLine(line);
	}
}
