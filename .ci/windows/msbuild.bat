::
:: This script runs the msbuild
::
echo "Prepare context for VsDevCmd.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat"
nuget restore -verbosity detailed -NonInteractive
msbuild
