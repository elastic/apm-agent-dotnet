::
:: This script runs the msbuild
::
nuget restore ElasticApmAgent.sln
nuget update MSBuildVersion 15.9
msbuild
