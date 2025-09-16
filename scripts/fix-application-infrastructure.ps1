#!/usr/bin/env pwsh
# Fix Infrastructure namespace to match actual namespace in source files

$ErrorActionPreference = "Stop"

Write-Host "Fixing Application.Infrastructure namespace references..." -ForegroundColor Cyan

$testFiles = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse

$replacements = @(
    # Infrastructure is under Application.Infrastructure
    @{ Pattern = 'using Darklands\.Infrastructure\.'; Replace = 'using Darklands.Application.Infrastructure.' },
    @{ Pattern = 'Darklands\.Infrastructure\.'; Replace = 'Darklands.Application.Infrastructure.' }
)

$filesModified = 0

foreach ($file in $testFiles) {
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

Write-Host "`nApplication.Infrastructure namespace fix complete!" -ForegroundColor Green
Write-Host "Files modified: $filesModified" -ForegroundColor Cyan