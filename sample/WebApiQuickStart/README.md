# End to End WebApi QuickStart

This End to End demo will introduce APM stack + APM dotnet webapi agent for test and development purpose (do not use it for production)

The full csharp webapi demo is available under AspNetCore.Demo folder

## Setup an apm-server

This part will setup APM stack (apm-server + elasticsearch + apm-server) locally for development and test purpose to enable end to end AspNetCore quick demo.

### By using the latest version

The docker file from the [apm-server project](https://github.com/elastic/apm-server)

```bash
curl -L -O https://raw.githubusercontent.com/elastic/apm-server/main/docker-compose.yml && docker compose up -d && docker compose logs -f
```

### Or using a lighter one

A copy of the apm-server `docker-compose.yml` project with less services (for development and test only)

```bash
docker compose up -d && docker compose logs -f
```

## Create a WebApiDemo

A full demo is available under AspNetCore.Demo folder.

The AspNetCore.Demo has been built following those steps

### Create the webapi project + reference Elastic.Apm.AspNetCore package

```bash
dotnet new webapi --name AspNetCore.Demo
dotnet add AspNetCore.Demo package Elastic.Apm.AspNetCore
```

### Configure the app

This configuration is well explained under the [apm-quickstart](https://www.elastic.co/guide/en/apm/guide/current/apm-quick-start.html)

open the file Properties>launchSettings.json and add the following configuration

```json
{
  "ElasticApm": {
    "SecretToken": "",
    "ServerUrls": "http://localhost:8200",
    "ServiceName": "AspNetCoreDemo"
}
```

The ElasticApm ServerUrls `http://localhost:8200`: the `apm-server` docker compose service

### Enable the Agent

Program.cs

```csharp
using Elastic.Apm.AspNetCore;
using Elastic.Apm.DiagnosticSource;
// ...
app.UseElasticApm(builder.Configuration, new HttpDiagnosticsSubscriber());
// ...
```

### Run the application

```
dotnet run
```

### Test your application

By default the webapi dotnet template uses a fake weatherapi
Right after running the AspNetCore.Demo webapi, test the api at `http://localhost:5264/weatherforecast`
After the first call to this api, the apm service `AspNetCoreDemo` will become available by following the next step.

### Open the APM kibana dashboard

Open In your favorite browser the [kibana apm services dashboard](http://localhost:5601/app/apm/services)
