dotnet test test\Elastic.Apm.StartupHook.Tests --no-build ^
 --verbosity normal ^
 --results-directory target ^
 --diag target\diag-startuphook.log ^
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose"
