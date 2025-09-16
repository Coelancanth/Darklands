#!/usr/bin/env pwsh
# Fix namespace references in test files for Clean Architecture migration

$ErrorActionPreference = "Stop"

Write-Host "Fixing test namespace references..." -ForegroundColor Cyan

$replacements = @(
    @{ Pattern = 'using Darklands\.Core\.Domain'; Replace = 'using Darklands.Domain' },
    @{ Pattern = 'using Darklands\.Core\.Application'; Replace = 'using Darklands.Application' },
    @{ Pattern = 'using Darklands\.Core\.Infrastructure'; Replace = 'using Darklands.Application' },  # Infrastructure is part of Application
    @{ Pattern = 'using Darklands\.Core\.Presentation'; Replace = 'using Darklands.Presentation' },

    # Fully qualified references
    @{ Pattern = 'Darklands\.Core\.Domain\.'; Replace = 'Darklands.Domain.' },
    @{ Pattern = 'Darklands\.Core\.Application\.'; Replace = 'Darklands.Application.' },
    @{ Pattern = 'Darklands\.Core\.Infrastructure\.'; Replace = 'Darklands.Application.' },
    @{ Pattern = 'Darklands\.Core\.Presentation\.'; Replace = 'Darklands.Presentation.' }
)

$testFiles = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse

$totalFiles = $testFiles.Count
$filesProcessed = 0
$filesModified = 0

foreach ($file in $testFiles) {
    $filesProcessed++
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    foreach ($replacement in $replacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replace
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $filesModified++
        Write-Host "  Modified: $($file.Name)" -ForegroundColor Green
    }

    # Progress indicator
    if ($filesProcessed % 10 -eq 0) {
        Write-Host "  Progress: $filesProcessed/$totalFiles files..." -ForegroundColor Gray
    }
}

Write-Host "`nNamespace fix complete!" -ForegroundColor Green
Write-Host "  Files processed: $totalFiles" -ForegroundColor Cyan
Write-Host "  Files modified: $filesModified" -ForegroundColor Cyan