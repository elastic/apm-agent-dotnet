
# Download .Net SDK installer script
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing ;

# Install .Net SDK'
& ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet

# Install NuGet Tool
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile dotnet\\nuget.exe -UseBasicParsing ;

#
& dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
& dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj

# Install tools
& dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
& dotnet tool install -g Codecov.Tool --version 1.2.0

Get-ChildItem -Path . -Recurse -Filter *.csproj |
Foreach-Object {
  & dotnet add $_.FullName package XunitXml.TestLogger --version 2.0.0
  & dotnet add $_.FullName package coverlet.msbuild --version 2.5.1
}
