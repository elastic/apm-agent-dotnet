# Windows docker build for building the apm-agent-dotnet

This is a Docker container with the VS build tools required to run the
build goals for the windows OS.

## How to use it

```bash
## Build docker image
docker build -t buildtools2019  -m 2GB .ci/docker/buildtools-windows

## Run container to build the apm-agent-dotnet
docker run --rm -ti \
       --name buildtools2019 \
       -v $(pwd):/src \
       -w /src \
       buildtools2019:latest \
       msbuild
```

## Further details

- https://docs.microsoft.com/en-us/visualstudio/install/build-tools-container?view=vs-2019
