param(
    [string]$PortraitsDir = (Join-Path $PSScriptRoot "..\data\official_card_portraits"),
    [string]$OutputPath   = (Join-Path $PSScriptRoot "..\data\official_card_index.json"),
    [string]$Version      = "sts2-official-card-index-v1",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function ConvertTo-PascalCase {
    param([string]$SnakeCase)

    if ([string]::IsNullOrWhiteSpace($SnakeCase)) {
        return ""
    }

    $parts = $SnakeCase -split '_+' | Where-Object { $_ -ne "" }
    $builder = [System.Text.StringBuilder]::new()
    foreach ($part in $parts) {
        [void]$builder.Append($part.Substring(0, 1).ToUpperInvariant())
        if ($part.Length -gt 1) {
            [void]$builder.Append($part.Substring(1))
        }
    }
    return $builder.ToString()
}

$resolvedPortraits = [System.IO.Path]::GetFullPath($PortraitsDir)
$resolvedOutput    = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $resolvedPortraits -PathType Container)) {
    throw "Portraits directory not found: $resolvedPortraits"
}

Write-Host "Scanning: $resolvedPortraits"

$groupDirs = Get-ChildItem -LiteralPath $resolvedPortraits -Directory | Sort-Object Name

$cards = New-Object System.Collections.Generic.List[object]
$seenKeys = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
$groupCounts = [ordered]@{}

foreach ($groupDir in $groupDirs) {
    $group = $groupDir.Name
    $pngFiles = Get-ChildItem -LiteralPath $groupDir.FullName -File -Filter *.png | Sort-Object Name
    $groupCounts[$group] = $pngFiles.Count

    foreach ($png in $pngFiles) {
        $cardId = [System.IO.Path]::GetFileNameWithoutExtension($png.Name).ToLowerInvariant()
        $key = "$group/$cardId"
        if (-not $seenKeys.Add($key)) {
            Write-Warning "Duplicate entry skipped: $key"
            continue
        }

        $cards.Add([pscustomobject][ordered]@{
            cardId        = $cardId
            canonicalName = ConvertTo-PascalCase $cardId
            group         = $group
        })
    }
}

Write-Host ""
Write-Host "Cards per group:"
foreach ($entry in $groupCounts.GetEnumerator()) {
    Write-Host ("  {0,-14} {1}" -f $entry.Key, $entry.Value)
}
Write-Host ("Total: {0}" -f $cards.Count)

if (Test-Path -LiteralPath $resolvedOutput -PathType Leaf) {
    try {
        $existing = Get-Content -LiteralPath $resolvedOutput -Raw | ConvertFrom-Json
        $existingCount = @($existing.cards).Count
        $delta = $cards.Count - $existingCount
        Write-Host ("Existing index: {0} cards (delta: {1:+#;-#;0})" -f $existingCount, $delta)
    }
    catch {
        Write-Warning "Could not read existing index for comparison: $($_.Exception.Message)"
    }
}

$payload = [pscustomobject][ordered]@{
    version = $Version
    cards   = $cards.ToArray()
}

$json = $payload | ConvertTo-Json -Depth 4

if ($DryRun) {
    Write-Host ""
    Write-Host "Dry-run mode. No file written."
    return
}

$outputDir = [System.IO.Path]::GetDirectoryName($resolvedOutput)
if (-not [string]::IsNullOrEmpty($outputDir) -and -not (Test-Path -LiteralPath $outputDir -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Set-Content -LiteralPath $resolvedOutput -Value $json -Encoding UTF8

Write-Host ""
Write-Host "Wrote: $resolvedOutput"
