# Build automation

The `./build/scripts` directory contains a small F# project that uses
[Bullseye](https://github.com/adamralph/bullseye) and [System.CommandLine](https://github.com/dotnet/command-line-api)
to define a set of build targets for performing common tasks.

The project root contains `build.bat` and `build.sh` that make it simple to run
the scripts project, passing arguments through to invocation. 

The following examples assume running in cmd or PowerShell on Windows.
To run in a Unix shell like Bash, change `.\build.bat` to `./build.sh`.

Usage and command line arguments can be listed with `--help`

```
.\build.bat --help
```

To list the supported targets

```
.\build.bat --list-targets
```

To see the dependencies of a specific target

```
.\build.bat <target> --list-dependencies
```

## Build all projects

```
.\build.bat
```

All build output can be found in `./build/output`, with each project in a directory named after the project.

## Build ElasticApmAgent_\<version\>.zip file

```
.\build.bat agent-zip
```

Builds a versioned zip file containing all the assemblies needed to auto instrument an application using [`DOTNET_STARTUP_HOOKS`](https://github.com/dotnet/runtime/blob/master/docs/design/features/host-startup-hook.md).



## Diff built assemblies

To diff locally built assemblies against the latest relevant released version on Nuget

```
.\build.bat diff
```

This will use the `netstandard2.0` TFM by default, but a TFM can be supplied with 
`--framework` argument.

To run for only certain assemblies, pass the nuget package ids with `--packageids` arguments

```
build.bat diff --packageids Elastic.Apm --packageids Elastic.SqlClient
```