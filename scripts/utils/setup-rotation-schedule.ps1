#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Set up automated Memory Bank rotation schedule
.DESCRIPTION
    Creates scheduled tasks (Windows) or cron jobs (Linux/Mac) to automatically
    rotate Memory Bank files according to protocol:
    - Daily check at 9 AM for due rotations
    - Can be run manually to update schedule
.PARAMETER Remove
    Remove existing scheduled tasks
.EXAMPLE
    ./setup-rotation-schedule.ps1
    Set up automatic rotation schedule
.EXAMPLE
    ./setup-rotation-schedule.ps1 -Remove
    Remove automatic rotation schedule
#>

param(
    [switch]$Remove
)

$script:TaskName = "BlockLife-MemoryBank-Rotation"
$script:ScriptPath = Join-Path $PSScriptRoot "rotate-memory-bank.ps1"

# Color functions
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Gray }

function Set-WindowsSchedule {
    if ($Remove) {
        # Remove existing task
        $existingTask = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
        if ($existingTask) {
            Unregister-ScheduledTask -TaskName $script:TaskName -Confirm:$false
            Write-Success "Removed scheduled task: $script:TaskName"
        } else {
            Write-Info "No scheduled task found to remove"
        }
        return
    }
    
    # Check if task already exists
    $existingTask = Get-ScheduledTask -TaskName $script:TaskName -ErrorAction SilentlyContinue
    if ($existingTask) {
        Write-Warning "Scheduled task already exists. Updating..."
        Unregister-ScheduledTask -TaskName $script:TaskName -Confirm:$false
    }
    
    # Create the scheduled task
    $action = New-ScheduledTaskAction -Execute "pwsh.exe" `
        -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$script:ScriptPath`"" `
        -WorkingDirectory (Split-Path $script:ScriptPath)
    
    # Daily trigger at 9 AM
    $trigger = New-ScheduledTaskTrigger -Daily -At 9:00AM
    
    # Run as current user
    $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME `
        -LogonType Interactive `
        -RunLevel Limited
    
    # Settings
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable `
        -RunOnlyIfNetworkAvailable:$false `
        -MultipleInstances IgnoreNew
    
    # Register the task
    $task = Register-ScheduledTask `
        -TaskName $script:TaskName `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Description "Automatically rotate BlockLife Memory Bank files according to retention protocol"
    
    if ($task) {
        Write-Success "Created scheduled task: $script:TaskName"
        Write-Info "Runs daily at 9:00 AM"
        Write-Info "Next run: $($task.Triggers[0].StartBoundary)"
        
        # Test run
        Write-Info ""
        Write-Info "Testing task (dry run)..."
        Start-ScheduledTask -TaskName $script:TaskName
        Start-Sleep -Seconds 2
        
        $taskInfo = Get-ScheduledTaskInfo -TaskName $script:TaskName
        if ($taskInfo.LastTaskResult -eq 0) {
            Write-Success "Test run completed successfully"
        } else {
            Write-Warning "Test run completed with code: $($taskInfo.LastTaskResult)"
        }
    }
}

function Set-UnixSchedule {
    if ($Remove) {
        # Remove from crontab
        $crontab = crontab -l 2>/dev/null | Where-Object { $_ -notmatch "rotate-memory-bank.ps1" }
        if ($crontab) {
            $crontab | crontab -
            Write-Success "Removed cron job for Memory Bank rotation"
        } else {
            Write-Info "No cron job found to remove"
        }
        return
    }
    
    # Check if already in crontab
    $existingCron = crontab -l 2>/dev/null | Where-Object { $_ -match "rotate-memory-bank.ps1" }
    if ($existingCron) {
        Write-Warning "Cron job already exists"
        return
    }
    
    # Add to crontab (daily at 9 AM)
    $cronLine = "0 9 * * * cd $(Split-Path $script:ScriptPath) && pwsh -NoProfile -File $script:ScriptPath"
    
    # Get existing crontab
    $currentCron = crontab -l 2>/dev/null
    if ($currentCron) {
        ($currentCron + "`n" + $cronLine) | crontab -
    } else {
        echo $cronLine | crontab -
    }
    
    Write-Success "Added cron job for Memory Bank rotation"
    Write-Info "Runs daily at 9:00 AM"
    Write-Info "View with: crontab -l"
}

function Add-GitHookIntegration {
    $hooksPath = Join-Path (git rev-parse --git-dir) "hooks"
    $postCommitHook = Join-Path $hooksPath "post-commit"
    
    $hookContent = @'

# Memory Bank rotation check
if command -v pwsh &> /dev/null; then
    pwsh -NoProfile -Command "& '$(Join-Path $PSScriptRoot '..' '..' 'scripts' 'utils' 'rotate-memory-bank.ps1')' -DryRun | Select-String 'Would rotate' && echo 'ℹ️  Consider running: scripts/utils/rotate-memory-bank.ps1'"
fi
'@
    
    if ($Remove) {
        if (Test-Path $postCommitHook) {
            $content = Get-Content $postCommitHook -Raw
            $content = $content -replace '# Memory Bank rotation check.*?fi\n', ''
            Set-Content -Path $postCommitHook -Value $content -NoNewline
            Write-Success "Removed git hook integration"
        }
        return
    }
    
    # Add to post-commit hook
    if (Test-Path $postCommitHook) {
        $existingContent = Get-Content $postCommitHook -Raw
        if ($existingContent -notmatch "Memory Bank rotation check") {
            Add-Content -Path $postCommitHook -Value $hookContent
            Write-Success "Added Memory Bank check to post-commit hook"
        } else {
            Write-Info "Git hook integration already exists"
        }
    } else {
        # Create new hook
        Set-Content -Path $postCommitHook -Value "#!/bin/sh$hookContent"
        chmod +x $postCommitHook 2>/dev/null
        Write-Success "Created post-commit hook with Memory Bank check"
    }
}

# Main execution
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($Remove) {
    Write-Host "  🗑️  REMOVING MEMORY BANK ROTATION SCHEDULE" -ForegroundColor Yellow
} else {
    Write-Host "  📅 SETTING UP MEMORY BANK ROTATION SCHEDULE" -ForegroundColor White
}
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Check if rotation script exists
if (-not (Test-Path $script:ScriptPath)) {
    Write-Error "Rotation script not found: $script:ScriptPath"
    Write-Info "Please ensure rotate-memory-bank.ps1 exists first"
    exit 1
}

# Set up schedule based on OS
if ($IsWindows -or $PSVersionTable.Platform -eq 'Win32NT' -or -not $PSVersionTable.Platform) {
    Write-Info "Setting up Windows scheduled task..."
    Set-WindowsSchedule
} else {
    Write-Info "Setting up Unix cron job..."
    Set-UnixSchedule
}

# Add git hook integration
Write-Info ""
Write-Info "Setting up git hook integration..."
Add-GitHookIntegration

Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
if ($Remove) {
    Write-Host "  ✅ Schedule removal complete!" -ForegroundColor Green
} else {
    Write-Host "  ✅ Schedule setup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Info "Manual commands:"
    Write-Host "  Test:     ./scripts/utils/rotate-memory-bank.ps1 -DryRun" -ForegroundColor White
    Write-Host "  Force:    ./scripts/utils/rotate-memory-bank.ps1 -Force" -ForegroundColor White  
    Write-Host "  Status:   ./scripts/utils/rotate-memory-bank.ps1 -DryRun -Type all" -ForegroundColor White
}
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""