#
# This script installs the required tools to be used during the msbuild
#

# Install visualstudio2019buildtools
choco install visualstudio2019buildtools -m -y --no-progress --force -r --version=16.8.5.0
if ($LASTEXITCODE -ne 0) {
  Write-Host "visualstudio2019buildtools installation failed."
  exit 1
}
choco install visualstudio2019professional -m -y --no-progress --force -r --version=16.8.5.0 --package-parameters "--includeRecommended --includeOptional"
if ($LASTEXITCODE -ne 0) {
  Write-Host "visualstudio2019professional installation failed."
  exit 1
}
choco install visualstudio2019-workload-netweb -m -y --no-progress --force -r --version=1.0.0
if ($LASTEXITCODE -ne 0) {
  Write-Host "visualstudio2019-workload-netweb installation failed."
  exit 1
}
