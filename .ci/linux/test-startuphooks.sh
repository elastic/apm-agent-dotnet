#!/usr/bin/env bash
#
# This script runs the tests and stored the test ouptut in a JUnit xml file
# defined in the test_results folder
#
set -euxo pipefail

dotnet test -c Release test/Elastic.Apm.StartupHook.Tests/Elastic.Apm.StartupHook.Tests.csproj --no-build \
 --verbosity normal \
 --results-directory target \
 --diag target/diag-startuphook.log \
 --logger:"junit;LogFilePath=junit-{framework}-{assembly}.xml;MethodFormat=Class;FailureBodyFormat=Verbose"
