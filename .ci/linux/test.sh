#!/usr/bin/env bash
#
# This script runs the tests and stored them in an xml file defined in the
# LogFilePath property
#
set -euxo pipefail

# Remove Full Framework projects
.ci/linux/remove-projects.sh

# Configure the projects for coverage and testing
while IFS= read -r -d '' file
do
	if [[ $file == *"AspNetFullFrameworkSampleApp.csproj"* ]]; then
		continue
	fi
	if [[ $file == *"Elastic.Apm.AspNetFullFramework.csproj"* ]]; then
		continue
	fi
	if [[ $file == *"Elastic.Apm.AspNetFullFramework.Tests.csproj"* ]]; then
		continue
	fi
	dotnet add "$file" package JunitXml.TestLogger --version 2.1.15
done <  <(find . -name '*.csproj' -print0)

# Run tests per project to generate the coverage report individually.
while IFS= read -r -d '' file
do
	projectName=$(basename "$file")
	dotnet test "$file" \
		--verbosity normal \
		--results-directory target \
		--diag "target/diag-${projectName}.log" \
		--logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose" \
		--collect:"XPlat Code Coverage" \
		--settings coverlet.runsettings \
		/p:CollectCoverage=true \
		/p:CoverletOutputFormat=cobertura \
		/p:CoverletOutput=target/Coverage/ \
		/p:Threshold=0 \
		/p:ThresholdType=branch \
		/p:ThresholdStat=total \
		|| echo -e "\033[31;49mTests FAILED\033[0m"

	echo 'Move coverage files if they were generated!'
	if [ -d target ] ; then
		find target -type f -name 'coverage.cobertura.xml' |
		while IFS= read -r fileName; do
			target=$(dirname "$fileName")
			mv "$fileName" "${target}/${projectName}-${fileName##*\/}"
		done
	fi
done <  <(find test -name '*.csproj' -print0)
