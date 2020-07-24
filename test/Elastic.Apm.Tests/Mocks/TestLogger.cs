// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestLogger : ConsoleLogger
	{
		private readonly StringWriter _writer;

		public TestLogger() : this(LogLevel.Error, new StringWriter()) { }

		public TestLogger(LogLevel level) : this(level, new StringWriter()) { }

		public TestLogger(LogLevel level, StringWriter writer) : base(level, writer, writer) => _writer = writer;

		public IReadOnlyList<string> Lines =>
			_writer.GetStringBuilder().ToString().Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
	}
}
