::
:: This script runs the msbuild
::
::vswhere -products *
::vswhere -legacy
dotnet\vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`
dotnet\vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath

dotnet\nuget restore ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"
dotnet\nuget update ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath "C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"

"C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\msbuild"
