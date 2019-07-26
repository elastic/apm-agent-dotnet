#
# This script installs the required tools to be used during the build
#

# Download .Net SDK installer script
#[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
#Invoke-RestMethod "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 ;

# Install .Net SDK'
#& ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet -Version 2.1.505

# Install NuGet Tool
#[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
#Invoke-RestMethod "https://dist.nuget.org/win-x86-commandline/v4.9.4/nuget.exe" -OutFile dotnet\\nuget.exe ;

#& ./dotnet/nuget help

# Install IIS
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools ;

# Install vswhere
#[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
#Invoke-RestMethod "https://github.com/microsoft/vswhere/releases/download/2.6.13%2Ba6d40ba5f4/vswhere.exe" -OutFile dotnet\\vswhere.exe ;

#& choco install microsoft-build-tools -y --no-progress -r 
& choco install nugetpackageexplorer -y --no-progress -r
& choco install dotnetcore-sdk -y --no-progress -r --version 2.1.801
& choco install nuget.commandline -y --no-progress -r --version 4.9.4
& choco install vswhere -y --no-progress -r 