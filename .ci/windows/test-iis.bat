set ELASTIC_APM_TESTS_FULL_FRAMEWORK_ENABLED=true
nuget restore ElasticApmAgent.sln
msbuild
dotnet test test\Elastic.Apm.AspNetFullFramework.Tests -v n -r target -d target\diag.log --no-build 
