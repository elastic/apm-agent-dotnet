#!/usr/bin/env bash
#
# This script deploys to nuget given the API key and URL
#
set -euo pipefail

declare -a projectsToPublish=("Elastic.Apm" "Elastic.Apm.AspNetCore" "Elastic.Apm.EntityFrameworkCore" "Elastic.Apm.NetCoreAll" "Elastic.Apm.EntityFramework6" "Elastic.Apm.AspNetFullFramework")

for project in  "${projectsToPublish[@]}"
do
	for nupkg in $(find . -type f -not -path './.nuget/*' -name '*.nupkg')
	do
		pattern=".*${project}[0-9|.]*.nupkg"
		if [[ $nupkg =~ $pattern ]]
		then
			echo "dotnet nuget push ${nupkg}"
			dotnet nuget push ${nupkg} -k ${1} -s ${2}
		fi
	done
done
