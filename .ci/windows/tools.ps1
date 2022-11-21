#
# This script installs the required tools to be used during the build
#

# Install IIS
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools
Add-WindowsFeature NET-Framework-45-ASPNET
Add-WindowsFeature Web-Asp-Net45

# Install .Net SDKs
Write-Host "Install .Net SDKs"
choco install dotnetcore-sdk -m -y --no-progress -r --version 3.1.416
choco install dotnet-sdk -m -y --no-progress -r --version 5.0.100
choco install dotnet-sdk -m -y --no-progress -r --version 6.0.101
choco install dotnet-sdk -m -y --no-progress -r --version 7.0.100

# Install NuGet Tool
choco install nuget.commandline -y --no-progress -r --version 6.0.0

# Install vswhere
choco install vswhere -y --no-progress -r --version 2.8.4

# Install rust
Write-Host "Install rust-ms"
choco install rust-ms -y --no-progress -r --version 1.57.0

# Download and install cargo make
Write-Host "Download cargo-make"
Invoke-WebRequest -UseBasicParsing `
    -Uri "https://github.com/sagiegurari/cargo-make/releases/download/0.35.0/cargo-make-v0.35.0-x86_64-pc-windows-msvc.zip" `
    -OutFile "C:\tools\cargo-make.zip"

Write-Host "Unzip cargo-make"
New-Item -ItemType directory -Path C:\tools\cargo
Expand-Archive -LiteralPath C:\tools\cargo-make.zip -DestinationPath C:\tools\cargo

# Install Azure Functions Core Tools (https://github.com/Azure/azure-functions-core-tools)
choco install azure-functions-core-tools -y --no-progress -r --version 4.0.4829
