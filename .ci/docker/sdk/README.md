# Linux docker build for building and testing the apm-agent-dotnet

This is a Docker container with the dotnet SDK and the test tools required to run the
build and test goals for the linux OS.

## How to use it

```bash
## Build docker image
docker build -t sdk .ci/docker/sdk

## Run container to build the apm-agent-dotnet
docker run --rm -ti \
       --name sdk \
       -v $(pwd):/src \
       -w /src \
       sdk:latest \
       /bin/bash -c '.ci/linux/build.sh'

## Run container to test the apm-agent-dotnet
docker run --rm -ti \
       --name sdk \
       -v $(pwd):/src \
       -w /src \
       sdk:latest \
       /bin/bash -c '.ci/linux/test.sh'
```
