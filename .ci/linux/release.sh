#!/usr/bin/env bash
#
# This script packages the project as a release without the
# sample projects.
#
# Parameters:
#   - isVersionSuffixEnabled whether to add the --canary flag. Optional. Default false
#
set -euxo pipefail

./build.sh pack
