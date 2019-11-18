::
:: This script runs the tests and stored them in an xml file defined in the
:: LogFilePath property
::

SET NUGET_PACKAGES="%HOMEDRIVE%%HOMEPATH%/.nuget/packages"
dotnet test -v n -r target -d target\diag.log --no-build --logger:"xunit;LogFilePath=TestResults.xml" /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=target\Coverage\ /p:Exclude=\"[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*\" /p:Threshold=0 /p:ThresholdType=branch /p:ThresholdStat=total
