// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Utilities;

public class UnitTestLogger : IApmLogger, ILogLevelSwitchable
{
	private readonly ITestOutputHelper _output;

	public UnitTestLogger(ITestOutputHelper output, LogLevel level = LogLevel.Information)
	{
		LogLevelSwitch = new LogLevelSwitch(level);
		_output = output;
	}

	public LogLevelSwitch LogLevelSwitch { get; }

	public bool IsEnabled(LogLevel level) => LogLevelSwitch.Level <= level;

	public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
	{
		var dateTime = DateTime.Now;

		var message = formatter(state, e);

		var fullMessage = $"[{dateTime:yyyy-MM-dd HH:mm:ss.fff zzz}][{ConsoleLogger.LevelToString(level)}] - {message}";
		if (e != null)
			fullMessage += $"{Environment.NewLine}+-> Exception: {e.GetType().FullName}: {e.Message}{Environment.NewLine}{e.StackTrace}";

		if (IsEnabled(level))
			_output.WriteLine(fullMessage);
	}
}
