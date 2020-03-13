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
done

# Run tests per project to generate the coverage report individually.

while IFS= read -r -d '' file
do
	projectName=$(basename "$file")
	dotnet test "$file" \
		--verbosity normal \
		--results-directory target \
		--diag "target/diag-${projectName}.log" \
		--logger:"xunit;LogFilePath=${projectName}-TestResults.xml" \
		--collect:"XPlat Code Coverage" \
		--settings coverlet.runsettings \
		/p:CollectCoverage=true \
		/p:CoverletOutputFormat=cobertura \
		/p:CoverletOutput=target/Coverage/ \
		/p:Threshold=0 \
		/p:ThresholdType=branch \
		/p:ThresholdStat=total \
		|| echo -e "\033[31;49mTests FAILED\033[0m"
	mv target/*/coverage.cobertura.xml "target/${projectName}-coverage.cobertura.xml"
done <  <(find test -name '*.csproj' -print0)
