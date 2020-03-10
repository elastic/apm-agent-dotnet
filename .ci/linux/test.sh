#!/usr/bin/env bash
#
# This script runs the tests and stored them in an xml file defined in the
# LogFilePath property
#
set -euxo pipefail

# Remove Full Framework projects
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj
dotnet sln remove test/Elastic.Apm.AspNetFullFramework.Tests/Elastic.Apm.AspNetFullFramework.Tests.csproj

# Configure the projects for coverage and testing
for i in $(find . -name '*.csproj')
do
	if [[ $i == *"AspNetFullFrameworkSampleApp.csproj"* ]]; then
		continue
	fi
	if [[ $i == *"Elastic.Apm.AspNetFullFramework.csproj"* ]]; then
		continue
	fi
	if [[ $i == *"Elastic.Apm.AspNetFullFramework.Tests.csproj"* ]]; then
		continue
	fi
	dotnet add "$i" package XunitXml.TestLogger --version 2.0.0
	dotnet add "$i" package coverlet.msbuild --version 2.5.1
done

# Run tests
dotnet test -v n -r target -d target/diag.log \
    --logger:"xunit;LogFilePath=TestResults.xml" \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:CoverletOutput=target/Coverage/ \
    /p:Exclude='"[Elastic.Apm.Tests]*,[SampleAspNetCoreApp*]*,[xunit*]*"' \
    /p:Threshold=0 \
    /p:ThresholdType=branch \
    /p:ThresholdStat=total
