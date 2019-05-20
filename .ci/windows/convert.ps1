#
# This script converts the test output generated previously
# NOTE: it does require the name of test output to be called test_result.xml
#
[System.Environment]::SetEnvironmentVariable("PATH", $Env:Path + ";" + $Env:USERPROFILE + "\\.dotnet\\tools")
Get-ChildItem -Path . -Recurse -Filter test_result.xml |
Foreach-Object {
    & dotnet xunit-to-junit $_.FullName $_.parent.FullName + '\\junit-testTesults.xml'
}
