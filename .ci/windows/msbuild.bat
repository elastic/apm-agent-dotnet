::
:: This script runs the msbuild
::
nuget
nuget update -self -verbosity detailed
nuget
nuget restore ElasticApmAgent.sln -verbosity detailed
msbuild
