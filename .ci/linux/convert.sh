#!/usr/bin/env bash
#
# This script converts the test output generated previously
# NOTE: It does require the name of test output to be called TestResults.xml
#
set -euxo pipefail

env | sort
# Convert xunit files to junit files
# shellcheck disable=SC2044
for i in $(find . -name TestResults.xml)
do
	DIR=$(dirname "$i")
	dotnet xunit-to-junit "$i" "${DIR}/junit-${STAGE_NAME}-testTesults.xml"
	mv "$i" "${DIR}/TestResults-${STAGE_NAME}.xml"
done
