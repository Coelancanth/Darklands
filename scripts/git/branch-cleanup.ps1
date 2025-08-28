#!/usr/bin/env pwsh
# Intelligent Branch Cleanup with Native Git State Management
# Usage: .\scripts\branch-cleanup.ps1 [branch-name]
# If no branch specified, cleans up current branch
#
# Algorithm:
# 1. Sync with remote using git fetch --prune (removes stale references)
# 2. Check if branch still exists on remote (indicates active work)
# 3. For deleted remote branches, safely clean up local branch
# 4. For active branches, check PR status via GitHub CLI

param(
    [string]$BranchName = "",
    [switch]$Force = $false
)

if ($BranchName -eq "") {
    $BranchName = git rev-parse --abbrev-ref HEAD
}

Write-Host "🧹 Intelligent Branch Cleanup Tool" -ForegroundColor Cyan
Write-Host "   Target Branch: $BranchName" -ForegroundColor White
Write-Host ""

# Safety check - never delete main
if ($BranchName -eq "main") {
    Write-Host "   ❌ Cannot cleanup main branch!" -ForegroundColor Red
    exit 1
}

# Step 1: Sync with remote and prune stale references
Write-Host "   📡 Syncing with remote..." -ForegroundColor Yellow
git fetch origin --prune 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ❌ Failed to sync with remote" -ForegroundColor Red
    exit 1
}

# Step 2: Check if branch exists on remote
$remoteBranches = git branch -r 2>$null | ForEach-Object { $_.Trim() }
$remoteBranchExists = $remoteBranches -contains "origin/$BranchName"

