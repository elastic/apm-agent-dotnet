::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::
dotnet publish -c Release test\Elastic.Apm.Tests --framework net462 --property:PublishDir=..\..\outtestnet462

dotnet test outtestnet462\Elastic.Apm.Tests.dll ^
 --logger:"junit;LogFilePath=test\junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose" ^
 --blame-hang ^
 --blame-hang-timeout 10m
