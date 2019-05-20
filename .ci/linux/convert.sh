#!/usr/bin/env bash
set -euxo pipefail

# convert xunit files to junit files
for i in $(find . -name TestResult.xml)
do
	DIR=$(dirname "$i")
	dotnet xunit-to-junit "$i" "${DIR}/junit-testTesults.xml"
done
