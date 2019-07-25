#
# This script installs the required tools to be used during the build
#

# Download .Net SDK installer script
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing ;

# Install .Net SDK'
& ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet -Version 2.1.505

# Install NuGet Tool
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/v5.1.0/nuget.exe" -OutFile dotnet\\nuget.exe -UseBasicParsing ;

# Install IIS
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools ;
