
#
# This script installs the required tools to be used during the build
#

# Install IIS
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools ;
Add-WindowsFeature NET-Framework-45-ASPNET ;
Add-WindowsFeature Web-Asp-Net45 ;

# Install .Net SDKs
& choco install dotnetcore-sdk -m -y --no-progress -r --version 3.1.100
& choco install dotnetcore-sdk -m -y --no-progress -r --version 2.1.505

# Install NuGet Tool
& choco install nuget.commandline -y --no-progress -r --version 5.1.0

# Install vswhere
& choco install vswhere -y --no-progress -r --version 2.7.1
