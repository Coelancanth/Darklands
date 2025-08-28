#!/usr/bin/env pwsh
# Install REAL Claude Protection that actually works
# This intercepts the 'claude' command that users actually type

param(
    [switch]$Uninstall
)

$profilePath = $PROFILE
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host ""
Write-Host "🛡️  Installing Claude Protection (The Right Way)" -ForegroundColor Cyan
Write-Host ("─" * 50) -ForegroundColor DarkGray
Write-Host ""

# Create PowerShell profile if it doesn't exist
if (-not (Test-Path $profilePath)) {
    New-Item -Path $profilePath -ItemType File -Force | Out-Null
    Write-Host "Created PowerShell profile at: $profilePath" -ForegroundColor Green
}

# The function that will intercept 'claude' command
$protectionFunction = @"

# BlockLife Claude Protection System
function claude {
    # Get current directory
    `$currentPath = (Get-Location).Path
    
    # Check if we're in BlockLife main directory
    if (`$currentPath -match "blocklife(?!.*personas)" -and (Test-Path ".git")) {
        # Check for protection bypass
        if (-not (Test-Path ".claude-protection")) {
            Clear-Host
            Write-Host ""
            Write-Host "  🏗️  BlockLife Workspace Protection" -ForegroundColor Cyan
            Write-Host "  `$('─' * 50)" -ForegroundColor DarkGray
            Write-Host ""
            Write-Host "  You're opening Claude in the main directory." -ForegroundColor Yellow
            Write-Host "  Consider using a persona workspace instead:" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "  Benefits of Persona Workspaces:" -ForegroundColor Green
            Write-Host "    ✓ Isolated context for focused work" -ForegroundColor Gray
            Write-Host "    ✓ No conflicts between different features" -ForegroundColor Gray
            Write-Host "    ✓ Clean git history per workspace" -ForegroundColor Gray
            Write-Host ""
            Write-Host "  Quick Start:" -ForegroundColor Cyan
            Write-Host "    blocklife dev         # Open Dev Engineer" -ForegroundColor White
            Write-Host "    blocklife tech        # Open Tech Lead" -ForegroundColor White
            Write-Host "    blocklife test        # Open Test Specialist" -ForegroundColor White
            Write-Host ""
            Write-Host "  `$('─' * 50)" -ForegroundColor DarkGray
            Write-Host ""
            Write-Host "  Choose:" -ForegroundColor Cyan
            Write-Host "    [P] Switch to Persona workspace" -ForegroundColor Green
            Write-Host "    [C] Continue anyway (this time)" -ForegroundColor Yellow
            Write-Host "    [D] Don't show again (disable)" -ForegroundColor Red
            Write-Host ""
            
            `$choice = Read-Host "  Your choice (P/C/D)"
            
            switch (`$choice.ToUpper()) {
                "P" {
                    Write-Host ""
                    Write-Host "  Use: blocklife [persona-name]" -ForegroundColor Green
                    return
                }
                "D" {
                    @{ disabled = `$true; date = Get-Date -Format "yyyy-MM-dd HH:mm" } | 
                        ConvertTo-Json | Set-Content .claude-protection
                    Write-Host "  Protection disabled for this project." -ForegroundColor Yellow
                }
                "C" {
                    # Continue to launch
                }
                default {
                    Write-Host "  Cancelled." -ForegroundColor Gray
                    return
                }
            }
        }
    }
    
    # Launch the real Claude CLI with all arguments
    & claude.exe `$args
}
"@

if ($Uninstall) {
    # Remove the function from profile
    $profileContent = Get-Content $profilePath -Raw
    $profileContent = $profileContent -replace '# BlockLife Claude Protection System.*?^}', '' -replace '(?m)^\s*$\n', ''
    Set-Content $profilePath $profileContent
    Write-Host "✅ Claude protection removed from PowerShell profile" -ForegroundColor Green
    Write-Host "   Restart PowerShell to complete removal" -ForegroundColor Gray
} else {
    # Check if function already exists
    $profileContent = Get-Content $profilePath -Raw -ErrorAction SilentlyContinue
    
    if ($profileContent -match "BlockLife Claude Protection System") {
        Write-Host "⚠️  Protection already installed. Updating..." -ForegroundColor Yellow
        # Remove old version
        $profileContent = $profileContent -replace '# BlockLife Claude Protection System.*?^}', '' -replace '(?m)^\s*$\n', ''
        Set-Content $profilePath $profileContent
    }
    
    # Add the function
    Add-Content $profilePath "`n$protectionFunction"
    
    Write-Host "✅ Claude protection installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📝 What this does:" -ForegroundColor Cyan
    Write-Host "   • Intercepts 'claude' command in BlockLife main directory" -ForegroundColor Gray
    Write-Host "   • Shows friendly reminder about persona workspaces" -ForegroundColor Gray
    Write-Host "   • Allows easy bypass when needed" -ForegroundColor Gray
    Write-Host "   • Works automatically - no need to type ./claude" -ForegroundColor Gray
    Write-Host ""
    Write-Host "🔄 To activate:" -ForegroundColor Yellow
    Write-Host "   Restart PowerShell or run: . `$PROFILE" -ForegroundColor White
    Write-Host ""
    Write-Host "🗑️  To uninstall:" -ForegroundColor Gray
    Write-Host "   .\scripts\protection\install-claude-protection.ps1 -Uninstall" -ForegroundColor Gray
}

Write-Host ""
Write-Host ("─" * 50) -ForegroundColor DarkGray
Write-Host ""
