# escape=`

# Use the latest Windows Server Core image with .NET Framework 4.8.
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-1909

# Restore the default Windows shell for correct batch processing.
SHELL ["cmd", "/S", "/C"]

# Download NuGet v5.4.0 
ADD https://dist.nuget.org/win-x86-commandline/v5.4.0/nuget.exe C:\nuget\nuget.exe

# Add NuGet to Beginning of Path (We want this version of Nuget before any other versions that may be in the path)
RUN @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "[Environment]::SetEnvironmentVariable(\"Path\", \"C:\nuget;\" + $env:Path, [EnvironmentVariableTarget]::Machine)"

#This route takes 20-30 minutes
# We need a place to download this from adding it to Github and copying locally is a potential option 
#COPY vs_enterprise__833835998.1575916908.exe C:\temp\vs_enterprise__833835998.1575916908.exe
# Install Visual Studio Enterprise with Only packages necessary to build apm-agent-dotnet
#RUN C:\temp\vs_enterprise__833835998.1575916908.exe --quiet --wait --norestart --nocache --add Microsoft.Net.Core.Component.SDK.2.1 --add Microsoft.VisualStudio.Component.Web --add Microsoft.Component.MSBuild

# This route takes about 40-50 minutes
#Install Chocolatey
RUN @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"

#Install Visual Studio From Chocolatey
RUN @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "& choco install visualstudio2019buildtools -m -y --no-progress -r --version=16.4.0.0"
RUN @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "& choco install visualstudio2019enterprise -m -y --no-progress -r --version=16.4.0.0 --package-parameters \"--includeRecommended --includeOptional\""
RUN @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "& choco install visualstudio2019-workload-netweb -m -y --no-progress -r --version=1.0.0"

# Default to PowerShell if no other command specified.
CMD ["powershell.exe", "-NoLogo", "-ExecutionPolicy", "Bypass"]
