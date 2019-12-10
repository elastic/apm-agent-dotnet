#
# This script installs the required test tools to be used during the tests execution
#

# Install tools
& dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
& dotnet tool install -g Codecov.Tool --version 1.2.0

Get-ChildItem -Path . -Recurse -Filter *.csproj |
Foreach-Object {
  & dotnet add $_.FullName package XunitXml.TestLogger --version 2.0.0
  & dotnet add $_.FullName package coverlet.msbuild --version 2.5.1
}
