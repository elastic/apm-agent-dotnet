---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/configuration-for-windows-services.html
---

# Configuration for Windows Services [configuration-for-windows-services]

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

