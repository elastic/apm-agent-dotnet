// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using SampleConsoleNetCoreApp;
using Test;

// This is an application used to test the Elastic.Apm.Extensions.Hosting and Elastic.Apm.Extensions.Logging
// packages. It is used by the Elastic.Apm.Extensions.Hosting.Tests project which starts this application
// with different configurations. This allows isolated testing of the Elastic.Apm.Extensions.Hosting package
// and avoids issues with the singleton ApmAgent affecting the outcome of other test cases.

// Args:
// [0] - Control IConfiguration enabling of IApmAgent - Valid values: true, false, unset
//       When unset, the configuration is not set and the agent defaults should apply.
// [1] - Control multiple registration of IApmAgent - Valid values: true, false
//       When true, the agent is registered twice so that we can validate that this does not
//       cause an exception or error logs. We expect the first registration to win.
// [2] - Control whether to use legacy IHostBuilder - Valid values: true, false
//       When true, the legacy extension method on the IHostBuilder is used. When false,
//       the new IServiceCollection registration is used.
// [3] - Control whether to test the logging by registering a mock payload sender - Valid values: true, false
//       When true, the application registers an IHostedService and MockPayloadSender to test that error logs
//       are captured.

bool? enabled;

if (args[0] == "unset")
{
	enabled = null;
}
else
{
	if (!bool.TryParse(args[0], out var e))
		throw new Exception("The first argument must be true, false or unset.");

	enabled = e;
}

if (!bool.TryParse(args[1], out var registerTwice))
	throw new Exception("The second argument must be true or false.");

if (!bool.TryParse(args[2], out var legacyIHostBuilder))
	throw new Exception("The third argument must be true or false.");

if (!bool.TryParse(args[3], out var loggingTestMode))
	throw new Exception("The forth argument must be true or false.");

if (enabled.HasValue)
{
	Console.WriteLine("Starting with enabled: " + enabled.Value);
}
else
{
	Console.WriteLine("Starting with enabled: unset");
}

Console.WriteLine($"Starting with registerTwice: {registerTwice}");
Console.WriteLine($"Starting with legacy IHostBuilder: {legacyIHostBuilder}");
Console.WriteLine($"Starting in logging test mode: {loggingTestMode}");

var fakeSubscriber = new FakeSubscriber();

if (fakeSubscriber.IsSubscribed)
	throw new Exception("Subscriber should not be subscribed yet.");

var enabledEnvironmentVariable = Environment.GetEnvironmentVariable("ELASTIC_APM_ENABLED");

var envEnabled = false;
if (enabledEnvironmentVariable is not null && bool.TryParse(args[0], out envEnabled))
{
	Console.WriteLine("Starting with ELASTIC_APM_ENABLED: " + envEnabled);
}
else
{
	Console.WriteLine("ELASTIC_APM_ENABLED not configured");
}

// Build the IHost, either via the legacy IHostBuilder or the newer HostApplicationBuilder (IHostApplicationBuilder)

var payloadSender = new MockPayloadSender();

IHost host;
if (legacyIHostBuilder)
{
#pragma warning disable CS0618 // Type or member is obsolete
	var builder = Host.CreateDefaultBuilder();

	if (enabled.HasValue)
		builder.ConfigureAppConfiguration((hostingContext, config) =>
			config.AddInMemoryCollection(new Dictionary<string, string?> { { "ElasticApm:Enabled", enabled.Value.ToString() } }));

	if (loggingTestMode)
	{
		// This must occur before UseElasticApm
		builder.ConfigureLogging((_, logging) =>
		{
			logging.ClearProviders();
			logging.AddSimpleConsole(o => o.IncludeScopes = true);
		});

		builder.ConfigureServices((_, services) =>
		{
			services.AddHostedService<HostedService>();
			services.AddSingleton<IPayloadSender>(payloadSender);
		});
	}

	builder.UseElasticApm(fakeSubscriber);

	if (registerTwice)
		builder.UseElasticApm();
#pragma warning restore CS0618 // Type or member is obsolete

	host = builder.Build();
}
else
{
	var builder = Host.CreateApplicationBuilder(args);

	if (enabled.HasValue)
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { { "ElasticApm:Enabled", enabled.Value.ToString() } });

	if (loggingTestMode)
	{
		// This must occur before AddElasticApm
		builder.Logging
			.ClearProviders()
			.AddSimpleConsole(o => o.IncludeScopes = true);
	}

	builder.Services.AddElasticApm(fakeSubscriber);

	if (registerTwice)
		builder.Services.AddElasticApm();

	if (loggingTestMode)
	{
		builder.Services
			.AddHostedService<HostedService>() // The IHostedService must be registered after AddElasticApm so its invoked after the agent is initialized
			.AddSingleton<IPayloadSender>(payloadSender);
	}

	host = builder.Build();
}

