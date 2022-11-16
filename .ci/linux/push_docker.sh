#!/usr/bin/env bash

# See full documentation in the "Creating and publishing Docker images" section
# of CONTRIBUTING.md

set -euxo pipefail

# This script is present on workers but may not be present in a development
# environment.

if [ ${WORKSPACE+x} ]  # We are on a CI worker
then
  source /usr/local/bin/bash_standard_lib.sh
fi

readonly RETRIES=3

# This script is intended to work in conjunction with the build_docker
# script. It assumes that build_docker.sh has been run at least once, thereby
# creating a Docker image to push. If this script does not detect an image
# to be uploaded, it will fail.

# This script is intended to be run from a CI job and will not work if run in
# standalone manner unless certain envrionment variables are set.

# 1. Grab the tag we are working with

echo "INFO: Determining latest tag"
if [ ! -z ${TAG_NAME+x} ]
then
  echo "INFO: Detected TAG_NAME variable. Probably a Jenkins instance."
  readonly GIT_TAG_DEFAULT=$(echo $TAG_NAME|sed s/^v//)
else
  echo "INFO: Did not detect TAG_NAME. Examining git log for latest tag"
  readonly GIT_TAG_DEFAULT=$(git describe --abbrev=0|sed s/^v//)
fi

readonly CUR_TAG=${CUR_TAG:-$GIT_TAG_DEFAULT}

# 2. Construct the image:tag that we are working with
# This is roughly <repo>/<namespace>/image
readonly DOCKER_PUSH_IMAGE="docker.elastic.co/observability/apm-agent-dotnet:$CUR_TAG"

# 3. Proceed with pushing to the registry
readonly DOCKER_REGISTRY_URL=`echo $DOCKER_PUSH_IMAGE|cut -f1 -d/`
echo "INFO: Pushing image $DOCKER_PUSH_IMAGE to $DOCKER_REGISTRY_URL"

if [ ${WORKERS+x} ]  # We are on a CI worker
then
  retry $RETRIES docker push $DOCKER_PUSH_IMAGE || echo "Push failed after 5 \
   retries"
else  # We are in a local (non-CI) environment
  docker push $DOCKER_PUSH_IMAGE || echo "You may need to run 'docker login' \
  first and then re-run this script"
fi