#!/usr/bin/env pwsh
# Remove duplicate using statements in test files

$ErrorActionPreference = "Stop"

Write-Host "Removing duplicate using statements..." -ForegroundColor Cyan

$testFiles = Get-ChildItem -Path "tests" -Filter "*.cs" -Recurse

$filesModified = 0

foreach ($file in $testFiles) {
    $lines = Get-Content $file.FullName

    $uniqueUsings = @{}
    $output = @()
    $inUsingBlock = $true

    foreach ($line in $lines) {
        if ($inUsingBlock -and $line -match '^using\s+(.+);$') {
            $usingStatement = $Matches[1]
            if (-not $uniqueUsings.ContainsKey($usingStatement)) {
                $uniqueUsings[$usingStatement] = $true
                $output += $line
            }
            # Skip duplicates
        } elseif ($inUsingBlock -and $line -match '^(namespace|public|internal|//)') {
            $inUsingBlock = $false
            $output += $line
        } else {
            $output += $line
        }
    }

    $newContent = $output -join "`r`n"
    $oldContent = Get-Content $file.FullName -Raw

    if ($newContent -ne $oldContent) {
        Set-Content -Path $file.FullName -Value $newContent -NoNewline
        Write-Host "  Cleaned: $($file.Name)" -ForegroundColor Green
        $filesModified++
    }
}

Write-Host "`nDuplicate using removal complete!" -ForegroundColor Green
Write-Host "Files modified: $filesModified" -ForegroundColor Cyan