#!/usr/bin/env bash
# shellcheck disable=SC1091
source /usr/local/bin/bash_standard_lib.sh

ARCH=$(uname -m| tr '[:upper:]' '[:lower:]')

if [ "${ARCH}" == "x86_64" ] ; then
    docker build --tag docker.elastic.co/observability-ci/apm-agent-dotnet-sdk-linux:latest .ci/docker/sdk-linux
else
    echo "The existing docker image on ARM is not supported yet."
fi
