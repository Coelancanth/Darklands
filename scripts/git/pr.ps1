#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Complete PR workflow - create, merge, and sync in one command
.DESCRIPTION
    Handles the entire PR lifecycle intelligently:
    - Create: Makes PR from current branch
    - Merge: Squash merges and auto-syncs dev/main
    - Sync: Intelligently syncs any branch
.EXAMPLE
    pr create
    Creates a PR from current branch with smart title/body
.EXAMPLE
    pr merge
    Merges current PR and auto-handles squash merge cleanup
.EXAMPLE
    pr sync
    Intelligently syncs current branch (rebase or reset as needed)
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet("create", "merge", "sync", "status", "continue", "start", "ready", "abandon")]
    [string]$Action = "status",
    
    [Parameter(Position = 1)]
    [string]$BranchName = ""
)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

function Write-Title($message) { 
    Write-Host ""
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Blue
    Write-Host "  $message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Blue
    Write-Host ""
}

switch ($Action) {
    "create" {
        Write-Title "Creating Pull Request"
        
        # Get current branch
        $branch = git branch --show-current
        
        if ($branch -eq "main") {
            Write-Host "❌ Cannot create PR from main branch" -ForegroundColor Red
            Write-Host "💡 Create a feature branch first: git checkout -b feat/your-feature" -ForegroundColor Yellow
            exit 1
        }
        
        # Auto-generate title from branch name
        $title = $branch -replace '^\w+/', '' -replace '-', ' ' -replace '_', ' '
        $title = (Get-Culture).TextInfo.ToTitleCase($title)
        
        # Check for conventional commit format
        if ($branch -match '^(feat|fix|tech|docs)/([A-Z]+_\d+)(.*)') {
            $type = $Matches[1]
            $ticket = $Matches[2]
            $desc = $Matches[3] -replace '^-', ''
            $title = "$type($ticket): $desc" -replace '-', ' '
        }
        
        Write-Host "📝 Creating PR: $title" -ForegroundColor Green
        
        # Create PR
        gh pr create --title "$title" --body "## Summary`n`nImplements $title`n`n## Changes`n- `n`n## Testing`n- [ ] Unit tests pass`n- [ ] Integration tests pass`n`n🤖 Created with smart-pr"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ PR created successfully!" -ForegroundColor Green
            gh pr view --web
        }
    }
    
    "merge" {
        Write-Title "Merging Pull Request"
        
        # Check if PR exists
        $prNumber = gh pr view --json number --jq '.number' 2>$null
        
        if (-not $prNumber) {
            Write-Host "❌ No PR found for current branch" -ForegroundColor Red
            Write-Host "💡 Create one first: pr create" -ForegroundColor Yellow
            exit 1
        }
        
        Write-Host "🔄 Merging PR #$prNumber with squash..." -ForegroundColor Cyan
        
        # Merge with squash
        gh pr merge --squash --delete-branch
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ PR merged!" -ForegroundColor Green
            
            # Auto-sync if on dev/main
            $branch = git branch --show-current
            if ($branch -eq "dev/main") {
                Write-Host ""
                Write-Host "🎯 Auto-syncing dev/main after squash merge..." -ForegroundColor Yellow
                
                # Give GitHub a moment to process the merge
                Start-Sleep -Seconds 2
                
                # Run smart sync
                & "$scriptPath\smart-sync.ps1"
            }
        }
    }
    
    "sync" {
        Write-Title "Smart Sync"
        & "$scriptPath\smart-sync.ps1"
    }
    
    "status" {
        Write-Title "PR Status"
        
        $branch = git branch --show-current
        Write-Host "📍 Current branch: $branch" -ForegroundColor Cyan
        
        # Check for PR
        $pr = gh pr view --json number,state,title,url 2>$null | ConvertFrom-Json
        
        if ($pr) {
            Write-Host ""
            Write-Host "📋 PR #$($pr.number): $($pr.title)" -ForegroundColor Green
            Write-Host "   Status: $($pr.state)" -ForegroundColor $(if ($pr.state -eq "OPEN") { "Yellow" } else { "Gray" })
            Write-Host "   URL: $($pr.url)" -ForegroundColor Blue
        } else {
            Write-Host "📭 No PR for this branch" -ForegroundColor Gray
            Write-Host ""
            Write-Host "💡 Create one with: pr create" -ForegroundColor Yellow
        }
        
        # Show sync status
        Write-Host ""
        Write-Host "📊 Sync Status:" -ForegroundColor Cyan
        $ahead = git rev-list --count origin/main..HEAD
        $behind = git rev-list --count HEAD..origin/main
        
        if ($ahead -eq 0 -and $behind -eq 0) {
            Write-Host "   ✅ Up to date with main" -ForegroundColor Green
        } else {
            if ($ahead -gt 0) { Write-Host "   ⬆️  $ahead commits ahead" -ForegroundColor Yellow }
            if ($behind -gt 0) { Write-Host "   ⬇️  $behind commits behind" -ForegroundColor Yellow }
            Write-Host ""
            Write-Host "💡 Sync with: pr sync" -ForegroundColor Yellow
        }
    }
    
    "continue" {
        Write-Title "PR Continue - Post-Merge Workflow"
        & "$scriptPath\pr-continue.ps1"
    }
    
    "start" {
        Write-Title "Starting New Feature Branch"
        
        # Ensure on main and up to date
        $current = git branch --show-current
        if ($current -ne "main") {
            Write-Host "🔄 Switching to main..." -ForegroundColor Yellow
            git checkout main
        }
        
        Write-Host "⬇️  Pulling latest main..." -ForegroundColor Cyan
        git pull origin main --ff-only
        
        # Get branch name
        if (-not $BranchName) {
            Write-Host "Enter branch name (e.g., TD_012, fix-bug): " -ForegroundColor Yellow -NoNewline
            $BranchName = Read-Host
        }
        
        # Normalize branch name
        if ($BranchName -match '^(TD|VS|BR)_\d+') {
            $BranchName = "feat/$BranchName"
        } elseif ($BranchName -notmatch '^(feat|fix|docs|refactor)/') {
            $BranchName = "feat/$BranchName"
        }
        
        Write-Host "🌿 Creating branch: $BranchName" -ForegroundColor Green
        git checkout -b $BranchName
        
        Write-Host "✅ Ready to work! Next: Make changes, then 'pr ready'" -ForegroundColor Green
    }
    
    "ready" {
        Write-Title "Preparing Pull Request"
        
        # Alias for create with better messaging
        Write-Host "📤 Pushing branch and creating PR..." -ForegroundColor Cyan
        
        # Push current branch
        $branch = git branch --show-current
        git push -u origin $branch
        
        # Create PR using existing logic
        $PSCmdlet.MyInvocation.MyCommand.Parameters["Action"].DefaultValue = "create"
        & $MyInvocation.MyCommand.Path create
    }
    
    "abandon" {
        Write-Title "Abandoning Current Branch"
        
        $branch = git branch --show-current
        if ($branch -eq "main") {
            Write-Host "✅ Already on main" -ForegroundColor Green
            exit 0
        }
        
        Write-Host "⚠️  This will delete branch: $branch" -ForegroundColor Yellow
        Write-Host "Are you sure? (y/N): " -ForegroundColor Yellow -NoNewline
        $confirm = Read-Host
        
        if ($confirm -ne 'y') {
            Write-Host "Cancelled" -ForegroundColor Gray
            exit 0
        }
        
        # Switch to main
        Write-Host "🔄 Switching to main..." -ForegroundColor Cyan
        git checkout main -f
        git pull origin main --ff-only
        
        # Delete branch
        Write-Host "🗑️  Deleting branch: $branch" -ForegroundColor Yellow
        git branch -D $branch
        git push origin --delete $branch 2>$null
        
        Write-Host "✅ Branch abandoned. You're now on main." -ForegroundColor Green
    }
}