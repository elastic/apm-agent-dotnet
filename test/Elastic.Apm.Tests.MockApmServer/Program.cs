// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;

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
