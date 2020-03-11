::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::

dotnet test test\Elastic.Apm.Tests -f net461 -v n -r target -d target\diag.log --no-build ^
 --logger:"junit;LogFilePath={framework}-{assembly}-test-result.xml;MethodFormat=Class;FailureBodyFormat=Verbose" ^
 /p:CollectCoverage=true ^
 /p:CoverletOutputFormat=cobertura ^
 /p:CoverletOutput=target\Coverage\ ^
 /p:Exclude=\"[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*\" ^
 /p:Threshold=0 ^
 /p:ThresholdType=branch ^
 /p:ThresholdStat=total
