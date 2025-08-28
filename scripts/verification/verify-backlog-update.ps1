#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Quick verification script for backlog updates by subagents
.DESCRIPTION
    Performs a 10-second verification that backlog updates actually happened.
    Used by personas after subagent work to verify completion.
.EXAMPLE
    ./scripts/verify-backlog-update.ps1
    ./scripts/verify-backlog-update.ps1 -ItemNumber "TD_041"
#>

param(
    [string]$ItemNumber = "",
    [switch]$Detailed = $false
)

$backlogPath = "Docs/01-Active/Backlog.md"
$result = @{
    Success = $true
    Messages = @()
}

Write-Host "🔍 Verifying Backlog Update..." -ForegroundColor Cyan

# Check 1: Was the file modified?
$gitStatus = git status --short $backlogPath 2>$null
if ($gitStatus) {
    $result.Messages += "✅ Backlog.md was modified"
} else {
    $lastCommit = git log -1 --format="%ar" -- $backlogPath
    $result.Messages += "⚠️  Backlog.md not modified (last change: $lastCommit)"
    $result.Success = $false
}

# Check 2: If item number provided, verify it exists
if ($ItemNumber) {
    $itemFound = Select-String -Path $backlogPath -Pattern $ItemNumber -Quiet
    if ($itemFound) {
        $result.Messages += "✅ Item $ItemNumber found in backlog"
        
        # Get context around the item if detailed mode
        if ($Detailed) {
            $context = Select-String -Path $backlogPath -Pattern $ItemNumber -Context 2,5
            $result.Messages += "📄 Context:"
            $result.Messages += $context.Line
        }
    } else {
        $result.Messages += "❌ Item $ItemNumber NOT found in backlog"
        $result.Success = $false
    }
}

# Check 3: Look for common issues
$content = Get-Content $backlogPath -Raw

# Check for duplicate statuses
$statusPattern = '(?m)^\*\*Status\*\*:'
$statuses = [regex]::Matches($content, $statusPattern)
$duplicateCheck = $statuses | Group-Object Value | Where-Object Count -gt 1
if ($duplicateCheck) {
    $result.Messages += "⚠️  Warning: Possible duplicate status entries detected"
}

# Check for counter updates
$counterPattern = '(?m)^- \*\*Next (\w+)\*\*: (\d+)'
$counters = [regex]::Matches($content, $counterPattern)
foreach ($counter in $counters) {
    $type = $counter.Groups[1].Value
    $num = $counter.Groups[2].Value
    $result.Messages += "📊 $type counter at: $num"
}

# Output results
Write-Host ""
foreach ($msg in $result.Messages) {
    Write-Host $msg
}

Write-Host ""
if ($result.Success) {
    Write-Host "✨ Verification Passed" -ForegroundColor Green
} else {
    Write-Host "⚠️  Verification Issues Found" -ForegroundColor Yellow
    Write-Host "   Review the subagent report and actual changes" -ForegroundColor Gray
}

exit ($result.Success ? 0 : 1)