#
# This script installs the required test tools to be used during the tests execution
#

# Install Codecov.tool. Redirect stderr to stdout so that already installed codecov.tool does not cause
# Jenkins to abort.
dotnet tool install -g Codecov.Tool --version 1.2.0 2>&1

