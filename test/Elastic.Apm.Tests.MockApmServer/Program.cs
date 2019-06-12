using System;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.TestHelpers;

namespace Elastic.Apm.Tests.MockApmServer
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class Program
	{
		public static void Main(string[] args) =>
			new MockApmServer(new FlushingTextWriterToLoggerAdaptor(Console.Out, LogLevel.Trace), nameof(Main)).Run();
	}
}
