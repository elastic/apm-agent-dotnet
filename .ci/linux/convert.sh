#!/usr/bin/env bash
#
# This script converts the test output generated previously
# NOTE: It does require the name of test output to be called TestResults.xml
#
set -euxo pipefail

# Convert xunit files to junit files
for i in $(find . -name TestResults.xml)
do
	DIR=$(dirname "$i")
	dotnet xunit-to-junit "$i" "${DIR}/junit-testTesults.xml"
done
