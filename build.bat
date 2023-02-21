@echo Off
SET PATH=%PATH%;C:\tools\cargo
dotnet run --project build\scripts -- %*
