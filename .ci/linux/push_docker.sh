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

# Grab the tag we are working with

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
readonly DOCKER_REGISTRY_URL="docker.elastic.co"
readonly DOCKER_IMAGE_NAME="observability/apm-agent-dotnet"
readonly DOCKER_PUSH_IMAGE="$DOCKER_REGISTRY_URL/$DOCKER_IMAGE_NAME:$CUR_TAG"
readonly DOCKER_PUSH_IMAGE_LATEST="$DOCKER_REGISTRY_URL/$DOCKER_IMAGE_NAME:latest"

# Proceed with pushing to the registry
echo "INFO: Pushing image $DOCKER_PUSH_IMAGE to $DOCKER_REGISTRY_URL"

if [ ${WORKERS+x} ]  # We are on a CI worker
then
  retry $RETRIES docker push $DOCKER_PUSH_IMAGE || echo "Push failed after $RETRIES retries"
else  # We are in a local (non-CI) environment
  docker push $DOCKER_PUSH_IMAGE || echo "You may need to run 'docker login' first and then re-run this script"
fi

readonly LATEST_TAG=$(git tag --list --sort=version:refname "v*" | grep -v RC | sed s/^v// | tail -n 1)

if [ "$CUR_TAG" = "$LATEST_TAG" ]
then
  echo "INFO: Current version ($CUR_TAG) is the latest version. Tagging and pushing $DOCKER_PUSH_IMAGE_LATEST ..."
  docker tag $DOCKER_PUSH_IMAGE $DOCKER_PUSH_IMAGE_LATEST

  if [ ${WORKERS+x} ]  # We are on a CI worker
  then
    retry $RETRIES docker push $DOCKER_PUSH_IMAGE_LATEST || echo "Push failed after $RETRIES retries"
  else  # We are in a local (non-CI) environment
    docker push $DOCKER_PUSH_IMAGE_LATEST || echo "You may need to run 'docker login' first and then re-run this script"
  fi
fi
