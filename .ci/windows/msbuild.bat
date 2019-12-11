::
:: This script runs the msbuild
::
echo "Prepare context for VsDevCmd.bat"
set
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat"
set
set MSBuildSDKsPath="C:\Program Files\dotnet\sdk\3.1.100\Sdks"
nuget restore -verbosity detailed -NonInteractive
msbuild
