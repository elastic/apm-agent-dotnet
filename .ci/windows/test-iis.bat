set ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED=true

set MSBUILD_PATH="C:\Program Files (x86)\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin"
nuget restore ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath "%MSBUILD_PATH%"
nuget update ElasticApmAgent.sln -verbosity detailed -NonInteractive -MSBuildPath "%MSBUILD_PATH%"

"%MSBUILD_PATH%\msbuild"
dotnet test test\Elastic.Apm.AspNetFullFramework.Tests -v n -r target -d target\diag.log --no-build 
