// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Utilities
{
	public class ToAllSinksLogger : LineWriterToLoggerAdaptor
	{
		public ToAllSinksLogger(ITestOutputHelper xUnitOutputHelper, LogLevel level = LogLevel.Trace)
			: base(
				new SplittingLineWriter(
					new SystemDiagnosticsTraceLineWriter("<Elastic APM .NET Tests> "),
					new FlushingTextWriterToLineWriterAdaptor(Console.Out),
					new XunitOutputToLineWriterAdaptor(xUnitOutputHelper))
				, level)
		{ }
	}
}
