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

:: TODO: Test only - building this seems to fail
dotnet sln remove test/Elastic.Apm.StaticImplicitInitialization.Tests/Elastic.Apm.StaticImplicitInitialization.Tests.csproj

::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::
dotnet test -c Release ^
 --verbosity normal ^
 --results-directory target ^
 --diag target\diag.log ^
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose" ^
 --collect:"XPlat Code Coverage" ^
 --settings coverlet.runsettings ^
 --blame-hang ^
 --blame-hang-timeout 10m ^
 /p:CollectCoverage=true ^
 /p:CoverletOutputFormat=cobertura ^
 /p:CoverletOutput=target/Coverage/ ^
 /p:Threshold=0 ^
 /p:ThresholdType=branch ^
 /p:ThresholdStat=total
