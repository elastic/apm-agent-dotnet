This is a Docker container with the dotnet SDK and the test tools.
The following commands would build the image and run the tests.

```
docker build -t sdk .ci/docker/sdk
docker run --rm -ti --name sdk -v $(pwd):/src -w src sdk /bin/bash
 ./ci/linux/build.sh
 ./ci/linux/test.sh
```
