::
:: This script runs the msbuild
::
vswhere -products *
vswhere -latest -requires Microsoft.Component.MSBuild -find **MSBuild\**\Bin\MSBuild.exe`

nuget update -self -verbosity detailed
nuget update ElasticApmAgent.sln -verbosity detailed -MSBuildPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"
nuget restore ElasticApmAgent.sln -verbosity detailed
msbuild
