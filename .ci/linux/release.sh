#!/usr/bin/env bash
#
# This script packes the project as a release without the
# sample projects
#
set -euxo pipefail

# For debugging purposes
env | sort
ls -l ${DOTNET_ROOT}
find ${DOTNET_ROOT} -type f -ls

# Remove sample projects
dotnet sln remove test/Elastic.Apm.PerfTests/Elastic.Apm.PerfTests.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj

dotnet pack -c Release
