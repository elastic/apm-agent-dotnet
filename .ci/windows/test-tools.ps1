#
# This script installs the required test tools to be used during the tests execution
#

## https://stackoverflow.com/questions/2124753/how-can-i-use-powershell-with-the-visual-studio-command-prompt
Write-Host "`nPrepare context for VsDevCmd.bat" -ForegroundColor Yellow
pushd "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\Tools"
cmd /c "VsDevCmd.bat&set" |
foreach {
  if ($_ -match "=") {
    $v = $_.split("="); set-item -force -path "ENV:\$($v[0])"  -value "$($v[1])"
  }
}
popd
Write-Host "`nVisual Studio 2019 Command Prompt variables set." -ForegroundColor Yellow

# Install tools
& dotnet tool install -g dotnet-xunit-to-junit --version 0.3.1
& dotnet tool install -g Codecov.Tool --version 1.2.0

Get-ChildItem -Path . -Recurse -Filter *.csproj | Where-Object { $_.Name -NotMatch "AspNetFullFrameworkSampleApp.csproj" } |
Foreach-Object {
  & dotnet add $_.FullName package XunitXml.TestLogger --version 2.0.0
  & dotnet add $_.FullName package coverlet.msbuild --version 2.5.1
}
