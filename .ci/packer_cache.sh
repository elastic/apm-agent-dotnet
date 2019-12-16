#!/usr/bin/env bash
# shellcheck disable=SC1091
source /usr/local/bin/bash_standard_lib.sh

docker build --tag docker.elastic.co/observability-ci/apm-agent-dotnet-sdk-linux:latest .ci/docker/sdk-linux
