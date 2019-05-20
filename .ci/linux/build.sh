#!/usr/bin/env bash
#
# This script runs the dotnet build without the sample projects
#
set -euxo pipefail

# Remove sample projects
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj

dotnet build
