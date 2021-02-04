#
# This script installs the required test tools to be used during the tests execution
#

dotnet tool install -g Codecov.Tool --version 1.2.0
if ($LASTEXITCODE) {
    Write-Host "codecov.tool installation failed."
    exit 1
}

