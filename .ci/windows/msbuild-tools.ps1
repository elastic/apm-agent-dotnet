#
# This script installs the required tools to be used during the msbuild
#

# Install visualstudio2019buildtools
& choco install visualstudio2017-workload-netcorebuildtools -m -y --no-progress -r --version=1.1.2
& choco install visualstudio2019buildtools -m -y --no-progress -r --version=16.3.10.0
