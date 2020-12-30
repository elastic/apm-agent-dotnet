#!/usr/bin/env bash
#
# This script runs the benchamarks
#
set -euxo pipefail

cd ./test/Elastic.Apm.Perf.Tests
dotnet run -c Release --filter AspNetCoreLoadTestWithAgent AspNetCoreLoadTestWithoutAgent
dotnet run -c Release --filter *CollectAllMetrics2X* *Simple100Transaction10Spans* *SimpleTransactionsWith*
