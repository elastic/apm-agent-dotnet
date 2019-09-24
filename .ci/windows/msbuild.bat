::
:: This script runs the msbuild
::
::vswhere -products *
::vswhere -legacy
::vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`
::vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath

set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"
nuget restore ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath %MSBUILD_PATH%
rem ### nuget update ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath %MSBUILD_PATH%

%MSBUILD_PATH%\msbuild
