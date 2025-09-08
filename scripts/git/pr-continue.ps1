#!/usr/bin/env pwsh
<#
.SYNOPSIS
    🔄 PR Continue - Zero-friction post-merge workflow
    
.DESCRIPTION
    Automatically handles the post-PR merge workflow:
    - Detects if your PR was merged
    - Switches to fresh main
    - Preserves your uncommitted work
    - Creates fresh branch for continued work
    
.EXAMPLE
    ./scripts/git/pr-continue.ps1
    
.EXAMPLE
    pr continue  # If you have the alias set up
    
.NOTES
    Part of the zero-friction PR lifecycle automation
#>

param(
    [switch]$Check = $false        # Just check if PR was merged
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# Colors for beautiful output
$colors = @{
    Success = "Green"
    Info = "Cyan"
    Warning = "Yellow"
    Error = "Red"
    Highlight = "Magenta"
}

function Write-Status($message, $color = "Info") {
    Write-Host $message -ForegroundColor $colors[$color]
}

function Get-PRStatus {
    param([string]$branch)
    
    # Check if PR exists and its status
    $prList = gh pr list --head $branch --json number,state,mergedAt 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $prList) {
        return @{ Exists = $false }
    }
    
    $pr = $prList | ConvertFrom-Json | Select-Object -First 1
    if (-not $pr) {
        return @{ Exists = $false }
    }
    
    return @{
        Exists = $true
        Number = $pr.number
        State = $pr.state
        IsMerged = ($pr.state -eq "MERGED" -or $null -ne $pr.mergedAt)
    }
}

function Save-WorkInProgress {
    $changes = git status --porcelain
    if ($changes) {
        Write-Status "📦 Saving your work in progress..." "Info"
        $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
        git stash push -m "pr-continue: WIP from $(git branch --show-current) at $timestamp"
        return $true
    }
    return $false
}

function Restore-WorkInProgress {
    Write-Status "📦 Restoring your work in progress..." "Info"
    git stash pop
    if ($LASTEXITCODE -eq 0) {
        Write-Status "✅ Work restored successfully!" "Success"
    } else {
        Write-Status "⚠️  Some conflicts during restore - please resolve manually" "Warning"
    }
}

# Main logic
try {
    # Get current branch
    $currentBranch = git branch --show-current
    if ($currentBranch -eq "main") {
        Write-Status "✅ Already on main branch - nothing to do!" "Success"
        exit 0
    }
    
    Write-Status "🔍 Checking PR status for branch: $currentBranch" "Info"
    
    # Check PR status
    $prStatus = Get-PRStatus -branch $currentBranch
    
    if ($Check) {
        # Just checking status
        if ($prStatus.IsMerged) {
            Write-Status "✅ PR #$($prStatus.Number) was merged!" "Success"
            exit 0
        } elseif ($prStatus.Exists) {
            Write-Status "⏳ PR #$($prStatus.Number) is still open (state: $($prStatus.State))" "Warning"
            exit 1
        } else {
            Write-Status "❓ No PR found for branch: $currentBranch" "Info"
            exit 2
        }
    }
    
    # Full continue workflow
    if (-not $prStatus.IsMerged) {
        if ($prStatus.Exists) {
            Write-Status "⏳ PR #$($prStatus.Number) hasn't been merged yet (state: $($prStatus.State))" "Warning"
            Write-Status "💡 Tip: Check your PR at: https://github.com/$(gh repo view --json nameWithOwner -q .nameWithOwner)/pull/$($prStatus.Number)" "Info"
        } else {
            Write-Status "❓ No PR found for branch: $currentBranch" "Info"
            Write-Status "💡 Tip: Use 'pr create' to create a PR first" "Info"
        }
        exit 0
    }
    
    Write-Status "`n✨ PR #$($prStatus.Number) was merged! Let's get you on fresh main..." "Success"
    
    # Save any uncommitted work
    $hadChanges = Save-WorkInProgress
    
    # Switch to main and update
    Write-Status "`n🔄 Switching to main branch..." "Info"
    git checkout main
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to switch to main branch"
    }
    
    Write-Status "⬇️  Pulling latest changes..." "Info"
    git pull origin main --ff-only
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to pull latest main"
    }
    
    # Delete old feature branch
    Write-Status "🗑️  Removing old branch: $currentBranch" "Info"
    git branch -D $currentBranch
    
    # Stay on main - let the developer/persona decide next task
    Write-Status "`n✅ Now on updated main branch" "Success"
    Write-Info "Ready for your next task!"
    
    # Suggest next steps based on context
    Write-Host ""
    Write-Status "💡 Next steps:" "Highlight"
    Write-Host "  • Review backlog for next priority item"
    Write-Host "  • Use 'pr start [branch-name]' to begin new work"
    Write-Host "  • Or embody a persona to check their assigned tasks"
    
    # Restore work if any
    if ($hadChanges) {
        Write-Host ""
        Restore-WorkInProgress
    }
    
    # Final status
    Write-Host ""
    Write-Status "════════════════════════════════════════" "Highlight"
    Write-Status "  🎉 Post-merge cleanup complete!" "Success"
    Write-Status "════════════════════════════════════════" "Highlight"
    
    # Show current status
    Write-Host ""
    git status --short --branch
    
} catch {
    Write-Status "`n❌ Error: $_" "Error"
    exit 1
}