---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/configuration-on-asp-net.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Configuration on ASP.NET [configuration-on-asp-net]

When monitoring ASP.NET applications the agent uses two sources of configuration:

* Web.config `<appSettings>` section
* Environment variables

Web.config takes precedence over environment variables which means that the agent tries first to find a configuration option value by its key in Web.config. If it’s not present, then the agent tries to look for it among environment variables. If it’s not present, the agent falls back to the options default value.

You can find the key of each configuration option in the `IConfiguration or Web.config key` column of the corresponding option’s description.


## Sample configuration file [asp-net-sample-config]

Below is a sample `Web.config` configuration file for a ASP.NET application.

```xml
<?xml version="1.0" encoding="utf-8"?>
<!-- ... -->
<configuration>
    <!-- ... -->
    <appSettings>
        <!-- ... -->
        <add key="ElasticApm:ServerUrl" value="https://my-apm-server:8200" />
        <add key="ElasticApm:SecretToken" value="apm-server-secret-token" />
        <!-- ... -->
    </appSettings>
    <!-- ... -->
</configuration>
```

Additionally, on ASP.NET, you can implement your own configuration reader. To do this, implement the `IConfigurationReader` interface from the `Elastic.Apm.Config` namespace. Once implemented, you can use the [`FullFrameworkConfigurationReaderType`](#config-full-framework-configuration-reader-type) setting.


## `FullFrameworkConfigurationReaderType` [config-full-framework-configuration-reader-type]

This setting is .NET Full Framework only.

This setting can point an agent to a custom `IConfigurationReader` implementation and the agent will read configuration from your `IConfigurationReader` implementation.

Use type name in  [AssemblyQualifiedName](https://docs.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=netcore-3.1#System_Type_AssemblyQualifiedName) format (e.g: `MyClass, MyNamespace`).

| Environment variable name | Web.config key |
| --- | --- |
| `ELASTIC_APM_FULL_FRAMEWORK_CONFIGURATION_READER_TYPE` | `ElasticApm:FullFrameworkConfigurationReaderType` |

| Default | Type |
| --- | --- |
| None | String |

If this setting is set in both the web.config file and as an environment variable, then the web.config file has precedence.

