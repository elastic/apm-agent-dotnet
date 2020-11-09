#
# This script installs the required tools to be used during the msbuild
#

# Install visualstudio2019buildtools
& choco install visualstudio2019buildtools-preview -m -y --no-progress --force -r --version=16.8.0.60000-preview1
if ($? -ne 0) {
  Write-Host "visualstudio2019buildtools-preview installation failed. See the logs."
  exit 1
}
& choco install visualstudio2019professional-preview -m -y --no-progress --force -r --version=16.8.0.60000-preview1 --package-parameters "--includeRecommended --includeOptional"
if ($? -ne 0) {
  Write-Host "visualstudio2019professional-preview installation failed. See the logs."
  exit 1
}
& choco install visualstudio2019-workload-netweb -m -y --no-progress --force -r --version=1.0.0
