---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/configuration-on-asp-net-core.html
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

More information is available in the official [Microsoft .NET Core configuration docs](https://learn.microsoft.com/aspnet/core/fundamentals/configuration) You can find the key for each APM configuration option in this documentation, under the `IConfiguration or Web.config key` column of the option’s description.

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

1. With ASP.NET Core, you must set `LogLevel` for the internal APM logger in the standard `Logging` section with the `Elastic.Apm` category name.


In certain scenarios—​like when you’re not using ASP.NET Core—​you won’t activate the agent with the `AddElasticApm()` method. In this case, set the agent log level with [`ElasticApm:LogLevel`](/reference/config-supportability.md#config-log-level), as shown in the following `appsettings.json` file:

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

