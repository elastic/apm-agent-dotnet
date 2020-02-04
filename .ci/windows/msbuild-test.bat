nuget install dotnet-xunit-to-junit -Source c:\packages
nuget install Codecov.Tool -Source c:\packages

Get-ChildItem -Path . -Recurse -Filter *.csproj |
Foreach-Object {
  & nuget install XunitXml.TestLogger -ProjectName $_.FullName -Source c:\packages
  & nuget install coverlet.msbuild -ProjectName $_.FullName -Source c:\packages
}
