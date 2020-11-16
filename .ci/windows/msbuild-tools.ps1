#
# This script installs the required tools to be used during the msbuild
#

function Write-ChocolateyLogs() {
    $chocoLogs = Get-Content -Raw -Path "C:\ProgramData\chocolatey\logs\chocolatey.log"
    Write-Host $chocoLogs
}

# Install visualstudio2019buildtools
choco install visualstudio2019buildtools -m -y --no-progress --force -r --version=16.8.1
if ($? -ne 0) {
  Write-Host "visualstudio2019buildtools installation failed."
  Write-ChocolateyLogs
  exit 1
}
choco install visualstudio2019professional -m -y --no-progress --force -r --version=16.8.1 --package-parameters "--includeRecommended --includeOptional"
if ($? -ne 0) {
  Write-Host "visualstudio2019professional installation failed."
  Write-ChocolateyLogs
  exit 1
}
choco install visualstudio2019-workload-netweb -m -y --no-progress --force -r --version=1.0.0
if ($? -ne 0) {
  Write-Host "visualstudio2019-workload-netweb installation failed."
  Write-ChocolateyLogs
  exit 1
}
