#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ⚡ Quick Tests - Fast Feedback Loop
    
.DESCRIPTION
    Runs quick/unit tests for ultra-fast feedback.
    Perfect for pre-commit hooks and rapid iteration.
    
.EXAMPLE
    ./scripts/test/quick.ps1
    
.NOTES
    Customize the test filter and commands for your project
    Time Target: <5 seconds
#>

param(
    [switch]$Silent = $false  # Suppress progress messages for hook usage
)

$ErrorActionPreference = "Stop"
$sw = [System.Diagnostics.Stopwatch]::StartNew()

# Progress indicator
if (-not $Silent) {
    Write-Host "⚡ Running Quick Tests..." -ForegroundColor Cyan
}

# TODO: CUSTOMIZE FOR YOUR PROJECT
# Example for .NET: Category=Quick or Category=Unit
# Example for Node: npm test -- --testNamePattern="unit"
# Example for Python: pytest -m "quick"
$testArgs = @(
    "test"
    "YourProject.sln"  # TODO: Update with your project file
    "--filter"
    "Category=Quick"  # TODO: Update with your quick test category
    "--configuration"
    "Debug"
    "--verbosity"
    "minimal"
    "--no-build"  # Assume build already done
    "--nologo"
)

# Execute tests
$testResult = & dotnet $testArgs 2>&1
$exitCode = $LASTEXITCODE

# Parse results - Handle different output formats
$passed = 0
$failed = 0
$skipped = 0

foreach ($line in $testResult) {
    # xUnit summary format
    if ($line -match "Passed:\s+(\d+)") { $passed = [int]$matches[1] }
    if ($line -match "Failed:\s+(\d+)") { $failed = [int]$matches[1] }
    if ($line -match "Skipped:\s+(\d+)") { $skipped = [int]$matches[1] }
    
    # VSTest summary format
    if ($line -match "Total tests:\s+(\d+)") {
        # Parse subsequent lines for passed/failed
        continue
    }
    if ($line -match "^\s+Passed:\s+(\d+)") { $passed = [int]$matches[1] }
    if ($line -match "^\s+Failed:\s+(\d+)") { $failed = [int]$matches[1] }
    if ($line -match "^\s+Skipped:\s+(\d+)") { $skipped = [int]$matches[1] }
    
    # Show output only if there are failures or verbose mode
    if ($failed -gt 0 -or $VerbosePreference -eq 'Continue') {
        Write-Host $line
    }
}

$sw.Stop()
$duration = [math]::Round($sw.Elapsed.TotalSeconds, 1)

# Results summary
if (-not $Silent) {
    Write-Host ""
    if ($failed -eq 0) {
        Write-Host "✅ Quick Tests Passed!" -ForegroundColor Green
        Write-Host "   $passed tests in ${duration}s" -ForegroundColor Gray
        
        if ($duration -gt 5) {
            Write-Host "   ⚠️ Tests took >5s (target: <5s)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "❌ Quick Tests Failed!" -ForegroundColor Red
        Write-Host "   Failed: $failed | Passed: $passed | Duration: ${duration}s" -ForegroundColor Gray
        Write-Host ""
        Write-Host "💡 Run with -Verbose for detailed output" -ForegroundColor Yellow
    }
}

# Exit with test result code
exit $exitCode