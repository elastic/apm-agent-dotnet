---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-ef6.html
---

# Entity Framework 6 [setup-ef6]


## Quick start [_quick_start_6]

You can enable auto instrumentation for Entity Framework 6 by referencing the [`Elastic.Apm.EntityFramework6`](https://www.nuget.org/packages/Elastic.Apm.EntityFramework6) package and including the `Ef6Interceptor` interceptor in your application’s `web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <entityFramework>
        <interceptors>
            <interceptor type="Elastic.Apm.EntityFramework6.Ef6Interceptor, Elastic.Apm.EntityFramework6" />
        </interceptors>
    </entityFramework>
</configuration>
```

As an alternative to registering the interceptor via the configuration, you can register it in the application code:

```csharp
DbInterception.Add(new Elastic.Apm.EntityFramework6.Ef6Interceptor());
```

For example, in an ASP.NET application, you can place the above call in the `Application_Start` method.

Instrumentation works with EntityFramework 6.2+ NuGet packages.

::::{note}
Be careful not to execute `DbInterception.Add` for the same interceptor type more than once, as this will register multiple instances, causing multiple database spans to be captured for every SQL command.
::::


