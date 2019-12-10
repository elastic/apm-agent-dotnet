#!/usr/bin/env bash
source /usr/local/bin/bash_standard_lib.sh

docker build --tag docker.elastic.co/observability-ci/apm-agent-dotnet-sdk-linux:latest .ci/docker/sdk-linux
