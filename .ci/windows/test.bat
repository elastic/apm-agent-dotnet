dotnet test -v n -r target -d target\\diag.log --logger:xunit --no-build /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=target\\Coverage\\ /p:Exclude="[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*" /p:Threshold=0 /p:ThresholdType=branch /p:ThresholdStat=total
