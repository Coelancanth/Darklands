#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated Memory Bank rotation for BlockLife personas
.DESCRIPTION
    Manages rotation of Memory Bank files according to protocol:
    - Monthly rotation of session-log.md
    - Quarterly rotation of active context files
    - Archive cleanup based on retention policy
    - Size-based early rotation triggers
.PARAMETER DryRun
    Preview what would be rotated without making changes
.PARAMETER Force
    Force rotation even if not due (useful for manual triggers)
.PARAMETER Type
    Specify rotation type: 'session', 'context', or 'all' (default)
.EXAMPLE
    ./rotate-memory-bank.ps1
    Check and perform all due rotations
.EXAMPLE
    ./rotate-memory-bank.ps1 -DryRun
    Preview what would be rotated
.EXAMPLE
    ./rotate-memory-bank.ps1 -Type session -Force
    Force rotate session log immediately
#>

param(
    [switch]$DryRun,
    [switch]$Force,
    [ValidateSet('session', 'context', 'all')]
    [string]$Type = 'all'
)

# Script configuration
$script:MemoryBankPath = Join-Path $PSScriptRoot ".." ".." ".claude" "memory-bank"
$script:ArchivePath = Join-Path $script:MemoryBankPath "archive"
$script:ActivePath = Join-Path $script:MemoryBankPath "active"
$script:SessionLogFile = Join-Path $script:MemoryBankPath "session-log.md"

# Retention policy (configurable)
$script:SessionLogRetentionMonths = 6
$script:ContextRetentionQuarters = 2
$script:MaxSessionLogLines = 1000
$script:MaxContextLines = 200

# Color functions for consistent output
function Write-Phase($message) { 
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
    Write-Host "  $message" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
}

function Write-Status($message) { Write-Host "🔄 $message" -ForegroundColor Cyan }
function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Gray }
function Write-DryRun($message) { Write-Host "🔍 [DRY RUN] $message" -ForegroundColor Yellow }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }

# Initialize archive directory if needed
function Initialize-ArchiveDirectory {
    if (-not (Test-Path $script:ArchivePath)) {
        if ($DryRun) {
            Write-DryRun "Would create archive directory: $script:ArchivePath"
        } else {
            New-Item -ItemType Directory -Path $script:ArchivePath -Force | Out-Null
            Write-Success "Created archive directory"
        }
    }
}

# Check if rotation is due based on schedule
function Test-RotationDue {
    param(
        [ValidateSet('session', 'context')]
        [string]$RotationType
    )
    
    $today = Get-Date
    
    switch ($RotationType) {
        'session' {
            # Check if it's the first work day of the month
            $isFirstOfMonth = $today.Day -le 3  # Allow first 3 days for flexibility
            $archivePattern = "session-log-$($today.ToString('yyyy-MM')).md"
            $alreadyRotated = Test-Path (Join-Path $script:ArchivePath $archivePattern)
            
            return ($isFirstOfMonth -and -not $alreadyRotated) -or $Force
        }
        'context' {
            # Check if it's the start of a quarter
            $quarterStarts = @(1, 4, 7, 10)
            $isQuarterStart = ($quarterStarts -contains $today.Month) -and ($today.Day -le 7)
            $quarter = [Math]::Ceiling($today.Month / 3)
            $archivePattern = "*-$($today.Year)-Q$quarter.md"
            $alreadyRotated = (Get-ChildItem -Path $script:ArchivePath -Filter $archivePattern -ErrorAction SilentlyContinue).Count -gt 0
            
            return ($isQuarterStart -and -not $alreadyRotated) -or $Force
        }
    }
}

# Check if file exceeds size limit
function Test-SizeLimit {
    param(
        [string]$FilePath,
        [int]$MaxLines
    )
    
    if (Test-Path $FilePath) {
        $lineCount = (Get-Content $FilePath | Measure-Object -Line).Lines
        return $lineCount -gt $MaxLines
    }
    return $false
}

