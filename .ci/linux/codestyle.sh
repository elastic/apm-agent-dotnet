#!/usr/bin/env bash
#
# This script installs the required tools to be used to make sure there are no code violations in the source code.
#
set -euxo pipefail

# Install
dotnet tool install -g dotnet-format

# Check
dotnet format --dry-run --check