// Start the host which should trigger the creation of the IApmAgent via DI
await host.StartAsync();

// We expect the agent to be configured by this point
if (!Agent.IsConfigured)
	throw new Exception("Agent should be configured.");

// We expect an ITracer to be available via DI
host.Services.GetRequiredService<ITracer>();

if (loggingTestMode)
{
	payloadSender.WaitForErrors();

	if (payloadSender.Errors.Count != 3)
		throw new Exception($"Expected 3 errors to be captured but receieved {payloadSender.Errors.Count}.");

	if (payloadSender.FirstError.Log.Message != "This is a sample error log message, with a sample value: 42")
		throw new Exception($"Unexpected first message: {payloadSender.FirstError.Log.Message}.");

	if (payloadSender.FirstError.Log.ParamMessage != "This is a sample error log message, with a sample value: {intParam}")
		throw new Exception($"Unexpected first param message: {payloadSender.FirstError.Log.ParamMessage}.");

	// Test a log with exception
	var logger = (ILogger?)host.Services.GetService(typeof(ILogger<object>));
	const string errorLogWithException = "error log with exception";

	try
	{
		throw new Exception();
	}
	catch (Exception e)
	{
		logger!.LogError(e, errorLogWithException);
	}

	payloadSender.WaitForErrors();

	if (payloadSender.Errors.SingleOrDefault(n => n.Log.Message == errorLogWithException &&
		n.Log.StackTrace != null && n.Log.StackTrace.Count > 0) is null)
		throw new Exception($"Expected one error log with exception.");
}

// Perform assertions based on the configuration
if (enabled.HasValue)
{
	// When the enabled configuration is set and 'true', we expect the agent configuration to reflect this.
	if (enabled.Value && !Agent.Config.Enabled)
		throw new Exception("Agent should be enabled.");

	// When the enabled configuration is set and 'true', we expect the subscriber to be subscribed.
	if (enabled.Value && !fakeSubscriber.IsSubscribed)
		throw new Exception("Subscriber should be subscribed.");

	// When the enabled configuration is set and 'false', we expect the agent configuration to reflect this.
	if (!enabled.Value && Agent.Config.Enabled)
		throw new Exception("Agent should not be enabled.");

	// When the enabled configuration is set and 'false', we do not expect the subscriber to be subscribed.
	if (!enabled.Value && fakeSubscriber.IsSubscribed)
		throw new Exception("Subscriber should not be subscribed.");
}
else if (enabledEnvironmentVariable is not null)
{
	// When the enabled env var is set and 'true', we expect the agent configuration to reflect this.
	if (envEnabled && !Agent.Config.Enabled)
		throw new Exception("Agent should be enabled.");

	// When the enabled env var is set and 'true', we expect the subscriber to be subscribed.
	if (envEnabled && !fakeSubscriber.IsSubscribed)
		throw new Exception("Subscriber should be subscribed.");

	// When the enabled env var is set and 'false', we expect the agent configuration to reflect this.
	if (!envEnabled && Agent.Config.Enabled)
		throw new Exception("Agent should not be enabled.");

	// When the enabled env var is set and 'false', we do not expect the subscriber to be subscribed.
	if (!envEnabled && fakeSubscriber.IsSubscribed)
		throw new Exception("Subscriber should not be subscribed.");
}
else
{
	// When the enabled configuration is not set and no env var is provided, we expect the agent configuration to default to enabled.
	if (!Agent.Config.Enabled)
		throw new Exception("Agent should be enabled.");

	// When the enabled configuration is not set and no env var is provided, we expect the subscriber to be subscribed.
	if (!fakeSubscriber.IsSubscribed)
		throw new Exception("Subscriber should be subscribed.");
}

// Stop and dispose the host
await host.StopAsync();
host.Dispose();

Console.WriteLine("FINISHED");

namespace Test
{
	public class FakeSubscriber : IDiagnosticsSubscriber
	{
		public bool IsSubscribed { get; set; }

		public IDisposable? Subscribe(IApmAgent components)
		{
			IsSubscribed = true;
			return null;
		}
	}
}

