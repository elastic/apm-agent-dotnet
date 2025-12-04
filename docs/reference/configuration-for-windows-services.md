---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/configuration-for-windows-services.html
applies_to:
  stack:
  serverless:
    observability:
  product:
    apm_agent_dotnet: ga
---

# Configuration for Windows services [configuration-for-windows-services]

::::{important}
While the APM agent supports transaction auto-creation for web frameworks such as ASP.NET and ASP.NET Core, it
doesn't know where the "unit of work" starts for bespoke services. Therefore, it cannot start a transaction
automatically. As a result, spans, such as those for outbound HTTP requests, are also not captured, as they expect
a running transaction. Therefore, no trace data will be generated or exported out of the box.

You will need to manually instrument the code for the service to manually create a transaction around the appropriate
unit of work for your scenario. A custom transaction can be started via the [Public API](/reference/public-api.md).

Alternatively, consider using the [Elastic Distribution of OpenTelemetry for .NET](https://www.elastic.co/docs/reference/opentelemetry/edot-sdks/dotnet)
where any spans, including those for outbound HTTP requests, are automatically captured. The first span without a
parent will be considered a transaction when ingested into Elastic Observability.
::::

Configuration for Windows services can be provided by setting environment variables for the specific Windows service in the Windows registry. With PowerShell

```powershell
$environment = [string[]]@(
  "ELASTIC_APM_SERVER_URL=http://localhost:8200", <1>
  "ELASTIC_APM_TRANSACTION_SAMPLE_RATE=1",
  "ELASTIC_APM_ENVIRONMENT=Production",
  "ELASTIC_APM_SERVICE_NAME=MyWindowsService")

Set-ItemProperty HKLM:SYSTEM\CurrentControlSet\Services\<service-name> -Name Environment -Value $environment <2>
```

1. define the environment variables to use for the Windows service
2. `<service-name>` is the name of the Windows service.

The service must then be restarted for the change to take effect

```powershell
Restart-Service <service-name>
```
