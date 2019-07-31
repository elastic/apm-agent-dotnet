FROM mcr.microsoft.com/dotnet/framework/aspnet

RUN powershell -NoProfile -Command Remove-Item -Recurse C:\\inetpub\wwwroot\*

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN New-Item -Path "c:\\" -Name "tools" -ItemType "directory"

WORKDIR /tools

# Download .Net SDK installer script
RUN "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12"
RUN Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing ;

# Install .Net SDK'
RUN & ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet -Version 2.1.505

# Install NuGet Tool
RUN Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/v4.9.4/nuget.exe" -OutFile dotnet\\nuget.exe -UseBasicParsing ;

RUN Invoke-WebRequest -UseBasicParsing "https://download.microsoft.com/download/A/6/3/A637DB94-8BA8-43BB-BA59-A7CF3420CD90/vs_BuildTools.exe" -OutFile "C:\tools\vs_BuildTools.exe"; \
  Start-Process vs_BuildTools.exe -ArgumentList "--add", "Microsoft.VisualStudio.Component.NuGet", \
                                                "--add", "Microsoft.Net.Core.Component.SDK", \
                                                "--add", "Microsoft.VisualStudio.Workload.MSBuildTools", \
                                                "--add", "Microsoft.VisualStudio.Component.WebDeploy ", \
                                                "--add", "Microsoft.VisualStudio.Workload.WebBuildTools", \
                                                "--add", "Microsoft.VisualStudio.Component.NuGet.BuildTools", \
                                                "--quiet", "--norestart", "--nocache" -NoNewWindow -Wait;

ENV PATH="C:\\Windows\\system32;C:\\Windows;C:\\Windows\\System32\\Wbem;C:\\Windows\\System32\\WindowsPowerShell\\v1.0;C:\\Windows\\System32\\OpenSSH;C:\\Users\ContainerAdministrator\\AppData\\Local\\Microsoft\\WindowsApps;c:\\tools;c:\\tools\\dotnet;;C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\BuildTools\\MSBuild\\15.0\\Bin"


