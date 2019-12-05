#!/usr/bin/env bash
#
# This script installs the required tools to be used during the build
# NOTE: HOME env variable is declared in the pipeline which it stores the tools
# which are required and downloaded on the fly.
#
set -euxo pipefail

# Install .Net SDK
curl -O https://dot.net/v1/dotnet-install.sh
/bin/bash ./dotnet-install.sh --install-dir ${HOME}/dotnet -version 3.1.100
