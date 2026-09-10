---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-asp-dot-net.html
description: "How to set up the Elastic APM .NET Agent to trace ASP.NET full framework applications using the ElasticApmModule IIS module."
navigation_title: ASP.NET
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Set up ASP.NET instrumentation [setup-asp-dot-net]


## Supported versions [_supported_versions_asp_net]

| Framework | Supported versions |
| --- | --- |
| ASP.NET (.NET Framework) | 4.6.2–4.8.1 (IIS 10) |

For the full compatibility matrix including supported installation methods, refer to [Web frameworks](/reference/supported-technologies.md#supported-web-frameworks).


## Quick start [_quick_start_4]

To enable tracing for ASP.NET (.NET Framework), install the `Elastic.Apm.AspNetFullFramework` package, add a reference to the package in your `web.config` file, and then compile and deploy your application.

1. Ensure you have access to the application source code and install the [`Elastic.Apm.AspNetFullFramework`](https://www.nuget.org/packages/Elastic.Apm.AspNetFullFramework) package.
2. Reference the `Elastic.Apm.AspNetFullFramework` in your application’s `web.config` file by adding the `ElasticApmModule` IIS module:

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <system.webServer>
            <modules>
                <add name="ElasticApmModule" type="Elastic.Apm.AspNetFullFramework.ElasticApmModule, Elastic.Apm.AspNetFullFramework" />
            </modules>
        </system.webServer>
    </configuration>
    ```

    ::::{note}
    There are two available configuration sources. To learn more, see [Configuration on ASP.NET](/reference/configuration-on-asp-net.md).
    ::::


    By default, the agent creates transactions for all HTTP requests, including static content: .html pages, images, and so on.

    To create transactions only for HTTP requests with dynamic content, such as `.aspx` pages, add the `managedHandler` preCondition to your `web.config` file:

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <system.webServer>
            <modules>
                <add name="ElasticApmModule" type="Elastic.Apm.AspNetFullFramework.ElasticApmModule, Elastic.Apm.AspNetFullFramework" preCondition="managedHandler" />
            </modules>
        </system.webServer>
    </configuration>
    ```

    ::::{note}
    To learn more about adding modules, see the [Microsoft docs](https://docs.microsoft.com/en-us/iis/configuration/system.webserver/modules/add).
    ::::


::::{note}
Our IIS module requires:

* IIS 7 or later
* Application pool’s pipeline mode has to be set to integrated (default for IIS 7 and up)
* The deployed .NET application must NOT run under quirks mode. This makes `LegacyAspNetSynchronizationContext` the async context handler and can break `HttpContext.Items` correctly restoring when async code introduces a thread switch.
::::

::::{important}
**Binding redirects.** The agent's .NET Framework assemblies reference the 8.x line of the .NET platform packages, for example `System.Diagnostics.DiagnosticSource` 8.0.x. ASP.NET applications, including SDK-style projects, do not automatically update `web.config` with binding redirects at build time. Add or update the required redirects in `web.config`, or configure your build tooling to do so. Regardless of project style, verify that the deployed `web.config` redirects the assemblies that the agent brings into the application to the versions deployed to the application's `bin` folder. Missing or outdated redirects can prevent the module from loading with a `FileLoadException`.

The following redirects cover the assemblies the agent itself depends on. The versions match the packages the agent depends on at this release:

```xml
<runtime>
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    <dependentAssembly>
      <assemblyIdentity name="System.Diagnostics.DiagnosticSource" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-8.0.0.1" newVersion="8.0.0.1" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Threading.Tasks.Dataflow" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-8.0.0.1" newVersion="8.0.0.1" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Diagnostics.PerformanceCounter" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-8.0.0.1" newVersion="8.0.0.1" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Reflection.Metadata" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-8.0.0.1" newVersion="8.0.0.1" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Collections.Immutable" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-8.0.0.0" newVersion="8.0.0.0" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Threading.Tasks.Extensions" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-4.2.0.1" newVersion="4.2.0.1" />
    </dependentAssembly>
    <!-- The next two entries apply only to applications targeting .NET Framework 4.6.2, where the agent brings these two
         packages. Include them only when the corresponding assemblies are present in your bin folder. -->
    <dependentAssembly>
      <assemblyIdentity name="System.ValueTuple" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-4.0.5.0" newVersion="4.0.5.0" />
    </dependentAssembly>
    <dependentAssembly>
      <assemblyIdentity name="System.Runtime.InteropServices.RuntimeInformation" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-4.0.1.0" newVersion="4.0.1.0" />
    </dependentAssembly>
  </assemblyBinding>
</runtime>
```

In every entry, `newVersion` must be the assembly version of the file that is actually in your `bin` folder, and the upper bound of `oldVersion` must be the same version. If another package in your application brings a newer version of one of these assemblies, or of shared dependencies such as `System.Memory`, `System.Buffers` and `System.Numerics.Vectors`, redirect to the version in `bin` instead of the values shown here. Check the versions again after updating the agent, because they change when the agent updates its dependencies.
::::


1. Recompile your application and deploy it.

    The `ElasticApmModule` instantiates the {{product.apm-agent-dotnet}} on the first initialization. However, there might be some scenarios where you want to control the agent instantiation, such as configuring filters in the application start.

    To do so, the `ElasticApmModule` exposes a `CreateAgentComponents()` method that returns agent components configured to work with ASP.NET Full Framework, which can then instantiate the agent.

    For example, you can add transaction filters to the agent in the application start:

    ```csharp
    using Elastic.Apm;
    using Elastic.Apm.Api;
    using Elastic.Apm.AspNetFullFramework;

    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // other application startup for example, RouteConfig, and so on.

            // set up agent with components
            var agentComponents = ElasticApmModule.CreateAgentComponents();
            Agent.Setup(agentComponents);

            // add transaction filter
            Agent.AddFilter((ITransaction t) =>
            {
                t.SetLabel("foo", "bar");
                return t;
            });
        }
    }
    ```

    Now, the `ElasticApmModule` will use the instantiated instance of the {{product.apm-agent-dotnet}} upon initialization.


## Configure the agent [asp-net-configuration]

After adding the agent, configure it to connect to your {{product.apm-server}}. The fastest way is through environment variables or `web.config`. See [Minimum configuration](/reference/configuration.md#minimum-configuration) for the three settings every deployment needs.

For the full list of configuration options available to ASP.NET applications, see [Configuration on ASP.NET](/reference/configuration-on-asp-net.md).
