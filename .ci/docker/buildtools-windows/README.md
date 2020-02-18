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

### If docker fails while downloading visual studio do the following

- Using Chrome go to <https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=enterprise&rel=16&utm_medium=microsoft&utm_source=docs.microsoft.com&utm_campaign=network+install&utm_content=download+vs2019>
- Open Chrome Dev Tools
- Select Network Tab
- Refresh Page
- Find vs_enterprise.exe in network tab list
- Right click and Copy URL
- Replace URL in dockerfile with the new URL

## Further details

- <https://docs.microsoft.com/en-us/visualstudio/install/build-tools-container?view=vs-2019>
