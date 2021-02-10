::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::
dotnet test -c Release --no-build ^
 --verbosity normal ^
 --results-directory target ^
 --diag target\diag.log ^
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose"
