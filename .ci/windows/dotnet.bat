::
:: This script runs the dotnet build without the Full Framework projects
::
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj
dotnet sln remove test/Elastic.Apm.AspNetFullFramework.Tests/Elastic.Apm.AspNetFullFramework.Tests.csproj
dotnet sln remove test/Elastic.Apm.SqlClient.Tests/Elastic.Apm.SqlClient.Tests.csproj
dotnet sln remove test/Elastic.Apm.EntityFramework6.Tests/Elastic.Apm.EntityFramework6.Tests.csproj
dotnet sln remove test/Elastic.Apm.MongoDb.Tests/Elastic.Apm.MongoDb.Tests.csproj

:: Remove startup hooks tests, which are tested separately- require agent zip to be built
dotnet sln remove test/Elastic.Apm.StartupHook.Tests/Elastic.Apm.StartupHook.Tests.csproj

:: Remove profiler tests, which are tested separately- require profiler to be built
dotnet sln remove test/Elastic.Apm.Profiler.Managed.Tests/Elastic.Apm.Profiler.Managed.Tests.csproj

dotnet nuget locals all --clear

dotnet nuget add source --name nuget.org https://api.nuget.org/v3/index.json

:: Build solution. Add `--verbosity detailed` for more detailed logs
dotnet build -c Release
