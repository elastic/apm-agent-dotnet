#
# This script installs the required tools to be used during the msbuild
#

# Install visualstudio2019buildtools
& choco install visualstudio2019buildtools -m -y --no-progress -r --version=16.4.0.0
& choco install visualstudio2019enterprise -m -y --no-progress -r --version=16.4.0.0 --package-parameters "--includeRecommended --includeOptional"
& choco install visualstudio2019-workload-netweb -m -y --no-progress -r --version=1.0.0
& choco install visualstudio2019-workload-webbuildtools -m -y --no-progress -r --version=1.0.0
