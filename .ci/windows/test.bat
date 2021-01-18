::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::
dotnet test -c Release ^
 --verbosity normal ^
 --results-directory target ^
 --diag target\diag.log ^
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose" ^
 /p:CollectCoverage=true ^
 /p:CoverletOutputFormat=cobertura ^
 /p:CoverletOutput=target\Coverage\ ^
 /p:Exclude=\"[System.*]*,[Elastic.Apm.Tests]*,[Elastic.Apm.*.Tests]*,[Elastic.Apm.Tests.*]*,[Elastic.Apm.Benchmarks]*\" ^
 /p:Threshold=0 ^
 /p:ThresholdType=branch ^
 /p:ThresholdStat=total
