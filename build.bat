@echo Off
REM Use the installed cargo that happened when runnin .ci/windows/tools.ps1
SET PATH=%PATH%;C:\tools\cargo
dotnet run --project build\scripts -- %*
