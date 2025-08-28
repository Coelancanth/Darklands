#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Core git sync module - single source of truth for all sync operations
.DESCRIPTION
    Consolidates git sync logic from multiple scripts to prevent bugs and data loss.
    Used by embody.ps1, smart-sync.ps1, and pr.ps1
.NOTES
    CRITICAL: This module handles data preservation. Every function must be defensive.
    Created: 2025-08-27
    Author: DevOps Engineer
#>

# Color output functions for consistent messaging
function Write-Status($message) { Write-Host "🔍 $message" -ForegroundColor Cyan }
function Write-Decision($message) { Write-Host "🎯 $message" -ForegroundColor Yellow }
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Gray }

<#
.SYNOPSIS
    Tests if a squash merge has occurred for the current branch
.DESCRIPTION
    Uses multiple strategies to detect squash merges:
    1. GitHub API check for recently merged PRs
    2. Commit count pattern analysis
    3. Commit message pattern matching
.PARAMETER Branch
    The branch to check (defaults to current)
.PARAMETER Verbose
    Show detailed detection logic
.OUTPUTS
    Boolean indicating if squash merge detected
#>
function Test-SquashMerge {
    param(
        [string]$Branch = (git branch --show-current),
        [switch]$Verbose
    )
    
    # Only check for dev/main branch by default
    if ($Branch -ne "dev/main" -and $Branch -notmatch "^feat/" -and $Branch -notmatch "^fix/") {
        return $false
    }
    
    # Strategy 1: Check GitHub API for merged PRs
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        try {
            $mergedPR = gh pr list --state merged --head $Branch --limit 1 --json number,mergedAt --jq '.[0]' 2>$null
            if ($mergedPR) {
                $prData = $mergedPR | ConvertFrom-Json -ErrorAction SilentlyContinue
                if ($prData -and $prData.mergedAt) {
                    $mergeTime = [DateTime]::Parse($prData.mergedAt)
                    $hourAgo = (Get-Date).AddHours(-1)
                    
                    if ($mergeTime -gt $hourAgo) {
                        if ($Verbose) { Write-Info "GitHub API: Found recently merged PR from $Branch" }
                        return $true
                    }
                }
            }
        } catch {
            if ($Verbose) { Write-Warning "GitHub API check failed: $_" }
        }
    }
    
    # Strategy 2: Commit count pattern analysis
    $localAhead = git rev-list --count "origin/main..HEAD" 2>$null
    $localBehind = git rev-list --count "HEAD..origin/main" 2>$null
    
    if ($localAhead -gt 5 -and $localBehind -eq 1) {
        # Many local commits but only 1 behind suggests squash merge
        $lastMainCommit = git log "origin/main" -1 --pretty=format:"%s" 2>$null
        
        # Check if the main commit looks like a squashed PR
        if ($lastMainCommit -match '\(#\d+\)$') {
            if ($Verbose) { Write-Info "Pattern analysis: Detected squash merge pattern (many->one)" }
            return $true
        }
    }
    
    # Strategy 3: Check for duplicate content in different forms
    if ($localAhead -gt 0) {
        # Get subject of first local commit
        $firstLocalCommit = git log --oneline -1 --skip=$localBehind HEAD 2>$null
        if ($firstLocalCommit) {
            $commitMessage = $firstLocalCommit -replace '^\w+\s+', ''
            
            # Check if this content exists in main
            $mainHasContent = git log "origin/main" --grep="$commitMessage" --oneline 2>$null
            if ($mainHasContent) {
                if ($Verbose) { Write-Info "Content detection: Your commits appear to be in main (squashed)" }
                return $true
            }
        }
    }
    
    return $false
}

<#
.SYNOPSIS
    Determines the appropriate sync strategy for the current git state
.DESCRIPTION
    Analyzes branch state and returns the best strategy to use
.PARAMETER Branch
    Current branch name
.PARAMETER Verbose
    Show detailed decision logic
.OUTPUTS
    Strategy string: "squash-reset", "fast-forward", "smart-rebase", "merge", "up-to-date"
