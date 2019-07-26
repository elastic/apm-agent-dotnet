#
# This script installs the required tools to be used during the build
#

# Download .Net SDK installer script
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-RestMethod "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 ;

# Install .Net SDK'
& ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet -Version 2.1.505

# Install NuGet Tool
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-RestMethod "https://dist.nuget.org/win-x86-commandline/v4.9.4/nuget.exe" -OutFile dotnet\\nuget.exe ;

& ./dotnet/nuget help

Import-Module PowerShellGet
Register-PSRepository -Name "nuget-build" -SourceLocation "https://dotnet.myget.org/F/nuget-build/api/v2"
Install-Module -Name "Microsoft.Build.NuGetSdkResolver" -RequiredVersion "4.9.4-rtm.5839" -Repository "nuget-build" -AllowPreRelease

# Install IIS
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools ;

# Install vswhere

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-RestMethod "https://github.com/microsoft/vswhere/releases/download/2.6.13%2Ba6d40ba5f4/vswhere.exe" -OutFile dotnet\\vswhere.exe ;
