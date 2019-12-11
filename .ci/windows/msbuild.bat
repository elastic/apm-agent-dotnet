::
:: This script runs the msbuild
::

nuget restore -verbosity detailed -NonInteractive
msbuild
