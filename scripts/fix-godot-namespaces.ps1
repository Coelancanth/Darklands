#!/usr/bin/env pwsh
# Fix namespace references in Godot files

$ErrorActionPreference = "Stop"

Write-Host "Fixing Godot file namespace references..." -ForegroundColor Cyan

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
    @{ Pattern = 'using Darklands\.Core\.Domain\.Debug'; Replace = 'using Darklands.Application.Common' },
    @{ Pattern = 'Darklands\.Core\.Domain\.Debug\.'; Replace = 'Darklands.Application.Common.' }
)

# Get all Godot C# files (root level only, not in src/ or tests/)
$godotFiles = Get-ChildItem -Path "." -Filter "*.cs" -File | Where-Object {
    $_.DirectoryName -eq (Get-Location).Path -or $_.DirectoryName -like "*\Views*"
}

$filesModified = 0

foreach ($file in $godotFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    foreach ($replacement in $replacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replace
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Modified: $($file.Name)" -ForegroundColor Green
        $filesModified++
    }
}

Write-Host "`nGodot namespace fix complete!" -ForegroundColor Green
Write-Host "Files modified: $filesModified" -ForegroundColor Cyan