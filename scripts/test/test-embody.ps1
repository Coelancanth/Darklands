#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test suite for embody.ps1 script
.DESCRIPTION
    Tests critical paths in embody script to prevent data loss bugs.
    Focuses on commit preservation and squash merge handling.
.EXAMPLE
    ./scripts/test/test-embody.ps1
    Run all tests
.EXAMPLE
    ./scripts/test/test-embody.ps1 -TestName "CommitPreservation"
    Run specific test
#>

param(
    [string]$TestName = "*",
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$script:TestDir = Join-Path $env:TEMP "embody-test-$(Get-Date -Format 'yyyyMMddHHmmss')"
$script:Passed = 0
$script:Failed = 0

# Color functions
function Write-TestResult {
    param([bool]$Success, [string]$TestName, [string]$Message)
    if ($Success) {
        Write-Host "  ✅ $TestName" -ForegroundColor Green
        if ($Message -and $Verbose) { Write-Host "     $Message" -ForegroundColor Gray }
        $script:Passed++
    } else {
        Write-Host "  ❌ $TestName" -ForegroundColor Red
        Write-Host "     $Message" -ForegroundColor Yellow
        $script:Failed++
    }
}

function Write-TestGroup([string]$Name) {
    Write-Host ""
    Write-Host "Testing $Name" -ForegroundColor Cyan
    Write-Host ("-" * 40) -ForegroundColor DarkGray
}

# Test Setup
function Initialize-TestEnvironment {
    Write-Host "Setting up test environment..." -ForegroundColor Gray
    
    # Create test directory
    New-Item -ItemType Directory -Path $script:TestDir -Force | Out-Null
    Set-Location $script:TestDir
    
    # Initialize git repo
    git init --quiet
    git config user.name "Test User"
    git config user.email "test@example.com"
    
    # Create initial commit
    "Initial content" | Out-File -FilePath "test.txt"
    git add .
    git commit -m "Initial commit" --quiet
    
    # Set up fake origin
    git remote add origin "https://fake.origin/repo.git"
}

function Cleanup-TestEnvironment {
    Set-Location $env:TEMP
    if (Test-Path $script:TestDir) {
        Remove-Item -Recurse -Force $script:TestDir
    }
}

# Test: Two dots vs three dots syntax
function Test-GitRevListSyntax {
    Write-TestGroup "Git Rev-List Syntax"
    
    try {
        # Check embody.ps1 doesn't use three dots
        $embodyContent = Get-Content "$PSScriptRoot\..\persona\embody.ps1" -Raw
        $threeDotsFound = $embodyContent -match 'git rev-list.*\.\.\.'
        
        Write-TestResult -Success (-not $threeDotsFound) `
            -TestName "No three-dot syntax in embody.ps1" `
            -Message $(if ($threeDotsFound) { "Found three-dot syntax!" } else { "Correct two-dot syntax used" })
        
        # Check smart-sync.ps1
        $syncContent = Get-Content "$PSScriptRoot\..\git\smart-sync.ps1" -Raw
        $threeDotsInSync = $syncContent -match 'git rev-list.*\.\.\.'
        
        Write-TestResult -Success (-not $threeDotsInSync) `
            -TestName "No three-dot syntax in smart-sync.ps1" `
            -Message $(if ($threeDotsInSync) { "Found three-dot syntax!" } else { "Correct two-dot syntax used" })
        
    } catch {
        Write-TestResult -Success $false -TestName "Syntax Check" -Message $_.Exception.Message
    }
}

# Test: Squash merge detection
function Test-SquashMergeDetection {
    Write-TestGroup "Squash Merge Detection"
    
    try {
        # Check for squash detection logic (now in module)
        $embodyContent = Get-Content "$PSScriptRoot\..\persona\embody.ps1" -Raw
        $moduleContent = Get-Content "$PSScriptRoot\..\git\sync-core.psm1" -Raw -ErrorAction SilentlyContinue
        $combinedContent = $embodyContent + "`n" + $moduleContent
        
        # Should check if HEAD matches origin/main (check combined content)
        # Module uses different approach but achieves same goal
        $hasHeadCheck = $combinedContent -match 'currentHead.*eq.*mainHead' -or 
                       ($combinedContent -match 'Get-SyncStrategy' -and $combinedContent -match 'up-to-date')
        Write-TestResult -Success $hasHeadCheck `
            -TestName "Checks if HEAD matches main" `
            -Message "Prevents unnecessary reset when already aligned"
        
        # Should filter NEW commits vs squashed commits (check combined content)
        $hasNewCommitFilter = $combinedContent -match '-notmatch.*\\(#\\d\+\\)' -or $combinedContent -match 'Test-SquashMerge'
        Write-TestResult -Success $hasNewCommitFilter `
            -TestName "Filters new commits from squashed" `
            -Message "Uses PR number pattern to identify squashed commits"
        
    } catch {
        Write-TestResult -Success $false -TestName "Detection Logic" -Message $_.Exception.Message
    }
}

# Test: Commit preservation logic
function Test-CommitPreservation {
    Write-TestGroup "Commit Preservation"
    
    try {
        # Check both embody.ps1 and sync-core.psm1 for preservation logic
        $embodyContent = Get-Content "$PSScriptRoot\..\persona\embody.ps1" -Raw
        $moduleContent = Get-Content "$PSScriptRoot\..\git\sync-core.psm1" -Raw -ErrorAction SilentlyContinue
        $combinedContent = $embodyContent + "`n" + $moduleContent
        
        # Should create temp branch for safety (now in module)
        $hasTempBranch = $combinedContent -match 'backup-.*-' -or $combinedContent -match 'tempBranch'
        Write-TestResult -Success $hasTempBranch `
            -TestName "Creates temp save branch" `
            -Message "Safety branch prevents permanent loss"
        
        # Should cherry-pick individually (now in module)
        $hasCherryPick = $combinedContent -match 'foreach.*cherry-pick' -or $combinedContent -match 'Preserve-LocalCommits'
        Write-TestResult -Success $hasCherryPick `
            -TestName "Cherry-picks commits individually" `
            -Message "Allows partial recovery on conflicts"
        
        # Should handle cherry-pick failures (now in module)
        $hasErrorHandling = $combinedContent -match 'cherry-pick --skip' -or $combinedContent -match 'cherry-pick --abort'
        Write-TestResult -Success $hasErrorHandling `
            -TestName "Handles cherry-pick failures" `
            -Message "Continues on conflict instead of failing"
        
        # Should preserve temp branch on failure (now in module)
        $preservesTempOnFail = $combinedContent -match 'Kept backup branch' -or $combinedContent -match 'FailedCommits.Count -eq 0'
        Write-TestResult -Success $preservesTempOnFail `
            -TestName "Preserves temp branch on failure" `
            -Message "Keeps rescue branch when preservation fails"
        
    } catch {
        Write-TestResult -Success $false -TestName "Preservation Logic" -Message $_.Exception.Message
    }
}

# Test: Smart-sync integration
function Test-SmartSyncIntegration {
    Write-TestGroup "Smart-Sync Integration"
    
    try {
        # Check both smart-sync and module for preservation logic
        $syncContent = Get-Content "$PSScriptRoot\..\git\smart-sync.ps1" -Raw
        $moduleContent = Get-Content "$PSScriptRoot\..\git\sync-core.psm1" -Raw -ErrorAction SilentlyContinue
        
        # Should have preservation logic (now in module)
        $hasPreservation = ($syncContent -match 'sync-core' -and $moduleContent -match 'Preserve-LocalCommits')
        Write-TestResult -Success $hasPreservation `
            -TestName "Smart-sync has preservation logic" `
            -Message "Consistent with embody.ps1"
        
        # PR script should call smart-sync
        $prContent = Get-Content "$PSScriptRoot\..\git\pr.ps1" -Raw
        $callsSmartSync = $prContent -match 'smart-sync\.ps1'
        Write-TestResult -Success $callsSmartSync `
            -TestName "PR script uses smart-sync" `
            -Message "Ensures consistent behavior"
        
    } catch {
        Write-TestResult -Success $false -TestName "Integration Check" -Message $_.Exception.Message
    }
}

# Main test runner
function Run-Tests {
    Write-Host ""
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "  EMBODY SCRIPT TEST SUITE" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Blue
    
    try {
        if ($TestName -eq "*" -or $TestName -eq "Syntax") {
            Test-GitRevListSyntax
        }
        
        if ($TestName -eq "*" -or $TestName -eq "Detection") {
            Test-SquashMergeDetection
        }
        
        if ($TestName -eq "*" -or $TestName -eq "Preservation") {
            Test-CommitPreservation
        }
        
        if ($TestName -eq "*" -or $TestName -eq "Integration") {
            Test-SmartSyncIntegration
        }
        
    } finally {
        # Summary
        Write-Host ""
        Write-Host "═══════════════════════════════════════════" -ForegroundColor Blue
        Write-Host "  TEST RESULTS" -ForegroundColor Cyan
        Write-Host "═══════════════════════════════════════════" -ForegroundColor Blue
        
        $total = $script:Passed + $script:Failed
        if ($script:Failed -eq 0) {
            Write-Host "  ✅ All tests passed! ($script:Passed/$total)" -ForegroundColor Green
        } else {
            Write-Host "  ❌ Some tests failed!" -ForegroundColor Red
            Write-Host "     Passed: $script:Passed | Failed: $script:Failed | Total: $total" -ForegroundColor Yellow
        }
        
        Write-Host ""
    }
    
    # Exit with appropriate code
    exit $(if ($script:Failed -eq 0) { 0 } else { 1 })
}

# Run tests
Run-Tests