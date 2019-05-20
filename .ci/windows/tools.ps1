# Download .Net SDK installer script
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing ;

# Install .Net SDK'
& ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet

# Install NuGet Tool
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile dotnet\\nuget.exe -UseBasicParsing ;
