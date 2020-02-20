# escape=`

# Use the latest Windows Server Core image with .NET Framework 4.8.
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-20191008-windowsservercore-ltsc2019

# Restore the default Windows shell for correct batch processing.
SHELL ["cmd", "/S", "/C"]

# Download NuGet v5.4.0 
ADD https://dist.nuget.org/win-x86-commandline/v5.4.0/nuget.exe C:\nuget\nuget.exe

# Download and Install .Net Core2.2
ADD https://dotnetwebsite.azurewebsites.net/download/dotnet-core/scripts/v1/dotnet-install.ps1 C:\temp\dotnet-install.ps1
RUN powershell -File c:\temp\dotnet-install.ps1 -Channel 2.2 -InstallDir C:\DotNetCore22

# Add NuGet to Beginning of Path (We want this version of Nuget before any other versions that may be in the path)
RUN setx /M PATH "C:\nuget;%PATH%;"

# We download Visual Studio from Microsoft
# If this fails see the Readme: https://github.com/elastic/apm-agent-dotnet/tree/master/.ci/docker/buildtools-windows
# VS 2019 Pro
ADD https://download.visualstudio.microsoft.com/download/pr/378e5eb4-c1d7-4c05-8f5f-55678a94e7f4/bace7d50d04acb355cf67ea7bb2ef7da7ceca883d3282f9a6544cb48579cc2a2/vs_Professional.exe C:\temp\vs_Professional.exe

# Install Visual Studio Enterprise with Only packages necessary to build apm-agent-dotnet
RUN C:\temp\vs_Professional.exe `
                --quiet `
                --wait `
                --norestart `
                --nocache `
                --add Microsoft.NetCore.Component.SDK `
                --add Microsoft.Net.Core.Component.SDK.2.1 `
                --add Microsoft.VisualStudio.Component.Web `
                --add Microsoft.Component.MSBuild

# Default to PowerShell if no other command specified.
CMD ["powershell.exe", "-NoLogo", "-ExecutionPolicy", "Bypass"]
