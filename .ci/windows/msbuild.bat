::
:: This script runs the msbuild
::
echo "Prepare context for VsDevCmd.bat"
call "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\VsDevCmd.bat"

dotnet nuget add source --name nuget.org https://api.nuget.org/v3/index.json
nuget restore -verbosity detailed -NonInteractive

msbuild /p:Configuration=Release
