#
# This script installs the required test tools to be used during the tests execution
#

# Install codecov.tool if not already installed
$tools = dotnet tool list -g
$codecov = $tools -match "codecov.tool"
if (!$codecov) {
    # Fixes https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1101
    # since it's done within the dotnet.bat and this script does not call it.
    dotnet nuget add source --name nuget.org https://api.nuget.org/v3/index.json
    dotnet tool install -g Codecov.Tool --version 1.2.0
}
