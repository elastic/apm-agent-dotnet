# apm-agent-dotnet

[![Build Status](https://apm-ci.elastic.co/buildStatus/icon?job=apm-agent-dotnet/apm-agent-dotnet-mbp/master)](https://apm-ci.elastic.co/job/apm-agent-dotnet/job/apm-agent-dotnet-mbp/job/master/)
[![codecov](https://codecov.io/gh/elastic/apm-agent-dotnet/branch/master/graph/badge.svg)](https://codecov.io/gh/elastic/apm-agent-dotnet)

Please fill out this survey to help us prioritize framework support:
[https://goo.gl/forms/FHHbhptcDx8eDNx92](https://goo.gl/forms/FHHbhptcDx8eDNx92)

## Installation

Official NuGet packages can be referenced from [NuGet.org](https://www.nuget.org).

| Package Name            | Purpose          | Download         |
| ----------------------- | ---------------- | -----------------|
| `Elastic.Apm`           |  The core of the Agent, Public Agent API, Auto instrumentation for libraries that are part of .NET Standard 2.0.  | [![NuGet Release][ElasticApm-image]][ElasticApm-nuget-url]  |
| `Elastic.Apm.AspNetCore` | ASP.NET Core auto instrumentation. | [![NuGet Release][ElasticApmAspNetCore-image]][ElasticApmAspNetCore-nuget-url] |
| `Elastic.Apm.EntityFrameworkCore` | Entity Framework Core auto instrumentation. | [![NuGet Release][Elastic.Apm.EntityFrameworkCore-image]][Elastic.Apm.EntityFrameworkCore-nuget-url] |
| `Elastic.Apm.NetCoreAll` | References every .NET Core related elastic APM package. It can be used to simply turn on the agent with a single line and activate all auto instrumentation. | [![NuGet Release][Elastic.Apm.NetCoreAll-image]][Elastic.Apm.NetCoreAll-nuget-url] |
| `Elastic.Apm.AspNetFullFramework` | ASP.NET (classic) auto instrumentation with an IIS Module. | [![NuGet Release][Elastic.Apm.AspNetFullFramework-image]][Elastic.Apm.AspNetFullFramework-nuget-url] |
| `Elastic.Apm.EntityFramework6` | Entity Framework 6 auto instrumentation. | [![NuGet Release][Elastic.Apm.EntityFramework6-image]][Elastic.Apm.EntityFramework6-nuget-url] |
| `Elastic.Apm.SqlClient` | `System.Data.SqlClient` and `Microsoft.Data.SqlClient` auto instrumentation. [More details](/src/Elastic.Apm.SqlClient/README.md) | [![NuGet Release][Elastic.Apm.SqlClient-image]][Elastic.Apm.SqlClient-nuget-url] |
| `Elastic.Apm.Elasticsearch` | Integration with the .NET clients for Elasticsearch. | [![NuGet Release][Elastic.Apm.Elasticsearch-image]][Elastic.Apm.Elasticsearch-nuget-url] |
| `Elastic.Apm.StackExchange.Redis` | Integration with the StackExchange.Redis client for Redis. | [![NuGet Release][Elastic.Apm.StackExchange.Redis-image]][Elastic.Apm.StackExchange.Redis-nuget-url] |

## Documentation

Docs are located [here](https://www.elastic.co/guide/en/apm/agent/dotnet/). That page is generated from the content of the [docs](docs) folder.

## Getting Help

If you have any feedback feel free to [open an issue](https://github.com/elastic/apm-agent-dotnet/issues/new).
For any other assistance, please open or add to a topic on the [APM discuss forum](https://discuss.elastic.co/c/apm).

If you need help or hit an issue, please start by opening a topic on our discuss forums.
Please note that we reserve GitHub tickets for confirmed bugs and enhancement requests.

## Contributing

See the [contributing documentation](CONTRIBUTING.md)

## Releasing

See the [releasing documentation](RELEASING.md)

## Repository structure

These are the main folders within the repository:

* `src`: The source code of the agent. Each project within this folder targets a specific library, and there is one core project, which is referenced by all other projects.
  * `Elastic.Apm`: The core project targeting .NET Standard 2.0. It contains the [Agent API](/docs/public-api.asciidoc), the infrastructure to report data to the APM Server, the logging infrastructure, and auto-instrumentation for things that are part of .NET Standard 2.0.
  * `Elastic.Apm.AspNetCore`: Auto-instrumentation for ASP.NET Core.
  * `Elastic.Apm.EntityFrameworkCore`: Auto-instrumentation for EntityFramework Core.
  * `Elastic.Apm.NetCoreAll`: A convenient project that references all other .NET Core related projects from the `src` folder. It contains an ASP.NET Core middleware extension that enables the agent and every other component with a single line of code. In a typical ASP.NET Core application (e.g. apps referencing [Microsoft.AspNetCore.All](https://www.nuget.org/packages/Microsoft.AspNetCore.All)) that uses EF Core the `Elastic.Apm.NetCoreAll` can be referenced.
  * `Elastic.Apm.AspNetFullFramework`: Auto-instrumentation for ASP.NET (classic).
  * `Elastic.Apm.EntityFramework6`: Auto-instrumentation for Entity Framework 6.
  * `Elastic.Apm.SqlClient`: Auto-instrumentation for `System.Data.SqlClient` and `Microsoft.Data.SqlClient`.
  * `Elastic.Apm.Elasticsearch`: Auto-instrumentation for the official .NET clients for Elasticsearch.
  * `Elastic.Apm.StackExchange.Redis`: Auto-instrumentation for the StackExchange.Redis client for Redis.
* `test`: This folder contains test projects. Typically each project from the `src` folder has a corresponding test project.
  * `Elastic.Apm.Tests`: Tests the `Elastic.Apm` project.
  * `Elastic.Apm.AspNetCore.Tests`: Tests the `Elastic.Apm.AspNetCore` project.
  * `Elastic.Apm.AspNetFullFramework.Tests`: Tests the `Elastic.Apm.AspNetFullFramework` project.
  * `Elastic.Apm.Tests.MockApmServer`: Implementation of APM Server mock used for agent-as-component tests (for example in `Elastic.Apm.AspNetFullFramework.Tests`).
* `docs`: This folder contains the official documentation.
* `sample`: Sample applications that are monitored by the APM .NET Agent. These are also very useful for development: you can start one of these applications and debug the agent through them.
* `.build`: Contains files used when building the solution, and [a project to perform
common build tasks](build/README.md).
* `.ci`: This folder contains all the scripts used to build, test and release the agent within the CI.

## APM .NET agent auto-instrumentation

This repository contains an implementation of a CLR profiler in Rust for auto instrumentation
for the Elastic APM .NET agent. It has been tested to work for both CLR (Windows) and 
CoreCLR (Windows and Linux).

[**Planning documentation**](https://docs.google.com/document/d/11UiFxrjBUc3ICH7lgElYstYW0yEyAgGIAeNdNdoztzw/edit?usp=sharing)

### Getting started

1. [Install Rust](https://www.rust-lang.org/tools/install)
2. Install cargo make

  ```sh
  cargo install --force cargo-make
  ```

3. Install [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0) and [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet/3.1) SDKs
4. Install an IDE to work with Rust
- [CLion](https://www.jetbrains.com/clion/) with [Rust plugin](https://www.jetbrains.com/rust/)
- [Intellij IDEA](https://www.jetbrains.com/idea/) with [Rust plugin](https://www.jetbrains.com/rust/)
- VS Code with [Rust plugin](https://marketplace.visualstudio.com/items?itemName=rust-lang.rust)

5. Compile profiler and .NET applications, and run the Example .NET console application with profiler attached

  ```sh~~~~
  cargo make test-example
  ```

The log level of the profiler can be controlled with the `ELASTIC_APM_PROFILER_LOG` environment variable, which can be passed to cargo make

  ```sh
  cargo make test-example --env ELASTIC_APM_PROFILER_LOG=trace
  ```

Similarly, other environment variables can be provided with `--env {key=value}` arguments to override
any default values defined in the `[env]` section in [Makefile.toml](Makefile.toml).

## License

Elastic APM .NET Agent is licensed under Apache License, Version 2.0.

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

[Elastic.Apm.AspNetFullFramework-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.AspNetFullFramework/
[Elastic.Apm.AspNetFullFramework-image]:
https://img.shields.io/nuget/v/Elastic.Apm.AspNetFullFramework.svg

[Elastic.Apm.EntityFramework6-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.EntityFramework6/
[Elastic.Apm.EntityFramework6-image]:
https://img.shields.io/nuget/v/Elastic.Apm.EntityFramework6.svg

[Elastic.Apm.SqlClient-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.SqlClient/
[Elastic.Apm.SqlClient-image]:
https://img.shields.io/nuget/v/Elastic.Apm.SqlClient.svg

[Elastic.Apm.Elasticsearch-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.Elasticsearch/
[Elastic.Apm.Elasticsearch-image]:
https://img.shields.io/nuget/v/Elastic.Apm.Elasticsearch.svg

[Elastic.Apm.StackExchange.Redis-nuget-url]:https://www.nuget.org/packages/Elastic.Apm.StackExchange.Redis/
[Elastic.Apm.StackExchange.Redis-image]:
https://img.shields.io/nuget/v/Elastic.Apm.StackExchange.Redis.svg
