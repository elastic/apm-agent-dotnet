---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/configuration-on-asp-net-core.html
description: "How to configure the Elastic APM .NET Agent in ASP.NET Core applications using the Microsoft.Extensions.Configuration infrastructure and appsettings.json."
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Configuration on ASP.NET Core [configuration-on-asp-net-core]

The `AddElasticApm()` extension method on the `IServiceCollection` automatically accesses configuration bound via the `Microsoft.Extensions.Configuration` sources. To use this type of setup, which is typical in an ASP.NET Core application, your application’s `Program.cs` file should contain code similar to the following:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAllElasticApm();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Run();
```

With this setup, the Agent is able to be configured in the same way as any other library in your application. For example, any configuration source that has been configured on the `IConfiguration` instance in use in the application can be used to set Agent configuration values.

More information is available in the official [Microsoft .NET Core configuration docs](https://learn.microsoft.com/aspnet/core/fundamentals/configuration). You can find the key for each {{product.apm-agent-dotnet}} configuration option in this documentation, under the `IConfiguration or Web.config key` column of the option’s description.

::::{note}
The `AddElasticApm` method only turns on ASP.NET Core monitoring. To turn on tracing for everything supported by the Agent on .NET Core, including HTTP and database monitoring, use the `AddAllElasticApm` method from the `Elastic.Apm NetCoreAll` package. Learn more in [ASP.NET Core setup](/reference/setup-asp-net-core.md).
::::


## Sample configuration file [sample-config]

Here is a sample `appsettings.json` configuration file for a typical ASP.NET Core application that has been activated with `AddElasticApm()`. There is one important takeaway, listed as a callout below the example:

```js
{
  "Logging": {
    "LogLevel": { <1>
      "Default": "Warning",
      "Elastic.Apm": "Debug"
    }
  },
  "AllowedHosts": "*",
  "ElasticApm":
    {
      "ServerUrl":  "http://myapmserver:8200",
      "SecretToken":  "apm-server-secret-token",
      "TransactionSampleRate": 1.0
    }
}
```

1. With ASP.NET Core, you must set `LogLevel` for the internal {{product.apm-agent-dotnet}} logger in the standard `Logging` section with the `Elastic.Apm` category name.


In certain scenarios, like when you’re not using ASP.NET Core, you won’t activate the agent with the `AddElasticApm()` method. In this case, set the agent log level with [`ElasticApm:LogLevel`](/reference/config-supportability.md#config-log-level), as shown in the following `appsettings.json` file:

```js
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ElasticApm":
    {
      "LogLevel":  "Debug",
      "ServerUrl":  "http://myapmserver:8200",
      "SecretToken":  "apm-server-secret-token",
      "TransactionSampleRate": 1.0
    }
}
```


## Overriding configuration values programmatically [asp-net-core-programmatic-config]

Because the agent reads all configuration from `IConfiguration`, you can inject computed or derived values at startup using the [in-memory configuration provider](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#memory-configuration-provider). Call `AddInMemoryCollection` on `builder.Configuration` before `AddElasticApm` or `AddAllElasticApm`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Derive the {{product.apm}} environment from application-specific configuration
var apmEnvironment = $"{builder.Configuration["App:Region"]}-{builder.Environment.EnvironmentName}";

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ElasticApm:ServiceName"] = "My Service",
    ["ElasticApm:Environment"] = apmEnvironment
});

builder.Services.AddAllElasticApm();
```

This pattern is useful when you need to set agent values that are derived from other configuration, fetched from a secrets manager, or otherwise not expressible as static file entries.

For example, to supply an API key retrieved from a secrets manager such as Azure Key Vault or AWS Secrets Manager:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Fetch the API key from your secrets manager at startup
var apiKey = GetApiKeyFromVault("elastic-apm-api-key"); // pseudo-code: replace with your secrets manager client call

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ElasticApm:ApiKey"] = apiKey
});

builder.Services.AddAllElasticApm();
```

::::{note}
When not explicitly configured, the agent’s `Environment` option defaults to the ASP.NET Core hosting environment name from `IHostEnvironment.EnvironmentName` (typically driven by `ASPNETCORE_ENVIRONMENT`). If you want {{product.apm-agent-dotnet}} to report a different environment label, set `ElasticApm:Environment` explicitly as shown in the preceding example.
::::

**Source ordering and environment variable precedence**

The agent resolves each configuration option through two layers in order:

1. `IConfiguration` — checked first. This includes all registered sources: `appsettings.json`, `ElasticApm__*` environment variables (double underscore is the .NET section separator), command-line arguments, and any in-memory values.
2. `ELASTIC_APM_*` environment variables — the agent's own env var form, checked only when layer 1 returns no value.

Because `AddInMemoryCollection` is appended to the end of the `IConfiguration` source list, it wins over all other layer 1 sources. It also implicitly overrides `ELASTIC_APM_*` variables, because a hit in layer 1 means the agent never falls through to layer 2.

If you want either form of environment variable to remain able to override a value at deployment time, check for both before injecting:

```csharp
var overrides = new Dictionary<string, string?>();

if (builder.Configuration["ElasticApm:Environment"] is null
    && Environment.GetEnvironmentVariable("ELASTIC_APM_ENVIRONMENT") is null)
    overrides["ElasticApm:Environment"] = DeriveEnvironment(builder.Configuration); // replace with your own logic

if (overrides.Count > 0)
    builder.Configuration.AddInMemoryCollection(overrides);

builder.Services.AddAllElasticApm();
```

Most agent configuration options are read once when the agent initializes at startup. In-memory values must be in place before `builder.Build()` is called; changes made after that point will not be picked up.

The `IConfiguration` key for each agent option is listed under the **IConfiguration or Web.config key** column in the [configuration reference](/reference/configuration.md).
