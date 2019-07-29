using System;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.TestHelpers;

namespace Elastic.Apm.Tests.MockApmServer
{
	/// <summary>
	/// User can run mock APM server as a standalone application via command line,
	/// in which case it will listen on the default port <seealso cref="ConfigConsts.DefaultValues.ApmServerPort" />.
	/// </summary>
	// ReSharper disable once ClassNeverInstantiated.Global
	public class Program
	{
		public static void Main(string[] args)
		{
			var mockApmServer = new MockApmServer(new FlushingTextWriterToLoggerAdaptor(Console.Out, LogLevel.Trace), nameof(Main));
			mockApmServer.Run(ConfigConsts.DefaultValues.ApmServerPort);
		}
	}
}
