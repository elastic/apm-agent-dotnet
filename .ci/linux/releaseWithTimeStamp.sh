#!/usr/bin/env bash
#
# This script packes the project as a release without the
# sample projects
#
set -euxo pipefail

# Remove sample projects - and other we don't want to pack
dotnet sln remove test/Elastic.Apm.PerfTests/Elastic.Apm.PerfTests.csproj
dotnet sln remove test/Elastic.Apm.AspNetFullFramework.Tests/Elastic.Apm.AspNetFullFramework.Tests.csproj
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet pack --version-suffix alpha-`date -u +"%Y%m%d-%H%M%S"` -c Release
