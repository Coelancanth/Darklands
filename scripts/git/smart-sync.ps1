#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Intelligent git sync that handles squash merges automatically
.DESCRIPTION
    NOW USES sync-core.psm1 MODULE FOR CONSISTENCY
    Detects if your PR was squash-merged and uses reset instead of rebase.
    For normal situations, uses rebase to maintain clean history.
    Zero friction - just run this instead of manual git commands.
.EXAMPLE
    smart-sync
    Automatically syncs current branch with main using the appropriate strategy
.EXAMPLE
    smart-sync -Check
    Preview what would happen without making changes
.NOTES
    Migrated to use sync-core.psm1: 2025-08-27
#>

param(
    [switch]$Check,      # Preview mode - don't make changes
    [switch]$Verbose     # Show detailed decision logic
)

# Import the core sync module
$modulePath = Join-Path $PSScriptRoot "sync-core.psm1"
Import-Module $modulePath -Force -DisableNameChecking

# Configuration
$mainBranch = "main"
$workBranch = "dev/main"

# Get current branch
$currentBranch = git branch --show-current
Write-Status "Current branch: $currentBranch"

# Show current state if verbose
if ($Verbose) {
    Write-Status "Repository state:"
    $ahead = git rev-list --count "origin/$mainBranch..HEAD" 2>$null
    $behind = git rev-list --count "HEAD..origin/$mainBranch" 2>$null
    Write-Info "  Commits ahead of main: $ahead"
    Write-Info "  Commits behind main: $behind"
    
    if ($currentBranch -eq $workBranch) {
        $isSquash = Test-SquashMerge -Branch $currentBranch -Verbose
        Write-Info "  PR Status: $(if ($isSquash) { 'Recently merged (squash detected)' } else { 'Not merged or old' })"
    }
}

# Execute sync using the module
Write-Status "Starting intelligent sync..."
$success = Sync-GitBranch -Branch $currentBranch -PreviewOnly:$Check -Verbose:$Verbose

if ($success) {
    if (-not $Check) {
        Write-Success "Sync complete! Your branch is up to date."
    } else {
        Write-Success "Preview complete. Run without -Check to apply changes."
    }
    
    # Show final status
    git status --short --branch
} else {
    Write-Error "Sync failed - please review the errors above"
    exit 1
}

# Additional safety check for dev/main branch
if ($currentBranch -eq $workBranch -and -not $Check) {
    # Verify we're aligned with main after sync
    $finalAhead = git rev-list --count "origin/$mainBranch..HEAD" 2>$null
    $finalBehind = git rev-list --count "HEAD..origin/$mainBranch" 2>$null
    
    if ($finalBehind -gt 0 -and $finalAhead -eq 0) {
        Write-Warning "Branch appears to be behind main. You may want to pull latest changes."
    } elseif ($finalAhead -eq 0 -and $finalBehind -eq 0) {
        Write-Info "✨ Perfect sync - dev/main is aligned with main"
    }
}