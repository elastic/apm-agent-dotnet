::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::
dotnet publish test\Elastic.Apm.Tests --framework net461 -o outtestnet461


dotnet vstest outtestnet461\Elastic.Apm.Tests.dll ^
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose"
