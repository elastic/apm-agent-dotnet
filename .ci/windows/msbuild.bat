::
:: This script runs the msbuild
::
nuget
nuget update self Verbosity detailed
nuget
nuget restore ElasticApmAgent.sln Verbosity detailed
msbuild
