[CmdletBinding()]
param(
    [string]$ResourceRoot = (Join-Path $PSScriptRoot '..\src\MediaForge.App\Strings')
)

$files = Get-ChildItem -LiteralPath $ResourceRoot -Recurse -Filter Resources.resw
if ($files.Count -lt 2) {
    throw "Expected at least two localized Resources.resw files under '$ResourceRoot'."
}

$resourceKeys = @{}
foreach ($file in $files) {
    [xml]$document = Get-Content -LiteralPath $file.FullName -Raw -Encoding utf8
    $resourceKeys[$file.FullName] = @($document.root.data | ForEach-Object { $_.name } | Sort-Object -Unique)
}

$baseline = $resourceKeys[$files[0].FullName]
$differences = foreach ($file in $files | Select-Object -Skip 1) {
    $comparison = Compare-Object -ReferenceObject $baseline -DifferenceObject $resourceKeys[$file.FullName]
    if ($comparison) {
        [pscustomobject]@{ File = $file.FullName; Difference = ($comparison | Out-String).Trim() }
    }
}

if ($differences) {
    $differences | Format-List | Out-String | Write-Error
    throw 'Localized resource key sets do not match.'
}

Write-Host "Localization resources verified: $($files.Count) languages, $($baseline.Count) keys."
