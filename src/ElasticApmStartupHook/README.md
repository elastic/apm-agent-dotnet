# ElasticApmStartupHook

This project contains the StartupHook logic that loads the Elastic APM Agent into a .NET Core application during startup.

It utilizes the startup hook mechanism described [here](https://github.com/dotnet/runtime/blob/master/docs/design/features/host-startup-hook.md).

### How to use it

The agent assemblies need to be loaded onto the disk where the .NET app is running, and then set environment variable to point to the agent:

```
set DOTNET_STARTUP_HOOKS=[pathToAgent]\ElasticApmAgentStartupHook.dll
```

With that, the agent will initialize itself during startup.