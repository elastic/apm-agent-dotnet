@echo Off
<<<<<<< Updated upstream
REM Use the installed cargo that happened when runnin .ci/windows/tools.ps1
SET PATH=%PATH%;C:\tools\cargo
dotnet run --project build\scripts -- %*
=======
dotnet run --project build -- %*
>>>>>>> Stashed changes
