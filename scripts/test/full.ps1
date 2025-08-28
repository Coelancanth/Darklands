#!/usr/bin/env pwsh
<#
.SYNOPSIS
    🧪 Full Test Suite - Staged Execution by Category
    
.DESCRIPTION
    Runs all tests in stages for optimal feedback:
    1. Architecture (fast) - <5s
    2. Unit/Integration - ~20s
    3. Performance/Stress - Variable
    
.EXAMPLE
    ./scripts/test/full.ps1
    ./scripts/test/full.ps1 -SkipSlow
    
.NOTES
    Part of TD_071: Test Categories for Faster Feedback
    Created: 2025-08-24 by DevOps Engineer
#>

param(
    [switch]$SkipSlow = $false,     # Skip Performance/Stress tests
    [switch]$StopOnFailure = $false  # Stop at first category failure
)

$ErrorActionPreference = "Stop"
$overallSw = [System.Diagnostics.Stopwatch]::StartNew()

# Test stages configuration
$stages = @(
    @{
        Name = "Architecture"
        Filter = "Category=Architecture"
        TimeTarget = 5
        Icon = "🏗️"
    },
    @{
        Name = "Core Tests"
        Filter = "Category!=Architecture&Category!=Performance&Category!=ThreadSafety"
        TimeTarget = 30
        Icon = "🧪"
    }
)

# Add slow tests if not skipped
if (-not $SkipSlow) {
    $stages += @{
        Name = "Performance & Stress"
        Filter = "Category=Performance|Category=ThreadSafety"
        TimeTarget = 60
        Icon = "⚡"
    }
}

Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🧪 FULL TEST SUITE - STAGED EXECUTION" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$allPassed = $true
$totalTests = 0
$totalPassed = 0
$totalFailed = 0

foreach ($stage in $stages) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    
    Write-Host "$($stage.Icon) Stage: $($stage.Name)" -ForegroundColor Yellow
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    
    # Run tests for this stage
    $testArgs = @(
        "test"
        "BlockLife.sln"
        "--filter"
        $stage.Filter
        "--configuration"
        "Debug"
        "--verbosity"
        "minimal"
        "--no-build"
        "--nologo"
    )
    
    $testOutput = & dotnet $testArgs 2>&1
    $exitCode = $LASTEXITCODE
    
    # Parse results
    $passed = 0
    $failed = 0
    $skipped = 0
    
    foreach ($line in $testOutput) {
        if ($line -match "Passed:\s+(\d+)") { $passed = [int]$matches[1] }
        if ($line -match "Failed:\s+(\d+)") { $failed = [int]$matches[1] }
        if ($line -match "Skipped:\s+(\d+)") { $skipped = [int]$matches[1] }
        
        # Show failures immediately
        if ($line -match "Failed\s+" -or $line -match "Error\s+") {
            Write-Host "  $line" -ForegroundColor Red
        }
    }
    
    $sw.Stop()
    $duration = [math]::Round($sw.Elapsed.TotalSeconds, 1)
    
    # Update totals
    $totalTests += ($passed + $failed + $skipped)
    $totalPassed += $passed
    $totalFailed += $failed
    
    # Stage results
    if ($failed -eq 0) {
        Write-Host "  ✅ Passed: $passed tests in ${duration}s" -ForegroundColor Green
        
        if ($duration -gt $stage.TimeTarget) {
            Write-Host "  ⚠️ Exceeded target time (${stage.TimeTarget}s)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ❌ Failed: $failed | Passed: $passed | Duration: ${duration}s" -ForegroundColor Red
        $allPassed = $false
        
        if ($StopOnFailure) {
            Write-Host ""
            Write-Host "🛑 Stopping due to failures (use without -StopOnFailure to continue)" -ForegroundColor Red
            break
        }
    }
    
    Write-Host ""
}

$overallSw.Stop()
$totalDuration = [math]::Round($overallSw.Elapsed.TotalSeconds, 1)

# Final summary
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($allPassed) {
    Write-Host "  ✅ ALL TESTS PASSED!" -ForegroundColor Green
} else {
    Write-Host "  ❌ TESTS FAILED!" -ForegroundColor Red
}
Write-Host "════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor White
Write-Host "  Total Tests: $totalTests" -ForegroundColor Gray
Write-Host "  Passed: $totalPassed" -ForegroundColor Green
Write-Host "  Failed: $totalFailed" -ForegroundColor Red
Write-Host "  Duration: ${totalDuration}s" -ForegroundColor Gray
Write-Host ""

# Performance insights
if ($totalDuration -lt 45) {
    Write-Host "🚀 Excellent performance! Tests completed quickly." -ForegroundColor Green
} elseif ($totalDuration -lt 90) {
    Write-Host "⚡ Good performance. Tests within acceptable range." -ForegroundColor Yellow
} else {
    Write-Host "🐌 Tests are taking longer than expected. Consider optimization." -ForegroundColor Yellow
}

# Exit with appropriate code
if ($allPassed) {
    exit 0
} else {
    exit 1
}