#!/usr/bin/env pwsh
# Branch Status Check for AI Persona Embodiment (Windows PowerShell)
# Usage: .\scripts\branch-status-check.ps1

Write-Host "🌿 Branch Status Analysis:" -ForegroundColor Cyan

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD
Write-Host "   Current Branch: $currentBranch" -ForegroundColor White

# Check if on main
if ($currentBranch -eq "main") {
    Write-Host "   ✅ On main branch - ready for new work" -ForegroundColor Green
    Write-Host "   💡 Create feature branch for work items: git checkout -b feat/VS_XXX" -ForegroundColor Yellow
    exit 0
}

# Check if branch has associated PR
Write-Host "   🔍 Checking PR status..." -ForegroundColor Yellow

try {
    # Use GitHub CLI to check PR status
    $prInfoJson = gh pr view $currentBranch --json state,merged,url,title 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        $prInfo = $prInfoJson | ConvertFrom-Json
        
        Write-Host "   📋 PR Found: $($prInfo.title)" -ForegroundColor Cyan
        Write-Host "   🔗 URL: $($prInfo.url)" -ForegroundColor Cyan
        
        if ($prInfo.merged -eq $true) {
            Write-Host "   ✅ PR MERGED - Branch cleanup recommended" -ForegroundColor Green
            Write-Host ""
            Write-Host "   🧹 Suggested Actions:" -ForegroundColor Yellow
            Write-Host "      git checkout main"
            Write-Host "      git pull origin main"
            Write-Host "      git branch -d $currentBranch"
            Write-Host "      git push origin --delete $currentBranch"
            Write-Host ""
            Write-Host "   Would you like to clean up this merged branch? (y/n)" -ForegroundColor Yellow
            
        } elseif ($prInfo.state -eq "OPEN") {
            Write-Host "   🟡 PR OPEN - Check before continuing work" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "   🤔 Consider:" -ForegroundColor Cyan
            Write-Host "      - Are you continuing work on this PR?"
            Write-Host "      - Has PR received feedback requiring changes?"
            Write-Host "      - Should this work be in a different branch?"
            Write-Host ""
            
        } elseif ($prInfo.state -eq "CLOSED") {
            Write-Host "   ❌ PR CLOSED (not merged) - Investigate why" -ForegroundColor Red
            Write-Host ""
            Write-Host "   🚨 Warning: This branch's PR was closed without merging" -ForegroundColor Red
            Write-Host "   🤔 Possible reasons:" -ForegroundColor Cyan
            Write-Host "      - Work was abandoned or superseded"
            Write-Host "      - PR was rejected and needs rework"
            Write-Host "      - Work moved to different branch"
            Write-Host ""
            Write-Host "   💡 Suggestion: Review PR comments before continuing" -ForegroundColor Yellow
        }
    }
} catch {
    # No PR found or GitHub CLI error
    Write-Host "   ⚪ No PR found for this branch" -ForegroundColor Gray
    Write-Host ""
    Write-Host "   🤔 Consider:" -ForegroundColor Cyan
    Write-Host "      - Is this work ready for PR? (gh pr create)"
    Write-Host "      - Should this be merged back to main?"
    Write-Host "      - Is this experimental work?"
    Write-Host "      - Should you create a new branch instead?"
}

# Check branch freshness
Write-Host ""
Write-Host "   🔄 Checking branch freshness..." -ForegroundColor Yellow
git fetch origin main --quiet 2>$null

try {
    git rev-parse --verify origin/main >$null 2>&1
    if ($LASTEXITCODE -eq 0) {
        $behindCount = (git rev-list --count HEAD..origin/main 2>$null) -as [int]
        $aheadCount = (git rev-list --count origin/main..HEAD 2>$null) -as [int]
        
        if ($behindCount -gt 0) {
            Write-Host "   ⚠️  Branch is $behindCount commits behind main" -ForegroundColor Yellow
            if ($behindCount -gt 10) {
                Write-Host "      🚨 STALE: Consider rebasing or creating fresh branch" -ForegroundColor Red
                Write-Host "      git rebase origin/main"
            } else {
                Write-Host "      💡 Consider updating: git rebase origin/main" -ForegroundColor Yellow
            }
        }
        
        if ($aheadCount -gt 0) {
            Write-Host "   📈 Branch is $aheadCount commits ahead of main" -ForegroundColor Green
        }
        
        if ($behindCount -eq 0 -and $aheadCount -eq 0) {
            Write-Host "   ✅ Branch is up to date with main" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "   ⚠️  Could not fetch origin/main - check network" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "   📝 Ready to proceed with current branch? Review above analysis." -ForegroundColor Cyan
