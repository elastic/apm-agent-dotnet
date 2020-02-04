Install-Package dotnet-xunit-to-junit -Source c:\packages
Install-Package Codecov.Tool -Source c:\packages

Get-ChildItem -Path . -Recurse -Filter *.csproj |
Foreach-Object {
  & Install-Package XunitXml.TestLogger -ProjectName $_.FullName -Source c:\packages
  & Install-Package coverlet.msbuild -ProjectName $_.FullName -Source c:\packages
}