if (-not $remoteBranchExists) {
    # Remote branch doesn't exist - it was likely merged and deleted
    Write-Host "   ✅ Remote branch already deleted (likely merged)" -ForegroundColor Green
    Write-Host "   📊 Branch was cleaned up on remote after merge" -ForegroundColor Cyan
    
    # Try to get PR info for context (but don't fail if we can't)
    try {
        # Try with --state all to find merged PRs
        $prList = gh pr list --state all --head $BranchName --limit 1 --json number,title,mergedAt,url 2>$null
        if ($LASTEXITCODE -eq 0 -and $prList -ne "[]") {
            $prInfo = $prList | ConvertFrom-Json | Select-Object -First 1
            if ($prInfo.mergedAt) {
                Write-Host "   📝 Found merged PR: $($prInfo.title)" -ForegroundColor Green
                Write-Host "   🔗 $($prInfo.url)" -ForegroundColor Cyan
                Write-Host "   📅 Merged: $($prInfo.mergedAt)" -ForegroundColor Gray
            }
        }
    } catch {
        # PR info is nice to have but not required
    }
    
    Write-Host ""
    Write-Host "   🧹 Performing local cleanup..." -ForegroundColor Yellow
    
    # Check if we're on the branch we're trying to delete
    $currentBranch = git rev-parse --abbrev-ref HEAD
    if ($currentBranch -eq $BranchName) {
        Write-Host "   → Switching to main branch"
        git checkout main 2>&1 | Out-Null
        
        Write-Host "   → Pulling latest main"
        git pull origin main 2>&1 | Out-Null
    }
    
    # Delete local branch (use -D if -Force, otherwise -d)
    Write-Host "   → Deleting local branch: $BranchName"
    if ($Force) {
        git branch -D $BranchName 2>&1 | Out-Null
    } else {
        git branch -d $BranchName 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "   ⚠️  Branch has unmerged changes" -ForegroundColor Yellow
            Write-Host "   💡 Use -Force flag to delete anyway" -ForegroundColor Cyan
            exit 1
        }
    }
    
    Write-Host ""
    Write-Host "   ✅ Cleanup complete! Branch removed locally." -ForegroundColor Green
    Write-Host "   🚀 Ready for new work on main branch" -ForegroundColor Cyan
    
} else {
    # Remote branch still exists - check if there's an active PR
    Write-Host "   📌 Remote branch still exists" -ForegroundColor Yellow
    
    try {
        $prInfoJson = gh pr view $BranchName --json state,merged,url,title,draft 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            $prInfo = $prInfoJson | ConvertFrom-Json
            
            if ($prInfo.merged -eq $true) {
                # PR is merged but remote branch wasn't deleted
                Write-Host "   ✅ PR is merged: $($prInfo.title)" -ForegroundColor Green
                Write-Host "   🔗 $($prInfo.url)" -ForegroundColor Cyan
                Write-Host "   ⚠️  Remote branch wasn't automatically deleted" -ForegroundColor Yellow
                
                Write-Host ""
                Write-Host "   🧹 Performing full cleanup..." -ForegroundColor Yellow
                
                # Switch to main if needed
                $currentBranch = git rev-parse --abbrev-ref HEAD
                if ($currentBranch -eq $BranchName) {
                    Write-Host "   → Switching to main branch"
                    git checkout main 2>&1 | Out-Null
                    
                    Write-Host "   → Pulling latest main"
                    git pull origin main 2>&1 | Out-Null
                }
                
                # Delete local branch
                Write-Host "   → Deleting local branch: $BranchName"
                git branch -d $BranchName 2>&1 | Out-Null
                
                # Delete remote branch
                Write-Host "   → Deleting remote branch: $BranchName"
                git push origin --delete $BranchName 2>&1 | Out-Null
                
                Write-Host ""
                Write-Host "   ✅ Full cleanup complete!" -ForegroundColor Green
                
            } elseif ($prInfo.draft -eq $true) {
                Write-Host "   📝 Draft PR in progress: $($prInfo.title)" -ForegroundColor Cyan
                Write-Host "   🔗 $($prInfo.url)" -ForegroundColor Cyan
                Write-Host "   💡 Complete the PR before cleanup" -ForegroundColor Yellow
                exit 1
            } else {
                Write-Host "   ⏳ Open PR exists (state: $($prInfo.state))" -ForegroundColor Yellow
                Write-Host "   📝 $($prInfo.title)" -ForegroundColor White
                Write-Host "   🔗 $($prInfo.url)" -ForegroundColor Cyan
                Write-Host "   💡 Merge the PR first, then run cleanup again" -ForegroundColor Yellow
                exit 1
            }
        } else {
            # No PR found but remote branch exists
            Write-Host "   ⚠️  No PR found for branch: $BranchName" -ForegroundColor Yellow
            Write-Host "   🔍 Remote branch exists but has no associated PR" -ForegroundColor Cyan
            
            # Check if branch has unpushed commits
            $unpushed = git log origin/$BranchName..$BranchName --oneline 2>$null
            if ($unpushed) {
                Write-Host "   📤 Branch has unpushed commits:" -ForegroundColor Yellow
                $unpushed | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
                Write-Host ""
                Write-Host "   💡 Push your changes: git push origin $BranchName" -ForegroundColor Cyan
                Write-Host "   💡 Then create PR: gh pr create" -ForegroundColor Cyan
            } else {
                # Check if branch has unique commits not in main
                $uniqueCommits = git log main..$BranchName --oneline 2>$null
                if ($uniqueCommits) {
                    Write-Host "   📊 Branch has commits not in main:" -ForegroundColor Yellow
                    $uniqueCommits | Select-Object -First 3 | ForEach-Object { Write-Host "      $_" -ForegroundColor Gray }
                    $commitCount = ($uniqueCommits | Measure-Object).Count
                    if ($commitCount -gt 3) {
                        Write-Host "      ... and $($commitCount - 3) more" -ForegroundColor Gray
                    }
                    Write-Host ""
                    Write-Host "   💡 Create a PR: gh pr create" -ForegroundColor Cyan
                    Write-Host "   🗑️  Or force cleanup: .\scripts\branch-cleanup.ps1 $BranchName -Force" -ForegroundColor Yellow
                } else {
                    Write-Host "   📊 Branch has no unique commits" -ForegroundColor Gray
                    Write-Host "   🗑️  Safe to delete: .\scripts\branch-cleanup.ps1 $BranchName -Force" -ForegroundColor Cyan
                }
            }
            exit 1
        }
    } catch {
        Write-Host "   ⚠️  Could not check PR status (GitHub CLI issue)" -ForegroundColor Yellow
        Write-Host "   📊 But remote branch still exists at origin/$BranchName" -ForegroundColor Cyan
        Write-Host "   💡 Options:" -ForegroundColor Yellow
        Write-Host "      1. Create PR: gh pr create" -ForegroundColor Gray
        Write-Host "      2. Force cleanup: .\scripts\branch-cleanup.ps1 $BranchName -Force" -ForegroundColor Gray
        exit 1
    }
}
