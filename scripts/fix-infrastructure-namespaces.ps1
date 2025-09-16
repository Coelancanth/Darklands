#!/usr/bin/env pwsh
# Fix specific Infrastructure namespace references

$ErrorActionPreference = "Stop"

Write-Host "Fixing infrastructure namespace references..." -ForegroundColor Cyan

$replacements = @(
    # Infrastructure sub-namespaces that are in Application
    @{ Pattern = 'using Darklands\.Application\.DependencyInjection'; Replace = 'using Darklands.Infrastructure.DependencyInjection' },
    @{ Pattern = 'using Darklands\.Application\.Identity'; Replace = 'using Darklands.Infrastructure.Identity' },
    @{ Pattern = 'using Darklands\.Application\.Validation'; Replace = 'using Darklands.Infrastructure.Validation' },

    # Debug moved to Application.Common
    @{ Pattern = 'using Darklands\.Domain\.Debug'; Replace = 'using Darklands.Application.Common' },

    # Services moved to Application.Services
    @{ Pattern = 'using Darklands\.Domain\.Services'; Replace = 'using Darklands.Application.Services' },

    # Fully qualified references
    @{ Pattern = 'Darklands\.Application\.DependencyInjection\.'; Replace = 'Darklands.Infrastructure.DependencyInjection.' },
    @{ Pattern = 'Darklands\.Application\.Identity\.'; Replace = 'Darklands.Infrastructure.Identity.' },
    @{ Pattern = 'Darklands\.Application\.Validation\.'; Replace = 'Darklands.Infrastructure.Validation.' },
    @{ Pattern = 'Darklands\.Domain\.Debug\.'; Replace = 'Darklands.Application.Common.' },
    @{ Pattern = 'Darklands\.Domain\.Services\.'; Replace = 'Darklands.Application.Services.' }
)

$testFiles = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content

    foreach ($replacement in $replacements) {
        $content = $content -replace $replacement.Pattern, $replacement.Replace
    }

    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        Write-Host "  Modified: $($file.Name)" -ForegroundColor Green
    }
}

Write-Host "`nInfrastructure namespace fix complete!" -ForegroundColor Green