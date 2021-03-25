#
# This script installs the required test tools to be used during the tests execution
#

# Install codecov.tool if not already installed
$tools = dotnet tool list -g
$codecov = $tools -match "codecov.tool"
if (!$codecov) {
    dotnet tool install -g Codecov.Tool --version 1.2.0
}

# Install terraform
choco install terraform -m -y --no-progress --force -r --version=0.14.8
if ($LASTEXITCODE -ne 0) {
    Write-Host "terraform installation failed."
    exit 1
}
