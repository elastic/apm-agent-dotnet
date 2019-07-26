::
:: This script runs the msbuild
::
vswhere -products *
vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`

nuget update -self -verbosity detailed
nuget update -MSBuildPath
nuget restore ElasticApmAgent.sln -verbosity detailed
msbuild
