// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true)]

namespace Elastic.Apm.AspNetCore.Tests;

public static class XunitLoggerFactoryExtensions
{
	public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper output)
	{
		builder.Services.AddSingleton<ILoggerProvider>(new XunitLoggerProvider(output));
		return builder;
	}
}

public class XunitLoggerProvider : ILoggerProvider
{
	private readonly ITestOutputHelper _testOutputHelper;

	public XunitLoggerProvider(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

	public ILogger CreateLogger(string categoryName)
		=> new XunitLogger(_testOutputHelper, categoryName);

	public void Dispose()
	{ }
}

public class XunitLogger : ILogger
{
	private readonly ITestOutputHelper _testOutputHelper;
	private readonly string _categoryName;

	public XunitLogger(ITestOutputHelper testOutputHelper, string categoryName)
	{
		_testOutputHelper = testOutputHelper;
		_categoryName = categoryName;
	}

	public IDisposable BeginScope<TState>(TState state)
		=> NoopDisposable.Instance;

	public bool IsEnabled(LogLevel logLevel)
		=> true;

	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
	{
		_testOutputHelper.WriteLine($"{_categoryName} [{eventId}] {formatter(state, exception)}");
		if (exception != null)
			_testOutputHelper.WriteLine(exception.ToString());
	}

	private class NoopDisposable : IDisposable
	{
		public static readonly NoopDisposable Instance = new NoopDisposable();
		public void Dispose()
		{ }
	}
}
