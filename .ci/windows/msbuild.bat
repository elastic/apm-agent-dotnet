::
:: This script runs the msbuild
::
vswhere -products *
vswhere -legacy
vswhere -latest -requires Microsoft.Build.NuGetSdkResolver
vswhere -latest -requires Microsoft.Component.MSBuild -find **MSBuild\**\Bin\MSBuild.exe`

nuget install Microsoft.Build.NuGetSdkResolver -Version 4.9.4-rtm.5839 -Source https://dotnet.myget.org/F/nuget-build/api/v3/index.json
nuget restore ElasticApmAgent.sln -verbosity detailed
nuget update ElasticApmAgent.sln -verbosity detailed -MSBuildPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"

"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\msbuild"
