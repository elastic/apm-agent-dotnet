#!/usr/bin/env bash
#
# This script runs the tests and stored the test ouptut in a JUnit xml file
# defined in the test_results folder
#
set -euxo pipefail

cargo make test
