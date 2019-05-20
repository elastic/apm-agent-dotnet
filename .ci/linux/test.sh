#!/usr/bin/env bash
set -euxo pipefail

# run tests
dotnet test -v n -r target -d target/diag.log --logger:"xunit;LogFileName=TestResult.xml" --no-build \
/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura \
/p:CoverletOutput=target/Coverage/ \
/p:Exclude='"[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*"' \
/p:Threshold=0 /p:ThresholdType=branch /p:ThresholdStat=total \
|| echo -e "\033[31;49mTests FAILED\033[0m"
