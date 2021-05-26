FROM mcr.microsoft.com/dotnet/framework/aspnet

RUN powershell -NoProfile -Command Remove-Item -Recurse C:\\inetpub\wwwroot\*

SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

RUN New-Item -Path "c:\\" -Name "tools" -ItemType "directory"

WORKDIR /tools

RUN "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12"

# Install chocolatey
RUN iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

# Install .NET (Core) SDKs
RUN Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile dotnet-install.ps1 -UseBasicParsing
RUN & ./dotnet-install.ps1 -Channel LTS -InstallDir ./dotnet -Version 5.0.100

# Install NuGet Tool
RUN Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile dotnet\\nuget.exe -UseBasicParsing

# Install 2019 build tools
RUN Invoke-WebRequest -UseBasicParsing "https://download.visualstudio.microsoft.com/download/pr/cb1d5164-e767-4886-8955-2df3a7c816a8/b9ff67da6d68d6a653a612fd401283cc213b4ec4bae349dd3d9199659a7d9354/vs_BuildTools.exe" -OutFile "C:\tools\vs_BuildTools.exe"; \
  Start-Process vs_BuildTools.exe -ArgumentList "--add", "Microsoft.VisualStudio.Component.NuGet", \
                                                "--add", "'Microsoft.VisualStudio.Workload.NetCoreBuildTools;includeRecommended;includeOptional'", \
                                                "--add", "Microsoft.VisualStudio.Workload.MSBuildTools", \
                                                "--add", "'Microsoft.VisualStudio.Workload.WebBuildTools;includeRecommended;includeOptional'", \
                                                "--quiet", "--norestart", "--nocache" -NoNewWindow -Wait;
                                                
ENV PATH="C:\\Windows\\system32;C:\\Windows;C:\\Windows\\System32\\Wbem;C:\\Windows\\System32\\WindowsPowerShell\\v1.0;C:\\Windows\\System32\\OpenSSH;C:\\Users\\ContainerAdministrator\\AppData\\Local\\Microsoft\\WindowsApps;c:\\tools;c:\\tools\\dotnet;C:\\ProgramData\\chocolatey\\bin"


