::
:: This script runs the msbuild
::
echo "Prepare context for VsDevCmd.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\Tools\VsDevCmd.bat"
nuget restore -verbosity detailed -NonInteractive

rem TODO: Workaround for https://github.com/dotnet/sdk/issues/14497
setx DOTNET_HOST_PATH "%ProgramFiles%\dotnet\dotnet.exe" /M

msbuild
