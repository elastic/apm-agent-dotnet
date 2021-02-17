#!/usr/bin/env bash
#
# This script packages the project as a release without the
# sample projects.
#
# Parameters:
#   - isVersionSuffixEnabled whether to add the --canary flag. Optional. Default false
#
set -euxo pipefail

VERSION_SUFFIX_ENABLED=${1:-"false"}

# Set the canary flag if required
FLAG=''
if [ "${VERSION_SUFFIX_ENABLED}" = "true" ]; then
    FLAG='--canary'
fi

./build.sh pack ${FLAG}
