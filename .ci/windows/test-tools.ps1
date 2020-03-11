#
# This script installs the required test tools to be used during the tests execution
#

function Exec {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$cmd,
        [string]$errorMessage = ($msgs.error_bad_command -f $cmd)
    )

    try {
        $global:lastexitcode = 0
        & $cmd 2>&1 | %{ "$_" }
        if ($lastexitcode -ne 0) {
            throw $errorMessage
        }
    }
    catch [Exception] {
        throw $_
    }
}

# Install tools
exec { dotnet tool install -g Codecov.Tool --version 1.2.0 }

Get-ChildItem -Path . -Recurse -Filter *.csproj |
Foreach-Object {
	if ($_.FullName -notlike "*AspNetFullFrameworkSampleApp.csproj") {
		exec { dotnet add $_.FullName package JunitXml.TestLogger --version 2.1.15 }
		exec { dotnet add $_.FullName package coverlet.msbuild --version 2.5.1 }
	}
}
