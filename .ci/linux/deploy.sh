#!/usr/bin/env bash
#
# This script deploys to nuget given the API key and URL
#
set -euxo pipefail

for nupkg in $(find . -type f -not -path '.nuget' -name '*.nupkg')
do
	dotnet nuget push ${nupkg} -k ${1} -s ${2}
done
