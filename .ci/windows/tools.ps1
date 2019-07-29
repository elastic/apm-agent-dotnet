
#
# This script installs the required tools to be used during the build
#

# Install IIS
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools ;

# Install .Net SDK' 
& choco install dotnetcore-sdk -y --no-progress -r --version 2.2.401

# Install NuGet Tool
& choco install nuget.commandline -y --no-progress -r --version 5.1.0

# Install vswhere
& choco install vswhere -y --no-progress -r --version 2.7.1