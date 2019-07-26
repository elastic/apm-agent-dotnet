::
:: This script runs the msbuild
::
::vswhere -products *
::vswhere -legacy
::vswhere -latest -requires Microsoft.Build.NuGetSdkResolver
::vswhere -latest -requires Microsoft.Component.MSBuild -find **MSBuild\**\Bin\MSBuild.exe`

::nuget restore ElasticApmAgent.sln -verbosity detailed -NonInteractive
nuget restore ElasticApmAgent.sln -MSBuildPath $(Split-Path -Path (get-command msbuild.exe).source -Parent)

nuget update ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath $(Split-Path -Path (get-command msbuild.exe).source -Parent)

"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\msbuild"
