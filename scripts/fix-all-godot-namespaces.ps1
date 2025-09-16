#!/usr/bin/env pwsh
# Comprehensive fix for all Godot file namespaces

$ErrorActionPreference = "Stop"

Write-Host "Fixing ALL Godot-related namespace references..." -ForegroundColor Cyan

$replacements = @(
    # Core namespaces migrated to new structure
    @{ Pattern = 'using Darklands\.Core\.Domain'; Replace = 'using Darklands.Domain' },
    @{ Pattern = 'using Darklands\.Core\.Application'; Replace = 'using Darklands.Application' },
    @{ Pattern = 'using Darklands\.Core\.Infrastructure'; Replace = 'using Darklands.Application.Infrastructure' },
    @{ Pattern = 'using Darklands\.Core\.Presentation'; Replace = 'using Darklands.Presentation' },

    # Fully qualified references
    @{ Pattern = 'Darklands\.Core\.Domain\.'; Replace = 'Darklands.Domain.' },
    @{ Pattern = 'Darklands\.Core\.Application\.'; Replace = 'Darklands.Application.' },
    @{ Pattern = 'Darklands\.Core\.Infrastructure\.'; Replace = 'Darklands.Application.Infrastructure.' },
    @{ Pattern = 'Darklands\.Core\.Presentation\.'; Replace = 'Darklands.Presentation.' },

    # Debug namespace moved to Application.Common
    @{ Pattern = 'using Darklands\.Domain\.Debug'; Replace = 'using Darklands.Application.Common' },
    @{ Pattern = 'Darklands\.Domain\.Debug\.'; Replace = 'Darklands.Application.Common.' },

    # Services moved to Application.Services
    @{ Pattern = 'using Darklands\.Domain\.Services'; Replace = 'using Darklands.Application.Services' },
    @{ Pattern = 'Darklands\.Domain\.Services\.'; Replace = 'Darklands.Application.Services.' }
)

# Get all C# files NOT in src/ or tests/ directories
$files = Get-ChildItem -Path "." -Filter "*.cs" -Recurse | Where-Object {
    $_.FullName -notlike "*\src\*" -and
    $_.FullName -notlike "*\tests\*" -and
    $_.FullName -notlike "*\bin\*" -and
    $_.FullName -notlike "*\obj\*"
}

$filesModified = 0
$totalFiles = $files.Count

Write-Host "Found $totalFiles files to process..." -ForegroundColor Gray

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    foreach ($replacement in $replacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replace
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
        Write-Host "  Modified: $relativePath" -ForegroundColor Green
        $filesModified++
    }
}

Write-Host "`nGodot namespace fix complete!" -ForegroundColor Green
Write-Host "Files scanned: $totalFiles" -ForegroundColor Cyan
Write-Host "Files modified: $filesModified" -ForegroundColor Cyan