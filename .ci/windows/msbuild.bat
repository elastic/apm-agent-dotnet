::
:: This script runs the msbuild
::
::vswhere -products *
::vswhere -legacy
::vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`
::vswhere -latest -requires Microsoft.Component.MSBuild -property installationPath

set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"
nuget restore ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath %MSBUILD_PATH%
rem ### It seems that the root cause for Full .NET Framework sample application (AspNetFullFrameworkSampleApp)
rem ### failing at runtime because of missing dependencies is nuget update below.
rem ### AspNetFullFrameworkSampleApp has bindingRedirect-s in Web.config and they fail when exact versions
rem ### at runtime don't match version specified in bindingRedirect-s in Web.config.
rem ### nuget update ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath %MSBUILD_PATH%

%MSBUILD_PATH%\msbuild
