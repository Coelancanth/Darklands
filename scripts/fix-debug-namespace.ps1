#!/usr/bin/env pwsh
# Fix Debug namespace references to use Application.Common

$ErrorActionPreference = "Stop"

Write-Host "Fixing Debug namespace references..." -ForegroundColor Cyan

$replacements = @(
    @{ Pattern = 'using Darklands\.Domain\.Debug'; Replace = 'using Darklands.Application.Common' },
    @{ Pattern = 'Darklands\.Domain\.Debug\.'; Replace = 'Darklands.Application.Common.' }
)

# Get all Godot C# files
$files = @()
$files += Get-ChildItem -Path "." -Filter "*.cs" -File | Where-Object {
    $_.DirectoryName -eq (Get-Location).Path
}
$files += Get-ChildItem -Path "Views" -Filter "*.cs" -Recurse

$filesModified = 0

foreach ($file in $files) {
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

Write-Host "`nDebug namespace fix complete!" -ForegroundColor Green
Write-Host "Files modified: $filesModified" -ForegroundColor Cyan