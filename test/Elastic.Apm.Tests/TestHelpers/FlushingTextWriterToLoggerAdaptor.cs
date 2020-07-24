// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class FlushingTextWriterToLoggerAdaptor : LineWriterToLoggerAdaptor
	{
		public FlushingTextWriterToLoggerAdaptor(TextWriter textWriter, LogLevel level = LogLevel.Information)
			: base(new FlushingTextWriterToLineWriterAdaptor(textWriter), level) { }
	}
}
