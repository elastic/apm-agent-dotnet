#!/usr/bin/env bash
#
# This script runs the dotnet build without the sample projects
#
set -euxo pipefail

# Remove Full Framework projects
.ci/linux/remove-projects.sh

dotnet build
