#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Universal verification script for any subagent work
.DESCRIPTION
    Provides quick verification patterns for different types of subagent work.
    Follows the "Trust but Verify" protocol with 10-second checks.
.EXAMPLE
    ./scripts/verify-subagent.ps1 -Type backlog
    ./scripts/verify-subagent.ps1 -Type general -CheckFiles "*.cs"
    ./scripts/verify-subagent.ps1 -Type output-style
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("backlog", "general", "output-style", "statusline")]
    [string]$Type,
    
    [string]$CheckFiles = "",
    [string]$SearchPattern = "",
    [switch]$Verbose = $false
)

function Write-Check {
    param($Message, $Success = $true)
    $icon = $Success ? "✅" : "❌"
    Write-Host "$icon $Message" -ForegroundColor ($Success ? "Green" : "Red")
}

Write-Host "🔍 Trust but Verify Protocol - $Type Subagent" -ForegroundColor Cyan
Write-Host "⏱️  10-Second Verification Starting..." -ForegroundColor Gray
Write-Host ""

$startTime = Get-Date
$verified = $true

switch ($Type) {
    "backlog" {
        # Quick backlog verification
        $gitCheck = git status --short "Docs/01-Active/Backlog.md" 2>$null
        Write-Check "Backlog.md modified" ($gitCheck -ne $null)
        
        if ($SearchPattern) {
            $found = Select-String -Path "Docs/01-Active/Backlog.md" -Pattern $SearchPattern -Quiet
            Write-Check "Pattern '$SearchPattern' found" $found
            if (-not $found) { $verified = $false }
        }
        
        # Check for common backlog issues
        $content = Get-Content "Docs/01-Active/Backlog.md" -ErrorAction SilentlyContinue
        if ($content) {
            $hasDoubleStatus = ($content | Select-String "Status.*Status" -Quiet)
            if ($hasDoubleStatus) {
                Write-Check "No duplicate status entries" $false
                $verified = $false
            }
        }
    }
    
    "general" {
        # General purpose verification
        if ($CheckFiles) {
            $files = Get-ChildItem -Path . -Filter $CheckFiles -Recurse -ErrorAction SilentlyContinue
            Write-Check "Files matching '$CheckFiles' exist" ($files.Count -gt 0)
            
            if ($Verbose -and $files) {
                Write-Host "  Found: $($files.Count) files" -ForegroundColor Gray
                $files | Select-Object -First 3 | ForEach-Object {
                    Write-Host "  - $($_.FullName)" -ForegroundColor Gray
                }
            }
        }
        
        if ($SearchPattern) {
            $matches = Get-ChildItem -Recurse -Include "*.cs","*.md","*.json" |
                      Select-String -Pattern $SearchPattern -List
            Write-Check "Pattern '$SearchPattern' found in codebase" ($matches.Count -gt 0)
            
            if ($Verbose -and $matches) {
                Write-Host "  Found in: $($matches.Count) files" -ForegroundColor Gray
            }
        }
        
        # Check git status for any changes
        $gitChanges = git status --short 2>$null
        if ($gitChanges) {
            Write-Host "📝 Files changed:" -ForegroundColor Yellow
            $gitChanges | Select-Object -First 5 | ForEach-Object {
                Write-Host "  $_" -ForegroundColor Gray
            }
        }
    }
    
    "output-style" {
        # Output style verification
        Write-Host "🎨 Output Style Verification:" -ForegroundColor Cyan
        Write-Host "  Check if responses show the requested style" -ForegroundColor Gray
        Write-Host "  Look for style-specific markers or formatting" -ForegroundColor Gray
        Write-Host ""
        Write-Check "Manual verification needed for output style" $true
        Write-Host "  Tip: Request a test output to verify style is active" -ForegroundColor Gray
    }
    
    "statusline" {
        # Statusline verification
        Write-Host "📊 Statusline Verification:" -ForegroundColor Cyan
        Write-Host "  Check if statusline displays requested information" -ForegroundColor Gray
        Write-Host "  Verify settings persist across sessions" -ForegroundColor Gray
        Write-Host ""
        Write-Check "Manual verification needed for statusline" $true
        Write-Host "  Tip: Check Claude Code UI for statusline changes" -ForegroundColor Gray
    }
}

$elapsed = (Get-Date) - $startTime
Write-Host ""
Write-Host "⏱️  Verification completed in $($elapsed.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Gray

if ($verified) {
    Write-Host "✨ Verification Passed - Subagent work appears complete" -ForegroundColor Green
} else {
    Write-Host "⚠️  Verification Issues - Review subagent report for discrepancies" -ForegroundColor Yellow
    Write-Host "   Remember: Trust but Verify means catching issues, not blame" -ForegroundColor Gray
}

exit ($verified ? 0 : 1)