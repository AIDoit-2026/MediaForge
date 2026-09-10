[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$env:NUGET_PACKAGES = Join-Path $repositoryRoot '.nuget\packages'
$env:NUGET_SCRATCH = Join-Path $repositoryRoot '.nuget\scratch'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $repositoryRoot '.nuget\http-cache'

$appProject = Join-Path $repositoryRoot 'src\MediaForge.App\MediaForge.App.csproj'
$coreProject = Join-Path $repositoryRoot 'src\MediaForge.Core\MediaForge.Core.csproj'
$infrastructureProject = Join-Path $repositoryRoot 'src\MediaForge.Infrastructure\MediaForge.Infrastructure.csproj'
$coreTests = Join-Path $repositoryRoot 'tests\MediaForge.Core.Tests\MediaForge.Core.Tests.csproj'
$integrationTests = Join-Path $repositoryRoot 'tests\MediaForge.IntegrationTests\MediaForge.IntegrationTests.csproj'
$commonArguments = @('-p:Platform=x64', '-p:UseSharedCompilation=false', '-m:1', '/nodeReuse:false')

function Invoke-DotNet {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]] $Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Write-Host 'Restoring dependencies...'
foreach ($project in @($coreProject, $infrastructureProject, $appProject, $coreTests, $integrationTests)) {
    Invoke-DotNet restore $project @commonArguments
}

Write-Host 'Building Release application...'
Invoke-DotNet build $appProject '-c' 'Release' '--no-restore' @commonArguments

Write-Host 'Running Core tests...'
Invoke-DotNet test $coreTests '-c' 'Release' '--no-restore' @commonArguments

Write-Host 'Running integration tests...'
Invoke-DotNet test $integrationTests '-c' 'Release' '--no-restore' @commonArguments

Write-Host 'Publishing framework-dependent x64 portable application...'
Invoke-DotNet publish $appProject '-c' 'Release' '--no-restore' @commonArguments

Write-Host 'Verification succeeded.'
