#
# This script installs the required tools to be used during the msbuild
#

# Install visualstudio2019buildtools
& choco install visualstudio2019buildtools -m -y --force --no-progress -r
& choco install visualstudio2019enterprise -m -y --force --no-progress -r --package-parameters "--includeRecommended --includeOptional"
& choco install visualstudio2019-workload-netweb -m -y --no-progress -r --version=1.0.0
