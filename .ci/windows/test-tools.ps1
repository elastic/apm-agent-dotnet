#
# This script installs the required test tools to be used during the tests execution
#

# Install codecov.tool if not already installed
$tools = dotnet tool list -g
$codecov = $tools -match "codecov.tool"
if (!$codecov) {
    dotnet tool install -g Codecov.Tool --version 1.2.0
}

