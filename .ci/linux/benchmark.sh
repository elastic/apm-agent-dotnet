#!/usr/bin/env bash
#
# This script runs the benchamarks
#
set -euxo pipefail

cd ./test/Elastic.Apm.PerfTests
dotnet run -c Release --filter AspNetCoreLoadTestWithAgent AspNetCoreLoadTestWithoutAgent
