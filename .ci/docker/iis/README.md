This is a Docker container with VisualStudio 2017, MSBuild tools, 
dotnet SDK and an IIS installed. 
This container could be used to build and run the IIS test.
The following commands would build the image and run the tests.

----

**IMPORTANT**: This uses a Windows container, so can only be run with Docker on Windows, switched to use Windows containers

----

```
docker build -t iis .ci\docker\iis
docker run --name iis -v "${PWD}:C:\inetpub\wwwroot" -w C:\inetpub\wwwroot\ iis
docker exec iis cmd /C .ci\windows\msbuild.bat
docker exec iis cmd /C .ci\windows\test-iis.bat
```