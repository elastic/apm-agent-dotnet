# ElasticApmStartupHook

This project contains the StartupHook logic that loads the Elastic APM Agent into a .NET Core application during startup.

It utilizes the startup hook mechanism described [here](https://github.com/dotnet/runtime/blob/master/docs/design/features/host-startup-hook.md).

### How to use it

Agent assemblies need to be loaded onto the disk where the traced .NET app is running. The agent with all the necessary files is currently distributed via GitHub on the [releases page](https://github.com/elastic/apm-agent-dotnet/releases) through a zip file (ElasticApmAgent_[version].zip).

Then set environment variable to point to the agent:

```
set DOTNET_STARTUP_HOOKS=[pathToAgent]\ElasticApmAgentStartupHook.dll
```

With that, the agent will initialize itself during startup.

### Prerequisites

- .NET Core 2.2 or newer 
- All `.dll` files from the Elastic .NET APM Agent distribution must be present in the folder next to the `ElasticApmAgentStartupHook.dll` file.

### Troubleshooting

- Log level can be changed via environment variables as described in the [agent documentation](https://www.elastic.co/guide/en/apm/agent/dotnet/current/config-all-options-summary.html).
- If an assembly is not present, the application will fail with a `FileNotFoundException` or with an `TypeLoadException`. In this case the exception message typically prints the name of the missing assembly.