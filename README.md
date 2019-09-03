[![Build Status](https://apm-ci.elastic.co/buildStatus/icon?job=apm-agent-dotnet/apm-agent-dotnet-mbp/master)](https://apm-ci.elastic.co/job/apm-agent-dotnet/job/apm-agent-dotnet-mbp/job/master/)
[![codecov](https://codecov.io/gh/elastic/apm-agent-dotnet/branch/master/graph/badge.svg)](https://codecov.io/gh/elastic/apm-agent-dotnet)

# apm-agent-dotnet

Please fill out this survey to help us prioritize framework support:
[https://goo.gl/forms/FHHbhptcDx8eDNx92](https://goo.gl/forms/FHHbhptcDx8eDNx92)

# Installation

Official NuGet packages can be referenced from [NuGet.org](https://www.nuget.org).

| Package Name            | Purpose          | Download         |
| ----------------------- | ---------------- | -----------------|
| `Elastic.Apm`           |  The core of the Agent, Public Agent API, Auto instrumentation for libraries that are part of .NET Standard 2.0  | [![NuGet Release][ElasticApm-image]][ElasticApm-nuget-url]  |
| `Elastic.Apm.AspNetCore` | ASP.NET Core auto instrumentation | [![NuGet Release][ElasticApmAspNetCore-image]][ElasticApmAspNetCore-nuget-url] |
| `Elastic.Apm.EntityFrameworkCore` | Entity Framework Core auto instrumentation | [![NuGet Release][Elastic.Apm.EntityFrameworkCore-image]][Elastic.Apm.EntityFrameworkCore-nuget-url] |
| `Elastic.Apm.NetCoreAll` | References every .NET Core related elastic APM package. It can be used to simply turn on the agent with a single line and activate all auto instrumentation. | [![NuGet Release][Elastic.Apm.NetCoreAll-image]][Elastic.Apm.NetCoreAll-nuget-url] |

## Documentation

Docs are located [here](https://www.elastic.co/guide/en/apm/agent/dotnet/). That page is generated from the content of the [docs](docs) folder.

## Getting Help

If you have any feedback feel free to [open an issue](https://github.com/elastic/apm-agent-dotnet/issues/new).
For any other assistance, please open or add to a topic on the [APM discuss forum](https://discuss.elastic.co/c/apm).

If you need help or hit an issue, please start by opening a topic on our discuss forums.
Please note that we reserve GitHub tickets for confirmed bugs and enhancement requests.

## Contributing

See the [contributing documentation](CONTRIBUTING.md)

## Repository structure

These are the main folders within the repository:
* src: The source code of the agent. Each project within this folder targets a specific library, and there is one core project, which is referenced by all other projects.
    * `Elastic.Apm`: The core project targeting .NET Standard 2.0. It contains the [Agent API](/docs/public-api.asciidoc), the infrastructure to report data to the APM Server, the logging infrastructure, and auto-instrumentation for things that are part of .NET Standard 2.0.
    * `Elastic.Apm.AspNetCore`: Auto-instrumentation for ASP.NET Core.
    * `Elastic.Apm.EntityFrameworkCore`: Auto-instrumentation for EntityFramework Core.
    * `Elastic.Apm.NetCoreAll`: A convenient project that references all other .NET Core related projects from the `src` folder. It contains an ASP.NET Core middleware extension that enables the agent and every other component with a single line of code. In a typical ASP.NET Core application (e.g. apps referencing [Microsoft.AspNetCore.All](https://www.nuget.org/packages/Microsoft.AspNetCore.All)) that uses EF Core the `Elastic.Apm.NetCoreAll` can be referenced.
    * `Elastic.Apm.AspNetFullFramework`: Auto-instrumentation for ASP.NET (classic).
* test: This folder contains test projects. Typically each project from the `src` folder has a corresponding test project.
    * `Elastic.Apm.Tests`: Tests the `Elastic.Apm` project.
    * `Elastic.Apm.AspNetCore.Tests`: Tests the `Elastic.Apm.AspNetCore` project.
    * `Elastic.Apm.AspNetFullFramework.Tests`: Tests the `Elastic.Apm.AspNetFullFramework` project.
    * `Elastic.Apm.Tests.MockApmServer`: Implementation of APM Server mock used for agent-as-component tests (for example in `Elastic.Apm.AspNetFullFramework`).
* docs: This folder contains the official documentation.
* sample: Sample applications that are monitored by the APM .NET Agent. These are also very useful for development: you can start one of these applications and debug the agent through them.
* .ci: This folder contains all the scripts used to build, test and release the agent within the CI.


## License
Elastic APM .Net Agent is licensed under Apache License, Version 2.0.

[ElasticApm-nuget-url]:https://www.nuget.org/packages/Elastic.Apm/
[ElasticApm-image]:
https://img.shields.io/nuget/v/Elastic.Apm.svg

[ElasticApmAspNetCore-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.AspNetCore/
[ElasticApmAspNetCore-image]:
https://img.shields.io/nuget/v/Elastic.Apm.AspNetCore.svg

[Elastic.Apm.EntityFrameworkCore-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.EntityFrameworkCore/
[Elastic.Apm.EntityFrameworkCore-image]:
https://img.shields.io/nuget/v/Elastic.Apm.EntityFrameworkCore.svg

[Elastic.Apm.NetCoreAll-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.NetCoreAll/
[Elastic.Apm.NetCoreAll-image]:
https://img.shields.io/nuget/v/Elastic.Apm.NetCoreAll.svg
