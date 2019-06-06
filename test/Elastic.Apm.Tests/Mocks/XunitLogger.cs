using System;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.Mocks
{
	public class XUnitLogger : IApmLogger
	{
		private readonly ITestOutputHelper _testOutputHelper;
		public LogLevel Level  => LogLevel.Trace;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var dateTime = DateTime.UtcNow;

			var message = formatter(state, e);

			var fullMessage = e == null ? $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{(level.ToString())}] - {message}"
				: $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{(level.ToString())}] - {message}{Environment.NewLine}Exception: {e.GetType().FullName}, Message: {e.Message}";

			_testOutputHelper.WriteLine(fullMessage);
		}


		public XUnitLogger(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}


	}
}
