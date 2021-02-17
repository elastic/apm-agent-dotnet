# Elastic Apm Agent Startup Hook

This project contains startup hook logic that loads the Elastic APM Agent and dependencies into a .NET Core application during startup, allowing
auto instrumentation of HTTP requests, ASP.NET Core, Entity Framework Core,
SQL client and Elasticsearch client (NEST/Elasticsearch.Net) without code changes.

It utilizes the startup hook mechanism described [in the dotnet runtime repository](https://github.com/dotnet/runtime/blob/master/docs/design/features/host-startup-hook.md).

### Prerequisites

- **.NET Core 2.2 or newer**

### How to use it

Agent assemblies need to be loaded onto the disk where the traced .NET app is running. The agent with all the necessary files is currently distributed via GitHub on the [releases page](https://github.com/elastic/apm-agent-dotnet/releases) through a zip file, ElasticApmAgent_`<version>`.zip.

Then set environment variable to point to the agent:

```
set DOTNET_STARTUP_HOOKS=[pathToAgent]\ElasticApmAgentStartupHook.dll
```

With the environment variable set, the agent will initialize itself during startup when the application is started with `dotnet`.

Agent configuration such as log level can be changed via environment variables as described in the [agent documentation](https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-all-options-summary.html).

### Building the zip file

A zip file can be built from a branch with the build script in the repository root:

For Windows

```shell
.\build.bat agent-zip
```

For macOS or Linux

```shell
./build.sh agent-zip
```

A versioned zip file can then be found in the `build/output` directory.

### Zip layout

The layout in the zip file is as follows

```
ElasticApmAgent_<version>.zip/
├── 4.0.0/
│   └── APM agent assemblies compiled against System.Diagnostics.DiagnosticSource 4.0.0
│
├── 5.0.0/
│   └── APM agent assemblies compiled against System.Diagnostics.DiagnosticSource 5.0.0
│
└── ElasticApmAgentStartupHook.dll
```

It contains numbered directories that each contain APM agent assemblies compiled against
a specific version System.Diagnostics.DiagnosticSource.

### Troubleshooting

- Startup hook logging can be enabled by setting the `ELASTIC_APM_STARTUP_HOOKS_LOGGING` environment variable

   ```
   set ELASTIC_APM_STARTUP_HOOKS_LOGGING=1
   ```

- If an assembly is not present, the application will fail with a `FileNotFoundException` or `TypeLoadException`. If this happens, the exception message typically prints the name of the missing assembly.