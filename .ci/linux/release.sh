#!/usr/bin/env bash
#
# This script packages the project as a release without the
# sample projects.
#
# Parameters:
#   - isVersionSuffixEnabled whether to add the --version-suffix with the
#                            current timestamp. Optional. Default false
#
set -euxo pipefail

VERSION_SUFFIX_ENABLED=${1:-"false"}

# Set the version-suffix flag if required
FLAG=''
if [ "${VERSION_SUFFIX_ENABLED}" = "true" ]; then
    CURRENT_DATE=$(date -u +"%Y%m%d-%H%M%S")
    FLAG="--version-suffix alpha-${CURRENT_DATE}"
fi

# Remove sample projects - and other we don't want to pack
dotnet sln remove test/Elastic.Apm.PerfTests/Elastic.Apm.PerfTests.csproj
dotnet sln remove test/Elastic.Apm.AspNetFullFramework.Tests/Elastic.Apm.AspNetFullFramework.Tests.csproj
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
# shellcheck disable=SC2086
dotnet pack ${FLAG} -c Release