# Rotate session log
function Invoke-SessionLogRotation {
    Write-Phase "Session Log Rotation"
    
    if (-not (Test-Path $script:SessionLogFile)) {
        Write-Warning "Session log not found, skipping rotation"
        return
    }
    
    # Check if rotation is needed
    $isDue = Test-RotationDue -RotationType 'session'
    $exceedsSize = Test-SizeLimit -FilePath $script:SessionLogFile -MaxLines $script:MaxSessionLogLines
    
    if (-not $isDue -and -not $exceedsSize) {
        Write-Info "Session log rotation not due and within size limits"
        return
    }
    
    # Determine archive name
    $lastMonth = (Get-Date).AddMonths(-1)
    $archiveName = "session-log-$($lastMonth.ToString('yyyy-MM')).md"
    $archivePath = Join-Path $script:ArchivePath $archiveName
    
    if ($exceedsSize) {
        Write-Warning "Session log exceeds $script:MaxSessionLogLines lines, forcing rotation"
    }
    
    if ($DryRun) {
        Write-DryRun "Would archive session log to: $archiveName"
        Write-DryRun "Would create new session log with carryover header"
    } else {
        # Backup existing file
        Copy-Item -Path $script:SessionLogFile -Destination $archivePath -Force
        Write-Success "Archived session log to: $archiveName"
        
        # Create new session log with carryover
        $newContent = @"
# BlockLife Session Log
> Continuation from archive/$archiveName
> Rotated on: $(Get-Date -Format 'yyyy-MM-dd HH:mm')

## $(Get-Date -Format 'yyyy-MM-dd')

### $(Get-Date -Format 'HH:mm') - DevOps Engineer
**Did**: Automated session log rotation
**Next**: Continue with development priorities
"@
        Set-Content -Path $script:SessionLogFile -Value $newContent -Encoding UTF8
        Write-Success "Created fresh session log with carryover reference"
    }
}

# Rotate active context files
function Invoke-ContextRotation {
    Write-Phase "Active Context Rotation"
    
    if (-not (Test-Path $script:ActivePath)) {
        Write-Warning "Active context directory not found, skipping rotation"
        return
    }
    
    # Check if rotation is needed
    $isDue = Test-RotationDue -RotationType 'context'
    
    if (-not $isDue -and -not $Force) {
        Write-Info "Context rotation not due"
        return
    }
    
    # Calculate current quarter
    $today = Get-Date
    $quarter = [Math]::Ceiling($today.Month / 3)
    $quarterLabel = "$($today.Year)-Q$quarter"
    
    # Get all persona files
    $personaFiles = Get-ChildItem -Path $script:ActivePath -Filter "*.md" -File
    
    foreach ($file in $personaFiles) {
        $persona = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $archiveName = "$persona-$quarterLabel.md"
        $archivePath = Join-Path $script:ArchivePath $archiveName
        
        # Check if file needs rotation due to size
        $exceedsSize = Test-SizeLimit -FilePath $file.FullName -MaxLines $script:MaxContextLines
        
        if ($exceedsSize) {
            Write-Warning "$persona context exceeds $script:MaxContextLines lines"
        }
        
        if ($DryRun) {
            Write-DryRun "Would archive $persona context to: $archiveName"
            Write-DryRun "Would preserve only 'Next Actions' section in active file"
        } else {
            # Archive the full file
            Copy-Item -Path $file.FullName -Destination $archivePath -Force
            Write-Success "Archived $persona context to: $archiveName"
            
            # Extract Next Actions section if it exists
            $content = Get-Content $file.FullName -Raw
            $nextActionsPattern = '(?ms)(## Next Actions.*?)(?=\n##|\z)'
            
            if ($content -match $nextActionsPattern) {
                $nextActions = $matches[1]
            } else {
                $nextActions = "## Next Actions`n- Continue with backlog priorities"
            }
            
            # Create trimmed active file
            $newContent = @"
# $([System.Globalization.CultureInfo]::CurrentCulture.TextInfo.ToTitleCase($persona.Replace('-', ' '))) Active Context

**Last Rotation**: $(Get-Date -Format 'yyyy-MM-dd')
**Previous Archive**: archive/$archiveName

$nextActions

## Current Work
_Starting fresh after quarterly rotation_
"@
            Set-Content -Path $file.FullName -Value $newContent -Encoding UTF8
            Write-Success "Refreshed $persona active context"
        }
    }
}

