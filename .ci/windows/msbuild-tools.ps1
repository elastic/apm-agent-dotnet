#
# This script installs the required tools to be used during the msbuild
#

$ErrorActionPreference = "Stop"

# Install visualstudio2019 and workloads needed to build ASP.NET and .NET Core apps
Invoke-WebRequest -UseBasicParsing `
    -Uri "https://download.visualstudio.microsoft.com/download/pr/ce8663b0-08ed-410a-9f5d-4f9469d1b2cb/73271b3d53a4e50e65e2e918a8c1100d2681c17bc418e03513c9f0554609ff8a/vs_BuildTools.exe" `
    -OutFile "C:\tools\vs_BuildTools.exe"

Start-Process "C:\tools\vs_BuildTools.exe" -ArgumentList "--add", "Microsoft.VisualStudio.Component.NuGet", `
    "--add", "Microsoft.VisualStudio.Workload.NetCoreBuildTools;includeRecommended;includeOptional", `
    "--add", "Microsoft.VisualStudio.Workload.MSBuildTools", `
    "--add", "Microsoft.VisualStudio.Workload.WebBuildTools;includeRecommended;includeOptional", `
    "--quiet", "--norestart", "--nocache" -NoNewWindow -Wait;
