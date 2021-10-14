#!/usr/bin/env bash
#
# This script runs the tests and stored the test ouptut in a JUnit xml file
# defined in the test_results folder
#
set -euxo pipefail

cargo make test

dotnet test -c Release test/Elastic.Apm.Profiler.Managed.Tests/Elastic.Apm.Profiler.Managed.Tests.csproj \
 --verbosity normal \
 --results-directory test_results \
 --diag test_results/diag.log \
 --logger:"junit;LogFilePath=./../../test_results/junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose" \
 --collect:"XPlat Code Coverage" \
 --settings coverlet.runsettings \
 --blame-hang \
 --blame-hang-timeout 5m \
 /p:CollectCoverage=true \
 /p:CoverletOutputFormat=cobertura \
 /p:CoverletOutput=test_results/Coverage/ \
 /p:Threshold=0 \
 /p:ThresholdType=branch \
 /p:ThresholdStat=total
