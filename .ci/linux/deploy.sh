#!/usr/bin/env bash
#
# This script deploys to nuget given the API key and URL
#
set -euo pipefail

# Packages that can be publicly released. A project may be marked as <IsPackable>true</IsPackable>
# to produce a nuget package for the CI feed, but it may not be ready for official release.
declare -a projectsToPublish=(
"Elastic.Apm"
"Elastic.Apm.AspNetCore"
"Elastic.Apm.EntityFrameworkCore"
"Elastic.Apm.NetCoreAll"
"Elastic.Apm.EntityFramework6"
"Elastic.Apm.AspNetFullFramework"
"Elastic.Apm.SqlClient"
"Elastic.Apm.Elasticsearch"
"Elastic.Apm.Extensions.Hosting"
"Elastic.Apm.GrpcClient"
"Elastic.Apm.Extensions.Logging"
"Elastic.Apm.StackExchange.Redis"
"Elastic.Apm.Azure.ServiceBus")

for project in  "${projectsToPublish[@]}"
do
	for nupkg in $(find ./build/output/_packages -type f -name '*.nupkg')
	do
		pattern=".*${project}[0-9|.]*.nupkg"
		if [[ $nupkg =~ $pattern ]]
		then
			echo "dotnet nuget push ${nupkg}"
			dotnet nuget push ${nupkg} -k ${1} -s ${2} --skip-duplicate
		fi
	done
done
