#!/usr/bin/env bash
#
# This script converts the test output generated previously
# NOTE: It does require the name of test output to be called *TestResults.xml
#
set -euxo pipefail

# Convert xunit files to junit files
while IFS= read -r -d '' file
do
	DIR=$(dirname "$file")
	projectName=$(basename "$file")
	dotnet xunit-to-junit "$file" "${DIR}/junit-${projectName}-testTesults.xml"
done <  <(find . -name '*TestResults.xml' -print0)
