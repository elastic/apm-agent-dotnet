#
# This script converts the test output generated previously
# NOTE: It does require the name of test output to be called TestResults.xml
#
[System.Environment]::SetEnvironmentVariable("PATH", $Env:Path + ";" + $Env:USERPROFILE + "\\.dotnet\\tools")
Get-ChildItem -Path . -Recurse -Filter TestResults.xml |
Foreach-Object {
    & dotnet xunit-to-junit $_.FullName $_.parent.FullName + '\\junit-testTesults.xml'
}
