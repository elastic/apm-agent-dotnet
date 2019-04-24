using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Helpers
{
	internal class XunitOutputLogger : AsyncLineWriterLogger
	{
		private readonly ITestOutputHelper _xUnitOutputHelper;

		internal XunitOutputLogger(ITestOutputHelper xUnitOutputHelper, LogLevel level = LogLevel.Trace)
			: base(level, new XunitOutputToAsyncLineWriterAdapter(xUnitOutputHelper)) { }

		private class XunitOutputToAsyncLineWriterAdapter : IAsyncLineWriter
		{
			private readonly ITestOutputHelper _xUnitOutputHelper;

			internal XunitOutputToAsyncLineWriterAdapter(ITestOutputHelper xUnitOutputHelper) => _xUnitOutputHelper = xUnitOutputHelper;

			public Task WriteLineAsync(string line)
			{
				_xUnitOutputHelper.WriteLine(line);
				return Task.CompletedTask;
			}
		}
	}
}
