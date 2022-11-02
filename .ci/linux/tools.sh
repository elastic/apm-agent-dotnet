#!/usr/bin/env bash
#
# This script installs tools required for building/testing the APM .NET Agnet
#
set -euxo pipefail

# Install Azure Functions Core Tools (https://github.com/Azure/azure-functions-core-tools)
wget https://github.com/Azure/azure-functions-core-tools/releases/download/4.0.4785/Azure.Functions.Cli.linux-x64.4.0.4785.zip
unzip -d azure-functions-cli Azure.Functions.Cli.linux-x64.4.0.4785.zip
cd azure-functions-cli
chmod +x func
chmod +x gozip
export PATH=`pwd`:$PATH
echo $PATH
cd ..