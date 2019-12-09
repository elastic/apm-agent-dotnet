#
# This script installs the required tools to be used during the build
#

# Install .Net SDKs
& Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile 'dotnet-install.ps1'
& ./dotnet-install.ps1 --install-dir $Env:DOTNET_ROOT -version 2.1.505
