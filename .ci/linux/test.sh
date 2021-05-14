#!/usr/bin/env bash
#
# This script runs the tests and stored them in an xml file defined in the
# LogFilePath property
#
set -euxo pipefail

# Remove Full Framework projects
.ci/linux/remove-projects.sh

# Build agent zip file
./build.sh agent-zip

# Run tests for all solution
dotnet test -c Release test/Elastic.Apm.SqlClient.Tests \
	--filter EfCoreWithMsSqlTests \
	--verbosity normal \
	--results-directory target \
	--diag "target/diag-ElasticApmAgent.log" \
	--logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose" \
	--collect:"XPlat Code Coverage" \
	--settings coverlet.runsettings \
	--blame-hang \
	--blame-hang-timeout 5m \
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
		parent=$(basename "$target")
		mv "$fileName" "${target}/${parent}-${fileName##*\/}"
	done
fi
