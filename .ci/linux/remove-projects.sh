#!/usr/bin/env bash
#
# This script will prepare the build and test executions by removing the full framework projects
#
set -euxo pipefail

# Remove Full Framework projects
dotnet sln remove sample/AspNetFullFrameworkSampleApp/AspNetFullFrameworkSampleApp.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj
dotnet sln remove test/Elastic.Apm.AspNetFullFramework.Tests/Elastic.Apm.AspNetFullFramework.Tests.csproj

# Remove Startup hooks test project. tested separately
dotnet sln remove test/Elastic.Apm.StartupHook.Tests/Elastic.Apm.StartupHook.Tests.csproj

# Remove Profiler test project. tested separately
dotnet sln remove test/Elastic.Apm.Profiler.Managed.Tests/Elastic.Apm.Profiler.Managed.Tests.csproj