#>
function Get-SyncStrategy {
    param(
        [string]$Branch = (git branch --show-current),
        [switch]$Verbose
    )
    
    # Get current state
    $ahead = git rev-list --count "origin/main..HEAD" 2>$null
    $behind = git rev-list --count "HEAD..origin/main" 2>$null
    
    # Check for squash merge first (highest priority)
    if (Test-SquashMerge -Branch $Branch -Verbose:$Verbose) {
        if ($Verbose) { Write-Info "Strategy: Squash merge detected, will use reset strategy" }
        return "squash-reset"
    }
    
    # Determine strategy based on commit counts
    if ($ahead -eq 0 -and $behind -eq 0) {
        if ($Verbose) { Write-Info "Strategy: Branch is up-to-date" }
        return "up-to-date"
    }
    elseif ($ahead -eq 0 -and $behind -gt 0) {
        if ($Verbose) { Write-Info "Strategy: Fast-forward possible" }
        return "fast-forward"
    }
    elseif ($ahead -gt 0 -and $behind -gt 0) {
        if ($Verbose) { Write-Info "Strategy: Rebase needed (diverged history)" }
        return "smart-rebase"
    }
    elseif ($ahead -gt 0 -and $behind -eq 0) {
        if ($Verbose) { Write-Info "Strategy: Already ahead, no sync needed" }
        return "up-to-date"
    }
    else {
        if ($Verbose) { Write-Warning "Strategy: Complex state, using merge" }
        return "merge"
    }
}

<#
.SYNOPSIS
    Preserves local commits when resetting after a squash merge
.DESCRIPTION
    CRITICAL FUNCTION: Prevents data loss by preserving new commits made after squash merge
.PARAMETER Branch
    Branch to preserve commits from
.OUTPUTS
    PSObject with Success (bool) and PreservedCommits (array)
#>
function Preserve-LocalCommits {
    param(
        [string]$Branch = (git branch --show-current)
    )
    
    $result = @{
        Success = $true
        PreservedCommits = @()
        TempBranch = $null
        FailedCommits = @()
    }
    
    # CRITICAL: Check for new commits made AFTER squash merge
    $localCommits = git rev-list "origin/main..HEAD" 2>$null
    if (-not $localCommits) {
        Write-Info "No local commits to preserve"
        return $result
    }
    
    # Count commits
    $commitCount = ($localCommits | Measure-Object -Line).Lines
    Write-Warning "Found $commitCount local commits that need preservation"
    
    # Create safety backup branch ALWAYS
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $tempBranch = "backup-$Branch-$timestamp"
    git branch $tempBranch HEAD 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Info "Safety backup created: $tempBranch"
        $result.TempBranch = $tempBranch
    } else {
        Write-Error "Failed to create backup branch - aborting for safety"
        $result.Success = $false
        return $result
    }
    
    # Reset to main (safe now that we have backup)
    git reset --hard "origin/main" 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Reset failed - restoring from backup"
        git reset --hard $tempBranch
        git branch -D $tempBranch 2>$null
        $result.Success = $false
        return $result
    }
    
    # Cherry-pick commits from backup
    $commits = git rev-list --reverse "$tempBranch" --not "origin/main" 2>$null
    
    foreach ($commitHash in $commits) {
        if (-not $commitHash) { continue }
        
        $commitInfo = git log -1 --pretty=format:"%h %s" $commitHash 2>$null
        
        # Attempt to cherry-pick
        $output = git cherry-pick $commitHash 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Preserved: $commitInfo"
            $result.PreservedCommits += $commitInfo
        } else {
            if ($output -match "nothing to commit") {
                # Commit was already applied (common with squash merges)
                git cherry-pick --skip 2>$null
                Write-Info "Already applied: $commitInfo"
            } else {
                # Real conflict - record it
                git cherry-pick --abort 2>$null
                Write-Warning "Could not preserve: $commitInfo"
                $result.FailedCommits += $commitInfo
            }
        }
    }
    
    # Clean up temp branch only if all commits were preserved
    if ($result.FailedCommits.Count -eq 0) {
        git branch -D $tempBranch 2>$null
        Write-Info "Cleaned up backup branch"
    } else {
        Write-Warning "Kept backup branch $tempBranch due to failed commits"
        Write-Host "To manually recover, use:" -ForegroundColor Cyan
        Write-Host "  git cherry-pick <commit-hash>" -ForegroundColor Green
        Write-Host "  Get hashes with: git log --oneline $tempBranch" -ForegroundColor Green
    }
    
    return $result
}

<#
.SYNOPSIS
    Main sync orchestration function
.DESCRIPTION
    High-level function that coordinates the entire sync process
.PARAMETER Branch
    Branch to sync
.PARAMETER Strategy
    Override strategy (otherwise auto-detected)
.PARAMETER PreviewOnly
    Show what would happen without making changes
.PARAMETER Verbose
    Show detailed progress
.OUTPUTS
    Boolean indicating success
