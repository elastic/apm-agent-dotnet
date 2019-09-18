using System.Diagnostics;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class SystemDiagnosticsTraceLineWriter : ILineWriter
	{
		private readonly string _prefix;

		public SystemDiagnosticsTraceLineWriter(string prefix = "") => _prefix = prefix;

		public void WriteLine(string line)
		{
			Trace.WriteLine(TextUtils.PrefixEveryLine(line, _prefix));
			Trace.Flush();
		}
	}
}
