#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Install smart-sync tools for zero-friction git workflow
#>

Write-Host "Installing Smart Sync Tools..." -ForegroundColor Cyan

# Add git alias
Write-Host "Adding 'git sync' alias..." -ForegroundColor Yellow
git config --global alias.sync "!powershell -File '$PSScriptRoot/smart-sync.ps1'"

# Add pr function to PowerShell profile
$profileContent = @'

# Smart PR workflow
function pr {
    param([string]$Action = "status")
    & "C:\Users\Coel\Documents\Godot\BlockLife\scripts\git\pr.ps1" $Action
}
'@

if (Test-Path $PROFILE) {
    $currentProfile = Get-Content $PROFILE -Raw
    if ($currentProfile -notmatch "Smart PR workflow") {
        Add-Content $PROFILE $profileContent
        Write-Host "Added 'pr' command to PowerShell profile" -ForegroundColor Green
    } else {
        Write-Host "'pr' command already in profile" -ForegroundColor Gray
    }
} else {
    New-Item -Path $PROFILE -ItemType File -Force | Out-Null
    Set-Content $PROFILE $profileContent
    Write-Host "Created PowerShell profile with 'pr' command" -ForegroundColor Green
}

Write-Host ""
Write-Host "✅ Installation complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Available commands:" -ForegroundColor Cyan
Write-Host "  git sync    - Intelligently sync any branch" -ForegroundColor White
Write-Host "  pr create   - Create PR from current branch" -ForegroundColor White
Write-Host "  pr merge    - Merge PR and auto-sync" -ForegroundColor White
Write-Host "  pr sync     - Same as 'git sync'" -ForegroundColor White
Write-Host "  pr status   - Show PR and sync status" -ForegroundColor White
Write-Host ""
Write-Host "💡 Restart your terminal to use the 'pr' command" -ForegroundColor Yellow