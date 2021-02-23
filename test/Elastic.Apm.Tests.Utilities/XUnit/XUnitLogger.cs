// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Utilities.XUnit
{
	public class XUnitLogger : IApmLogger
	{
		private readonly LogLevel _level;
		private readonly ITestOutputHelper _output;
		private readonly string _scope;

		public XUnitLogger(LogLevel level, ITestOutputHelper output, string scope = null)
		{
			_level = level;
			_output = output;
			_scope = scope;
		}

		public bool IsEnabled(LogLevel level) => level >= _level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(level)) return;

			var message = formatter(state, e);
			if (_scope is null)
				_output.WriteLine(message);
			else
				_output.WriteLine(_scope + ": " + message);
		}
	}
}
