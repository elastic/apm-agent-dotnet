---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-asp-dot-net.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# ASP.NET [setup-asp-dot-net]


## Quick start [_quick_start_4]

To enable auto instrumentation for ASP.NET (.NET Framework), you need to install the `Elastic.Apm.AspNetFullFramework` package, add a reference to the package in your `web.config` file, and then compile and deploy your application.

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


    By default, the agent creates transactions for all HTTP requests, including static content: .html pages, images, etc.

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


1. Recompile your application and deploy it.

    The `ElasticApmModule` instantiates the APM agent on the first initialization. However, there may be some scenarios where you want to control the agent instantiation, such as configuring filters in the application start.

    To do so, the `ElasticApmModule` exposes a `CreateAgentComponents()` method that returns agent components configured to work with ASP.NET Full Framework, which can then instantiate the agent.

    For example, you can add transaction filters to the agent in the application start:

    ```csharp
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // other application startup e.g. RouteConfig, etc.

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

    Now, the `ElasticApmModule` will use the instantiated instance of the APM agent upon initialization.


