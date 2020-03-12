#
# This script converts the test output generated previously
# NOTE: It does require the name of test output to be called TestResults.xml
#
[System.Environment]::SetEnvironmentVariable("PATH", $Env:Path + ";" + $Env:USERPROFILE + "\\.dotnet\\tools")
Get-ChildItem -Path . -Recurse -Filter TestResults.xml |
Foreach-Object {
    $junitFile = [IO.Path]::Combine($_.parent.FullName, "junit-" + $Env:STAGE_NAME + "-testTesults.xml")
	& dotnet xunit-to-junit $_.FullName $junitFile
	Move-Item -Path $_.FullName -Destination [IO.Path]::Combine($_.parent.FullName, "TestResults-" + $Env:STAGE_NAME + ".xml")
}
