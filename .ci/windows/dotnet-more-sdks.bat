:: This script installs the required tools to be used during the build

:: Install .Net SDKs
powershell -Command "& Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile 'C:\dotnet-install.ps1'"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& 'C:\dotnet-install.ps1' --install-dir $Env:DOTNET_ROOT -version 2.1.505"