#>
function Sync-GitBranch {
    param(
        [string]$Branch = (git branch --show-current),
        [string]$Strategy = $null,
        [switch]$PreviewOnly,
        [switch]$Verbose
    )
    
    # Safety check - ensure we're in a git repo
    if (-not (Test-Path .git)) {
        Write-Error "Not in a git repository"
        return $false
    }
    
    # Fetch latest
    Write-Status "Fetching latest from origin..."
    git fetch origin main --quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to fetch from origin"
        return $false
    }
    
    # Detect uncommitted changes
    $hasUncommitted = [bool](git status --porcelain)
    if ($hasUncommitted) {
        Write-Warning "Detected uncommitted changes"
        if (-not $PreviewOnly) {
            Write-Status "Stashing uncommitted changes..."
            $stashMessage = "sync-core auto-stash $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
            git stash push -m $stashMessage
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to stash changes"
                return $false
            }
        }
    }
    
    # Determine strategy if not provided
    if (-not $Strategy) {
        $Strategy = Get-SyncStrategy -Branch $Branch -Verbose:$Verbose
    }
    
    Write-Decision "Using strategy: $Strategy"
    
    # Execute strategy
    $success = $true
    
    switch ($Strategy) {
        "squash-reset" {
            Write-Status "Handling squash merge..."
            
            if ($PreviewOnly) {
                Write-Info "[Preview] Would preserve local commits and reset to main"
            } else {
                $preserveResult = Preserve-LocalCommits -Branch $Branch
                
                if ($preserveResult.Success) {
                    Write-Success "Successfully preserved $($preserveResult.PreservedCommits.Count) commits"
                    
                    # Force push to update remote
                    if ($Branch -eq "dev/main") {
                        Write-Status "Updating remote $Branch..."
                        git push origin $Branch --force-with-lease
                        if ($LASTEXITCODE -eq 0) {
                            Write-Success "Remote branch updated"
                        } else {
                            Write-Warning "Could not update remote (may need manual push)"
                        }
                    }
                } else {
                    Write-Error "Failed to preserve commits safely"
                    $success = $false
                }
            }
        }
        
        "fast-forward" {
            Write-Status "Fast-forwarding to latest..."
            
            if ($PreviewOnly) {
                Write-Info "[Preview] Would fast-forward to origin/main"
            } else {
                git merge origin/main --ff-only
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Fast-forward complete"
                } else {
                    Write-Error "Fast-forward failed"
                    $success = $false
                }
            }
        }
        
        "smart-rebase" {
            Write-Status "Rebasing onto latest main..."
            
            if ($PreviewOnly) {
                Write-Info "[Preview] Would rebase onto origin/main"
            } else {
                git rebase origin/main
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Rebase successful"
                    
                    # Push if needed
                    if (git status | Select-String "Your branch is ahead") {
                        Write-Status "Pushing rebased commits..."
                        git push origin $Branch --force-with-lease
                    }
                } else {
                    Write-Warning "Rebase has conflicts - attempting merge strategy"
                    git rebase --abort 2>$null
                    
                    git merge origin/main --no-edit
                    if ($LASTEXITCODE -eq 0) {
                        Write-Success "Resolved via merge"
                    } else {
                        Write-Error "Both rebase and merge failed - manual intervention needed"
                        $success = $false
                    }
                }
            }
        }
        
        "merge" {
            Write-Status "Merging with main..."
            
            if ($PreviewOnly) {
                Write-Info "[Preview] Would merge origin/main"
            } else {
                git merge origin/main --no-edit
                if ($LASTEXITCODE -eq 0) {
                    Write-Success "Merge complete"
                } else {
                    Write-Error "Merge failed - manual intervention needed"
                    $success = $false
                }
            }
        }
        
        "up-to-date" {
            Write-Success "Already up to date with main"
        }
        
        default {
            Write-Error "Unknown strategy: $Strategy"
            $success = $false
        }
    }
    
    # Restore stashed changes
    if ($hasUncommitted -and -not $PreviewOnly -and $success) {
        Write-Status "Restoring stashed changes..."
        git stash pop --quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not auto-restore stash - run 'git stash pop' manually"
        } else {
            Write-Success "Restored uncommitted changes"
        }
    }
    
    return $success
}

# Export module functions
Export-ModuleMember -Function @(
    'Test-SquashMerge',
    'Get-SyncStrategy',
    'Preserve-LocalCommits',
    'Sync-GitBranch',
    'Write-Status',
    'Write-Decision',
    'Write-Success',
    'Write-Warning',
    'Write-Error',
    'Write-Info'
)