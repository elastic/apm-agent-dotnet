---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/setup-azure-functions.html
---

# Azure Functions [setup-azure-functions]

The .NET APM Agent can trace HTTP triggered function invocations in an [Azure Functions](https://learn.microsoft.com/en-us/azure/azure-functions) app.


## Prerequisites [_prerequisites]

You need an APM Server to send APM data to. Follow the [APM Quick start](docs-content://solutions/observability/apps/get-started-with-apm.md) if you have not set one up yet. You will need your **APM server URL** and an APM server **secret token** (or **API key**) for configuring the APM agent below.

You will also need an Azure Function app to monitor. If you do not have an existing one, you can follow [this Azure guide](https://learn.microsoft.com/en-us/azure/azure-functions/create-first-function-cli-csharp) to create one.

You can also take a look at and use this [Azure Functions example app with Elastic APM already integrated](https://github.com/elastic/apm-agent-dotnet/tree/main/test/azure/applications/Elastic.AzureFunctionApp.Isolated).


## Azure Functions isolated worker model [_azure_functions_isolated_worker_model]


### Step 1: Add the NuGet package [azure-functions-setup]

Add the `Elastic.Apm.Azure.Functions` NuGet package to your Azure Functions project:

```bash
dotnet add package Elastic.Apm.Azure.Functions
```


### Step 2: Add the tracing Middleware [_step_2_add_the_tracing_middleware]

For the APM agent to trace Azure Functions invocations, the `Elastic.Apm.Azure.Functions.ApmMiddleware` must be used in your Azure Functions app.

```csharp
using Elastic.Apm.Azure.Functions;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
  .ConfigureFunctionsWebApplication(builder =>
  {
    builder.UseMiddleware<ApmMiddleware>();
  })
  .Build();

host.Run();
```


### Step 3: Configure the APM agent [_step_3_configure_the_apm_agent]

The APM agent can be configured with environment variables.

```yaml
ELASTIC_APM_SERVER_URL: <your APM server URL from the prerequisites step>
ELASTIC_APM_SECRET_TOKEN: <your APM secret token from the prerequisites step>
ELASTIC_APM_ENVIRONMENT: <your environment>
ELASTIC_APM_SERVICE_NAME: <your service name> (optional)
```

If `ELASTIC_APM_SERVICE_NAME` is not configured, the agent will use a fallback value.

* **Local development** - The discovered service name (the entry Assembly name) will be used.
* **Azure** - The Function App name (retrieved from the `WEBSITE_SITE_NAME` environment variable) will be used.

**Configuring in Local development**

While developing your Function locally, you can configure the agent by providing the environment variables via the `local.settings.json` file.

For example:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ELASTIC_APM_ENVIRONMENT": "Development",
    "ELASTIC_APM_SERVICE_NAME": "MyServiceName",
    "ELASTIC_APM_SERVER_URL": "https://my-serverless-project.apm.eu-west-1.aws.elastic.cloud:443",
    "ELASTIC_APM_API_KEY": "MySecureApiKeyFromApmServer=="
  }
}
```

**Configuring in Azure**

Using environment variables allows you to use [application settings in the Azure Portal](https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-use-azure-function-app-settings?tabs=portal#settings), enabling you to update settings without needing to re-deploy code.

Open *Settings > Environment variables* for your Function App in the Azure Portal and configure the ELASTIC_APM_* variables as required.

For example:

:::{image} ../images/azure-functions-configuration.png
:alt: Configuring the APM Agent in the Azure Portal
:::


## Limitations [azure-functions-limitations]

Azure Functions instrumentation currently does *not* collect system metrics in the background because of a concern with unintentionally increasing Azure Functions costs (for Consumption plans).

