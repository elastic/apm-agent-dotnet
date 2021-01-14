#
# This script installs the required test tools to be used during the tests execution
#

# Install tools
dotnet tool install -g Codecov.Tool --version 1.2.0

Get-ChildItem -Path . -Recurse -Filter *.csproj |
Foreach-Object {
  dotnet add $_.FullName package JunitXml.TestLogger --version 2.1.78
  dotnet add $_.FullName package coverlet.msbuild --version 2.9.0
}
