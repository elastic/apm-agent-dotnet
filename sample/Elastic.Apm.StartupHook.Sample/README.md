# Sample ASP.NET (Core) application for startup hooks

This sample application is a default ASP.NET (Core) application
configured to run with 

- `netcoreapp3.0`
- `netcoreapp3.1`
- `net5.0` 
  
target frameworks that can be used to try out the [Elastic APM
startup hooks implementation](../../src/ElasticApmAgentStartupHook).

## How to use

1. Obtain the zip file containing the Elastic APM startup hooks and unzip it in a location accessible to this application.
2. Set the `DOTNET_STARTUP_HOOKS` environment variable to point to the `ElasticApmAgentStartupHook.dll` in the unzipped directory

    ```
    set DOTNET_STARTUP_HOOKS=[pathToAgent]\ElasticApmAgentStartupHook.dll
    ```
3. Set any other APM agent configuration using environment variables. For example,

    ```
    set ELASTIC_APM_SERVER_URL=http://localhost:8200
    set ELASTIC_APM_LOG_LEVEL=Trace
    ```
4. Start the sample application with the specified target framework. From the `sample/Elastic.Apm.StartupHook.Sample` directory

    ```
    dotnet run -f net5.0
    ```
5. Observe APM data collected.