# Clean up old archives based on retention policy
function Invoke-ArchiveCleanup {
    Write-Phase "Archive Cleanup"
    
    if (-not (Test-Path $script:ArchivePath)) {
        Write-Info "No archive directory to clean"
        return
    }
    
    $now = Get-Date
    $removedCount = 0
    
    # Clean old session logs
    $sessionLogs = Get-ChildItem -Path $script:ArchivePath -Filter "session-log-*.md" -File
    foreach ($log in $sessionLogs) {
        if ($log.Name -match 'session-log-(\d{4})-(\d{2})\.md') {
            $logDate = [DateTime]::ParseExact("$($matches[1])-$($matches[2])-01", 'yyyy-MM-dd', $null)
            $ageMonths = (($now.Year - $logDate.Year) * 12) + ($now.Month - $logDate.Month)
            
            if ($ageMonths -gt $script:SessionLogRetentionMonths) {
                if ($DryRun) {
                    Write-DryRun "Would remove old session log: $($log.Name) (${ageMonths} months old)"
                } else {
                    Remove-Item $log.FullName -Force
                    Write-Info "Removed old session log: $($log.Name)"
                    $removedCount++
                }
            }
        }
    }
    
    # Clean old context archives
    $contextFiles = Get-ChildItem -Path $script:ArchivePath -Filter "*-Q*.md" -File
    foreach ($context in $contextFiles) {
        if ($context.Name -match '-(\d{4})-Q(\d)\.md') {
            $year = [int]$matches[1]
            $quarter = [int]$matches[2]
            $quartersAgo = (($now.Year - $year) * 4) + ([Math]::Ceiling($now.Month / 3) - $quarter)
            
            if ($quartersAgo -gt $script:ContextRetentionQuarters) {
                if ($DryRun) {
                    Write-DryRun "Would remove old context: $($context.Name) ($quartersAgo quarters old)"
                } else {
                    Remove-Item $context.FullName -Force
                    Write-Info "Removed old context: $($context.Name)"
                    $removedCount++
                }
            }
        }
    }
    
    if ($removedCount -gt 0) {
        Write-Success "Cleaned up $removedCount old archive file(s)"
    } else {
        Write-Info "No archives exceeded retention policy"
    }
}

# Generate status report
function Show-RotationStatus {
    Write-Phase "Memory Bank Status"
    
    # Session log status
    if (Test-Path $script:SessionLogFile) {
        $lines = (Get-Content $script:SessionLogFile | Measure-Object -Line).Lines
        $sizePercent = [Math]::Round(($lines / $script:MaxSessionLogLines) * 100)
        Write-Info "Session log: $lines lines ($sizePercent% of limit)"
    }
    
    # Active context status
    if (Test-Path $script:ActivePath) {
        $personaFiles = Get-ChildItem -Path $script:ActivePath -Filter "*.md" -File
        foreach ($file in $personaFiles) {
            $lines = (Get-Content $file.FullName | Measure-Object -Line).Lines
            $sizePercent = [Math]::Round(($lines / $script:MaxContextLines) * 100)
            $persona = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
            Write-Info "$persona context: $lines lines ($sizePercent% of limit)"
        }
    }
    
    # Archive status
    if (Test-Path $script:ArchivePath) {
        $archiveCount = (Get-ChildItem -Path $script:ArchivePath -File).Count
        Write-Info "Archive contains $archiveCount file(s)"
    }
}

# Main execution
function Main {
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  🗄️  MEMORY BANK ROTATION MANAGER" -ForegroundColor White
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Warning "Running in DRY RUN mode - no changes will be made"
    }
    
    # Initialize archive directory
    Initialize-ArchiveDirectory
    
    # Perform rotations based on type parameter
    switch ($Type) {
        'session' {
            Invoke-SessionLogRotation
        }
        'context' {
            Invoke-ContextRotation
        }
        'all' {
            Invoke-SessionLogRotation
            Invoke-ContextRotation
        }
    }
    
    # Always perform cleanup
    Invoke-ArchiveCleanup
    
    # Show status
    Show-RotationStatus
    
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  ✅ Memory Bank rotation check complete!" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

# Execute main function
Main