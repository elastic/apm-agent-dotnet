#!/usr/bin/env bash

set -euxo pipefail

source .ci/linux/tools.sh

./build.sh
