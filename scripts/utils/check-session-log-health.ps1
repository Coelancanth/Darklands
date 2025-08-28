#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Comprehensive health check for Memory Bank session log
.DESCRIPTION
    Performs multiple health checks on the session log:
    - Chronological ordering validation
    - Size limit checking
    - Format consistency
    - Duplicate entry detection
    Can be integrated with embody.ps1 or run standalone.
.PARAMETER QuietMode
    Only output if issues are found
.PARAMETER AutoFix
    Automatically fix issues if found
.EXAMPLE
    ./check-session-log-health.ps1
    Run all health checks
.EXAMPLE
    ./check-session-log-health.ps1 -QuietMode
    Only show output if issues exist
#>

param(
    [switch]$QuietMode,
    [switch]$AutoFix
)

$script:SessionLogPath = Join-Path $PSScriptRoot ".." ".." ".claude" "memory-bank" "session-log.md"
$script:MaxSessionLogLines = 1000
$script:WarningThresholdLines = 800

# Color functions
function Write-Success($message) { 
    if (-not $QuietMode) { Write-Host "✅ $message" -ForegroundColor Green }
}
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Info($message) { 
    if (-not $QuietMode) { Write-Host "ℹ️  $message" -ForegroundColor Gray }
}
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }

$script:IssuesFound = $false

# Check chronological ordering
function Test-ChronologicalOrdering {
    $result = & (Join-Path $PSScriptRoot "fix-session-log-order.ps1") -ValidateOnly
    
    if ($result -match "No chronological issues found") {
        Write-Success "Chronological order: OK"
        return $true
    } else {
        Write-Warning "Chronological order: NEEDS FIX"
        $script:IssuesFound = $true
        
        if ($AutoFix) {
            Write-Info "  Auto-fixing chronological order..."
            & (Join-Path $PSScriptRoot "fix-session-log-order.ps1") | Out-Null
            Write-Success "  Fixed chronological ordering"
        } else {
            Write-Info "  Run: ./scripts/utils/fix-session-log-order.ps1"
        }
        return $false
    }
}

# Check file size
function Test-FileSize {
    if (-not (Test-Path $script:SessionLogPath)) {
        Write-Error "Session log not found!"
        $script:IssuesFound = $true
        return $false
    }
    
    $lines = (Get-Content $script:SessionLogPath | Measure-Object -Line).Lines
    $percentage = [Math]::Round(($lines / $script:MaxSessionLogLines) * 100)
    
    if ($lines -gt $script:MaxSessionLogLines) {
        Write-Error "Size limit: EXCEEDED ($lines lines, $percentage% of max)"
        $script:IssuesFound = $true
        
        if ($AutoFix) {
            Write-Info "  Auto-rotating session log..."
            & (Join-Path $PSScriptRoot "rotate-memory-bank.ps1") -Type session -Force | Out-Null
            Write-Success "  Rotated to archive"
        } else {
            Write-Info "  Run: ./scripts/utils/rotate-memory-bank.ps1 -Type session -Force"
        }
        return $false
    }
    elseif ($lines -gt $script:WarningThresholdLines) {
        Write-Warning "Size limit: APPROACHING ($lines lines, $percentage% of max)"
        Write-Info "  Consider rotation soon"
        return $true
    }
    else {
        Write-Success "Size limit: OK ($lines lines, $percentage% of max)"
        return $true
    }
}

# Check for duplicate timestamps
function Test-DuplicateTimestamps {
    $content = Get-Content $script:SessionLogPath -Raw
    $timestamps = [regex]::Matches($content, '### (\d{1,2}:\d{2}) -') | ForEach-Object { $_.Groups[1].Value }
    
    $duplicates = $timestamps | Group-Object | Where-Object { $_.Count -gt 1 }
    
    if ($duplicates) {
        Write-Warning "Duplicate timestamps: FOUND"
        $script:IssuesFound = $true
        foreach ($dup in $duplicates) {
            Write-Info "  $($dup.Name) appears $($dup.Count) times"
        }
        Write-Info "  Manual review recommended"
        return $false
    } else {
        Write-Success "Duplicate timestamps: NONE"
        return $true
    }
}

# Check entry format consistency
function Test-EntryFormat {
    $content = Get-Content $script:SessionLogPath
    $entryLines = $content | Where-Object { $_ -match '^### \d{1,2}:\d{2} -' }
    
    $invalidFormats = @()
    foreach ($line in $entryLines) {
        if ($line -notmatch '^### \d{1,2}:\d{2} - [A-Z]') {
            $invalidFormats += $line
        }
    }
    
    if ($invalidFormats.Count -gt 0) {
        Write-Warning "Entry format: INCONSISTENT"
        $script:IssuesFound = $true
        Write-Info "  Found $($invalidFormats.Count) entries with format issues"
        return $false
    } else {
        Write-Success "Entry format: CONSISTENT"
        return $true
    }
}

# Main execution
function Main {
    if (-not $QuietMode) {
        Write-Host ""
        Write-Host "🔍 Session Log Health Check" -ForegroundColor Cyan
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    }
    
    # Run all checks
    $checks = @(
        (Test-ChronologicalOrdering),
        (Test-FileSize),
        (Test-DuplicateTimestamps),
        (Test-EntryFormat)
    )
    
    # Summary
    if (-not $QuietMode -or $script:IssuesFound) {
        Write-Host ""
        if ($script:IssuesFound) {
            Write-Warning "Session log has issues that need attention"
            if (-not $AutoFix) {
                Write-Info "Use -AutoFix to automatically resolve fixable issues"
            }
            exit 1
        } else {
            Write-Success "Session log is healthy!"
            exit 0
        }
    }
}

Main