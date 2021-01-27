#
# This script installs the required test tools to be used during the tests execution
#

# Install tools
dotnet tool install -g Codecov.Tool --version 1.2.0

# Add coverlet.msbuild only to test projects
Get-ChildItem -Path ./test -Recurse -Filter *.csproj |
Foreach-Object {
  dotnet add $_.FullName package coverlet.msbuild --version 2.9.0
}
