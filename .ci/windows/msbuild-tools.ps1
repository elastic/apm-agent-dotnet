#
# This script installs the required tools to be used during the msbuild
#

$ErrorActionPreference = "Stop"

# Install visualstudio2019 and workloads needed to build ASP.NET and .NET Core apps
Invoke-WebRequest -UseBasicParsing `
    -Uri "https://download.visualstudio.microsoft.com/download/pr/5a50b8ac-2c22-47f1-ba60-70d4257a78fa/a4dd4b97c2b8f1280a8ce66bf9e7522e93896ba617212e5ca16be5cdf7b17f1c/vs_BuildTools.exe" `
    -OutFile "C:\tools\vs_BuildTools.exe"

Start-Process "C:\tools\vs_BuildTools.exe" -ArgumentList "--add", "Microsoft.VisualStudio.Component.NuGet", `
    "--add", "Microsoft.VisualStudio.Workload.NetCoreBuildTools;includeRecommended;includeOptional", `
    "--add", "Microsoft.VisualStudio.Workload.MSBuildTools", `
    "--add", "Microsoft.VisualStudio.Workload.WebBuildTools;includeRecommended;includeOptional", `
    "--quiet", "--norestart", "--nocache" -NoNewWindow -Wait
