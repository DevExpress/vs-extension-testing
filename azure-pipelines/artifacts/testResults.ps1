[CmdletBinding()]
Param(
)

$result = @{}

$testRoot = Resolve-Path "$PSScriptRoot\..\..\src\Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests"
$result[$testRoot] = (Get-ChildItem "$testRoot\TestResults" -Recurse -Directory | Get-ChildItem -Recurse -File)

$testRoot = Resolve-Path "$PSScriptRoot\..\..\src\Microsoft.VisualStudio.Extensibility.Testing.Xunit.IntegrationTests"
$result[$testRoot] = (Get-ChildItem "$testRoot\TestResults" -Recurse -Directory | Get-ChildItem -Recurse -File)

$testRoot = Resolve-Path "$PSScriptRoot\..\..\src\Microsoft.VisualStudio.Extensibility.Testing.Xunit.Legacy.IntegrationTests"
$result[$testRoot] = (Get-ChildItem "$testRoot\TestResults" -Recurse -Directory | Get-ChildItem -Recurse -File)

$testlogsPath = "$env:BUILD_ARTIFACTSTAGINGDIRECTORY\test_logs"
if (Test-Path $testlogsPath) {
    $result[$testlogsPath] = Get-ChildItem "$testlogsPath\*";
}

$result
