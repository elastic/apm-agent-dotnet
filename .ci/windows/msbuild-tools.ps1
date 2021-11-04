#
# This script installs the required tools to be used during the msbuild
#

$ErrorActionPreference = "Stop"

# Install visualstudio2019 and workloads needed to build ASP.NET and .NET Core apps
Invoke-WebRequest -UseBasicParsing `
    -Uri "https://download.visualstudio.microsoft.com/download/pr/cb1d5164-e767-4886-8955-2df3a7c816a8/b9ff67da6d68d6a653a612fd401283cc213b4ec4bae349dd3d9199659a7d9354/vs_BuildTools.exe" `
    -OutFile "C:\tools\vs_BuildTools.exe"

Start-Process "C:\tools\vs_BuildTools.exe" -ArgumentList "--add", "Microsoft.VisualStudio.Component.NuGet", `
    "--add", "Microsoft.VisualStudio.Workload.NetCoreBuildTools;includeRecommended;includeOptional", `
    "--add", "Microsoft.VisualStudio.Workload.MSBuildTools", `
    "--add", "Microsoft.VisualStudio.Workload.WebBuildTools;includeRecommended;includeOptional", `
    "--quiet", "--norestart", "--nocache" -NoNewWindow -Wait;
