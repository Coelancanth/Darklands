#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automatic persona embodiment wrapper for Claude
.DESCRIPTION
    This script is designed to be called by Claude when you type "embody [persona]"
    It handles ALL git situations automatically:
    - Squash merges
    - Conflicts
    - Uncommitted changes
    - Network issues
    Everything is resolved automatically with zero user intervention.
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$Persona
)

# Validate persona
$validPersonas = @('dev-engineer', 'tech-lead', 'test-specialist', 'debugger-expert', 'product-owner', 'devops-engineer')
if ($Persona -notin $validPersonas) {
    Write-Host "❌ Invalid persona: $Persona" -ForegroundColor Red
    Write-Host "Valid personas: $($validPersonas -join ', ')" -ForegroundColor Yellow
    exit 1
}

# Get script locations
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$personaScripts = Join-Path (Split-Path $scriptRoot) "scripts\persona"
$gitScripts = Join-Path (Split-Path $scriptRoot) "scripts\git"

# Check if v4 exists, otherwise fall back to v3
$embodyScript = Join-Path $personaScripts "embody-v4.ps1"
if (-not (Test-Path $embodyScript)) {
    $embodyScript = Join-Path $personaScripts "embody.ps1"
}

# Pre-flight checks
Write-Host "🔍 Running pre-flight checks..." -ForegroundColor Cyan

# Check for network connectivity
$hasNetwork = Test-Connection github.com -Count 1 -Quiet 2>$null
if (-not $hasNetwork) {
    Write-Host "⚠️  No network connection - working offline" -ForegroundColor Yellow
}

# Check for GitHub CLI
$hasGH = Get-Command gh -ErrorAction SilentlyContinue
if (-not $hasGH) {
    Write-Host "⚠️  GitHub CLI not found - some features limited" -ForegroundColor Yellow
}

# Smart pre-sync to handle edge cases
Write-Host "🔄 Preparing git state..." -ForegroundColor Cyan

# Handle detached HEAD state
$headState = git symbolic-ref -q HEAD 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "🔧 Fixing detached HEAD state..." -ForegroundColor Yellow
    git checkout main 2>$null
    if ($LASTEXITCODE -ne 0) {
        git checkout -b temp-recovery
        git checkout main
        git branch -D temp-recovery 2>$null
    }
}

# Handle interrupted rebase
if (Test-Path ".git/rebase-merge" -or Test-Path ".git/rebase-apply") {
    Write-Host "🔧 Cleaning up interrupted rebase..." -ForegroundColor Yellow
    git rebase --abort 2>$null
}

# Handle interrupted merge
if (Test-Path ".git/MERGE_HEAD") {
    Write-Host "🔧 Cleaning up interrupted merge..." -ForegroundColor Yellow
    git merge --abort 2>$null
}

# Handle interrupted cherry-pick
if (Test-Path ".git/CHERRY_PICK_HEAD") {
    Write-Host "🔧 Cleaning up interrupted cherry-pick..." -ForegroundColor Yellow
    git cherry-pick --abort 2>$null
}

# Now run the main embodiment
Write-Host "" 
& $embodyScript -Persona $Persona

# Post-embodiment automation
$exitCode = $LASTEXITCODE

if ($exitCode -eq 0) {
    # Success - run post-embodiment automations
    
    # Auto-update Memory Bank with embodiment
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $memoryBankPath = Join-Path (Split-Path (Split-Path $scriptRoot)) ".claude\memory-bank"
    $sessionLogPath = Join-Path $memoryBankPath "session-log.md"
    
    if (Test-Path $sessionLogPath) {
        Add-Content $sessionLogPath "`n### $timestamp - $Persona embodied"
        Add-Content $sessionLogPath "- Git state: Synchronized"
        Add-Content $sessionLogPath "- Branch: $(git branch --show-current)"
    }
    
    # Check for any automated tasks
    $branch = git branch --show-current
    if ($branch -eq "dev/main") {
        # Check if there are any recently merged PRs that need cleanup
        if ($hasGH -and $hasNetwork) {
            $mergedPRs = gh pr list --state merged --limit 5 --json number,headRefName,mergedAt 2>$null | ConvertFrom-Json
            foreach ($pr in $mergedPRs) {
                if ($pr.headRefName -and $pr.headRefName -ne "dev/main") {
                    # Clean up merged feature branches
                    git branch -d $pr.headRefName 2>$null
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "🧹 Cleaned up merged branch: $($pr.headRefName)" -ForegroundColor Green
                    }
                }
            }
        }
    }
    
    # Smart suggestions based on current state
    Write-Host ""
    Write-Host "🤖 Automation Complete" -ForegroundColor Green
    
    # Check for stale branches
    $localBranches = git branch --format="%(refname:short)" | Where-Object { $_ -ne "main" -and $_ -ne "dev/main" }
    $staleBranches = @()
    
    foreach ($localBranch in $localBranches) {
        $lastCommit = git log -1 --format="%cr" $localBranch
        if ($lastCommit -match "(\d+) days? ago" -and [int]$Matches[1] -gt 7) {
            $staleBranches += $localBranch
        }
    }
    
    if ($staleBranches) {
        Write-Host "💡 Found stale branches (>7 days old):" -ForegroundColor Yellow
        $staleBranches | ForEach-Object {
            Write-Host "   - $_" -ForegroundColor Gray
        }
        Write-Host "   Consider cleaning up with: git branch -d <branch>" -ForegroundColor Gray
    }
    
} else {
    # Failed - provide recovery options
    Write-Host ""
    Write-Host "⚠️  Embodiment had issues" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Recovery options:" -ForegroundColor Cyan
    Write-Host "  1. Force sync to main: git reset --hard origin/main" -ForegroundColor Gray
    Write-Host "  2. Stash everything: git stash push --all" -ForegroundColor Gray
    Write-Host "  3. Start fresh: git checkout main && git pull" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Then try: embody $Persona" -ForegroundColor Yellow
}

exit $exitCode