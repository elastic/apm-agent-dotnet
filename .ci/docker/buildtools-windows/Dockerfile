# escape=`

# Use the latest Windows Server Core image with .NET Framework 4.8.
FROM docker.io/elwpenn/windows-vstudio-msbuild:latest

# Restore the default Windows shell for correct batch processing.
SHELL ["cmd", "/S", "/C"]

# Default to PowerShell if no other command specified.
CMD ["powershell.exe", "-NoLogo", "-ExecutionPolicy", "Bypass"]
