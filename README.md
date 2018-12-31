# apm-agent-dotnet

Please fill out this survey to help us prioritizing framework support:
[https://goo.gl/forms/FHHbhptcDx8eDNx92](https://goo.gl/forms/FHHbhptcDx8eDNx92)

This agent is in prototyping state and under heavy development. Do not deploy into production!

## Documentation

Docs are located [here](docs).

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
    * `Elastic.Apm`: The core project, which contains the [Agent API](/docs/public-api.asciidoc), the infrastructure to report data to the APM Server, the logging infrastructure, and autoinstrumentation for things that are part of .NET Standard 2.0.
    * `Elastic.Apm.AspNetCore`: Autoinstrumentation for ASP.NET Core.
    * `Elastic.Apm.EntityFrameworkCore`: Autoinstrumentation for EntityFramework Core.
* test: This folder contains test projects. Typically each project from the `src` folder has a corresponding test project.
    * `Elastic.Apm.Tests`: Tests the `Elastic.Apm` project.
    * `Elastic.Apm.AspNetCore.Tests`: Tests the `Elastic.Apm.AspNetCore` project.
* docs: This folder contains the official documentation.
* sample: Sample applications that are monitored by the APM .NET Agent. These are also very useful for development: you can start one of these applications and debug the agent through them.