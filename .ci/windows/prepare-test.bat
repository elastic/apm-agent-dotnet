::
:: This script prepares the tests for the msbuild
::
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj
dotnet sln remove test/Elastic.Apm.AspNetFullFramework.Tests/Elastic.Apm.AspNetFullFramework.Tests.csproj
dotnet sln remove test/Elastic.Apm.SqlClient.Tests/Elastic.Apm.SqlClient.Tests.csproj
