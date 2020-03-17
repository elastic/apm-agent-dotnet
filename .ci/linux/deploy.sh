#!/usr/bin/env bash
#
# This script deploys to nuget given the API key and URL
#
set -euo pipefail

for nupkg in Elastic.Apm Elastic.Apm.AspNetCore Elastic.Apm.EntityFrameworkCore Elastic.Apm.NetCoreAll Elastic.Apm.EntityFramework6 Elastic.Apm.AspNetFullFramework
do
	nupkg+=".nupkg"
	echo "dotnet nuget push ${nupkg}"
	dotnet nuget push ${nupkg} -k ${1} -s ${2}
done
