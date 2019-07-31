This is a Docker container with VisualStudio 2017, MSBuild tools, 
dotnet SDK and an IIS installed. 
This container could be used to build and run the IIS test.
The following commands would build the image and run the tests.

```
docker build -t iss .ci\docker\iis
docker run --name iis -w C:\inetpub\wwwroot\ iis
docker cp . iis:C:\inetpub\wwwroot
docker exec iis .ci\windows\build.bat
docker exec iis .ci\windows\test-iis.bat
```