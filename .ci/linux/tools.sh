#!/usr/bin/env bash
set -euxo pipefail

# Install .Net SDK
curl -O https://dot.net/v1/dotnet-install.sh
/bin/bash ./dotnet-install.sh --install-dir ${HOME}/dotnet -Channel LTS
