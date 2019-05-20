#!/usr/bin/env bash
set -euxo pipefail
for nupkg in $(find . -name '*.nupkg')
do
	dotnet nuget push ${nupkg} -k ${1} -s ${2}
done
