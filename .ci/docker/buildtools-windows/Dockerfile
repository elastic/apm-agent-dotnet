# escape=`

# Use the latest Windows Server Core image with .NET Framework 4.8.
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-20191008-windowsservercore-ltsc2019

# Restore the default Windows shell for correct batch processing.
SHELL ["cmd", "/S", "/C"]

# Download NuGet v5.4.0 
ADD https://dist.nuget.org/win-x86-commandline/v5.4.0/nuget.exe C:\nuget\nuget.exe

# Add NuGet to Beginning of Path (We want this version of Nuget before any other versions that may be in the path)
RUN setx /M PATH "C:\nuget;%PATH%;"

# We download Visual Studio from Microsoft
# If this fails see the Readme: https://github.com/elastic/apm-agent-dotnet/tree/master/.ci/docker/buildtools-windows
ADD https://download.visualstudio.microsoft.com/download/pr/b79d5d47-f792-4eec-adcf-2da2dfa3e3db/4a244b3dd7c258763614d6e4eb914d42f47917e7cc6a70b509e9d9029aceddf2/vs_Enterprise.exe C:\temp\vs_enterprise.exe

# Install Visual Studio Enterprise with Only packages necessary to build apm-agent-dotnet
RUN C:\temp\vs_enterprise.exe `
                --quiet `
                --wait `
                --norestart `
                --nocache `
                --add Microsoft.Net.Core.Component.SDK.2.1 `
                --add Microsoft.VisualStudio.Component.Web `
                --add Microsoft.Component.MSBuild

# Download .net Core
Add https://dotnet.microsoft.com/download/dotnet-core/thank-you/sdk-3.1.101-windows-x64-installer c:\temp\sdk-3.1.101-windows-x64-installer
RUN C:\temp\sdk-3.1.101-windows-x64-installer `
				--quiet `
				--wait `
				--norestart `
				--nocache `

# Default to PowerShell if no other command specified.
CMD ["powershell.exe", "-NoLogo", "-ExecutionPolicy", "Bypass"]
