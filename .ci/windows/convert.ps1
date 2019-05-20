[System.Environment]::SetEnvironmentVariable("PATH", $Env:Path + ";" + $Env:USERPROFILE + "\\.dotnet\\tools")
Get-ChildItem -Path . -Recurse -Filter TestResults.xml |
Foreach-Object {
    & dotnet xunit-to-junit $_.FullName $_.parent.FullName + '\\junit-testTesults.xml'
}
