#!/usr/bin/env bash
#
# This script installs the required tools to run/build/test on linux
#
set -euxo pipefail

mkdir -p "${DOTNET_ROOT}"
# Download .Net SDK installer script
curl -s -O -L https://dot.net/v1/dotnet-install.sh
chmod ugo+rx dotnet-install.sh
# Install .Net SDKs
./dotnet-install.sh --install-dir "${DOTNET_ROOT}" -version '3.1.100'
./dotnet-install.sh --install-dir "${DOTNET_ROOT}" -version '5.0.100'
./dotnet-install.sh --install-dir "${DOTNET_ROOT}" -version '6.0.100'
