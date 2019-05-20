#!/usr/bin/env bash
set -euxo pipefail
dotnet sln remove test/Elastic.Apm.PerfTests/Elastic.Apm.PerfTests.csproj
dotnet sln remove src/Elastic.Apm.AspNetFullFramework/Elastic.Apm.AspNetFullFramework.csproj
dotnet pack -c Release
