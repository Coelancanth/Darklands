#!/usr/bin/env pwsh
# Fix Infrastructure references to use Darklands.Infrastructure namespace

$ErrorActionPreference = "Stop"

Write-Host "Fixing Infrastructure references in tests..." -ForegroundColor Cyan

# Simple replacement - Infrastructure is a top-level namespace compiled in Application project
$testFiles = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse

$replacements = @(
    # Fix the namespace references
    @{ Pattern = 'using Darklands\.Infrastructure\.DependencyInjection'; Replace = 'using Darklands.Infrastructure.DependencyInjection' },
    @{ Pattern = 'using Darklands\.Infrastructure\.Identity'; Replace = 'using Darklands.Infrastructure.Identity' },
    @{ Pattern = 'using Darklands\.Infrastructure\.Validation'; Replace = 'using Darklands.Infrastructure.Validation' },
    @{ Pattern = 'using Darklands\.Infrastructure\.Services'; Replace = 'using Darklands.Infrastructure.Services' },
    @{ Pattern = 'using Darklands\.Infrastructure\.Events'; Replace = 'using Darklands.Infrastructure.Events' },
    @{ Pattern = 'using Darklands\.Infrastructure\.Vision'; Replace = 'using Darklands.Infrastructure.Vision' }
)

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    # These should already exist, we just ensure they're correct
    foreach ($replacement in $replacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replace
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Modified: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "Infrastructure refs fix complete!" -ForegroundColor Green