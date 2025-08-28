#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix chronological ordering of Memory Bank session log entries
.DESCRIPTION
    Parses session log entries and reorders them chronologically within each date section.
    Preserves all content and formatting while ensuring temporal consistency.
    Creates automatic backup before making changes.
.PARAMETER DryRun
    Preview what would be changed without modifying files
.PARAMETER NoBackup
    Skip creating backup (not recommended)
.PARAMETER ValidateOnly
    Only check for ordering issues, don't fix them
.EXAMPLE
    ./fix-session-log-order.ps1
    Fix session log ordering with automatic backup
.EXAMPLE
    ./fix-session-log-order.ps1 -DryRun
    Preview changes without modifying files
.EXAMPLE
    ./fix-session-log-order.ps1 -ValidateOnly
    Check if log has ordering issues
#>

param(
    [switch]$DryRun,
    [switch]$NoBackup,
    [switch]$ValidateOnly
)

# Script configuration
$script:SessionLogPath = Join-Path $PSScriptRoot ".." ".." ".claude" "memory-bank" "session-log.md"
$script:BackupDir = Join-Path $PSScriptRoot ".." ".." ".claude" "memory-bank" "backups"

# Color functions
function Write-Phase($message) { 
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
    Write-Host "  $message" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
}

function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Gray }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-DryRun($message) { Write-Host "🔍 [DRY RUN] $message" -ForegroundColor Yellow }
function Write-Fix($message) { Write-Host "🔧 $message" -ForegroundColor Cyan }

# Class to represent a session log entry
class LogEntry {
    [string]$Time
    [int]$Hour
    [int]$Minute
    [int]$TotalMinutes
    [string]$Content
    [int]$OriginalIndex
    
    LogEntry([string]$time, [string]$content, [int]$index) {
        $this.Time = $time
        $this.Content = $content
        $this.OriginalIndex = $index
        
        if ($time -match '(\d{1,2}):(\d{2})') {
            $this.Hour = [int]$matches[1]
            $this.Minute = [int]$matches[2]
            $this.TotalMinutes = ($this.Hour * 60) + $this.Minute
        }
    }
}

# Parse session log into structured format
function Get-SessionLogStructure {
    param([string]$Content)
    
    $structure = @{
        Header = ""
        DateSections = @()
        Footer = ""
    }
    
    # Split content into lines
    $lines = $Content -split "`n"
    
    # States: header, date-section, entry, footer
    $state = "header"
    $currentSection = $null
    $currentEntry = @()
    $headerLines = @()
    $footerLines = @()
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        
        # Check for date section header (## YYYY-MM-DD)
        if ($line -match '^## \d{4}-\d{2}-\d{2}') {
            # Save any pending entry
            if ($currentEntry.Count -gt 0 -and $currentSection) {
                $entryText = $currentEntry -join "`n"
                if ($entryText -match '^### (\d{1,2}:\d{2})') {
                    $currentSection.Entries += [LogEntry]::new($matches[1], $entryText, $currentSection.Entries.Count)
                }
                $currentEntry = @()
            }
            
            # Save header if we haven't yet
            if ($state -eq "header" -and $headerLines.Count -gt 0) {
                $structure.Header = $headerLines -join "`n"
            }
            
            # Create new date section
            $currentSection = @{
                Date = $line
                Entries = @()
            }
            $structure.DateSections += $currentSection
            $state = "date-section"
        }
        # Check for entry header (### HH:MM - Persona)
        elseif ($line -match '^### \d{1,2}:\d{2} -') {
            # Save any pending entry
            if ($currentEntry.Count -gt 0 -and $currentSection) {
                $entryText = $currentEntry -join "`n"
                if ($entryText -match '^### (\d{1,2}:\d{2})') {
                    $currentSection.Entries += [LogEntry]::new($matches[1], $entryText, $currentSection.Entries.Count)
                }
            }
            # Start new entry
            $currentEntry = @($line)
            $state = "entry"
        }
        # Building header
        elseif ($state -eq "header") {
            $headerLines += $line
        }
        # Building entry
        elseif ($state -eq "entry") {
            $currentEntry += $line
        }
        # Check if we've hit footer content (after last entry)
        elseif ($state -eq "date-section" -and $line -match '^---$|^## Session Log Guidelines') {
            # Save any pending entry
            if ($currentEntry.Count -gt 0 -and $currentSection) {
                $entryText = $currentEntry -join "`n"
                if ($entryText -match '^### (\d{1,2}:\d{2})') {
                    $currentSection.Entries += [LogEntry]::new($matches[1], $entryText, $currentSection.Entries.Count)
                }
                $currentEntry = @()
            }
            
            # Start collecting footer
            $state = "footer"
            $footerLines = @($line)
            # Add remaining lines to footer
            for ($j = $i + 1; $j -lt $lines.Count; $j++) {
                $footerLines += $lines[$j]
            }
            $structure.Footer = $footerLines -join "`n"
            break
        }
        # Continue building current section
        else {
            if ($currentEntry.Count -gt 0) {
                $currentEntry += $line
            }
        }
    }
    
    # Save any final pending entry
    if ($currentEntry.Count -gt 0 -and $currentSection) {
        $entryText = $currentEntry -join "`n"
        if ($entryText -match '^### (\d{1,2}:\d{2})') {
            $currentSection.Entries += [LogEntry]::new($matches[1], $entryText, $currentSection.Entries.Count)
        }
    }
    
    return $structure
}

# Check if entries are in chronological order
function Test-ChronologicalOrder {
    param([LogEntry[]]$Entries)
    
    if ($Entries.Count -le 1) { return $true }
    
    for ($i = 1; $i -lt $Entries.Count; $i++) {
        if ($Entries[$i].TotalMinutes -lt $Entries[$i-1].TotalMinutes) {
            return $false
        }
    }
    return $true
}

# Sort entries chronologically
function Get-SortedEntries {
    param([LogEntry[]]$Entries)
    
    return $Entries | Sort-Object -Property TotalMinutes
}

# Rebuild session log content
function New-SessionLogContent {
    param($Structure)
    
    $parts = @()
    
    # Add header
    if ($Structure.Header) {
        $parts += $Structure.Header
    }
    
    # Add each date section with sorted entries
    foreach ($section in $Structure.DateSections) {
        $parts += $section.Date
        $parts += ""
        
        foreach ($entry in $section.SortedEntries) {
            $parts += $entry.Content
            $parts += ""
        }
    }
    
    # Add footer
    if ($Structure.Footer) {
        $parts += $Structure.Footer
    }
    
    return $parts -join "`n"
}

# Create backup of session log
function New-SessionLogBackup {
    if (-not (Test-Path $script:BackupDir)) {
        New-Item -ItemType Directory -Path $script:BackupDir -Force | Out-Null
    }
    
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupName = "session-log-backup-$timestamp.md"
    $backupPath = Join-Path $script:BackupDir $backupName
    
    Copy-Item -Path $script:SessionLogPath -Destination $backupPath -Force
    return $backupPath
}

# Main function
function Main {
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  📅 SESSION LOG CHRONOLOGICAL ORDER FIXER" -ForegroundColor White
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    
    if ($DryRun) {
        Write-Warning "Running in DRY RUN mode - no changes will be made"
    }
    
    if ($ValidateOnly) {
        Write-Info "Running in VALIDATE ONLY mode - checking for issues"
    }
    
    # Check if session log exists
    if (-not (Test-Path $script:SessionLogPath)) {
        Write-Error "Session log not found: $script:SessionLogPath"
        return
    }
    
    Write-Phase "Analyzing Session Log"
    
    # Read current content
    $currentContent = Get-Content $script:SessionLogPath -Raw
    
    # Parse structure
    $structure = Get-SessionLogStructure -Content $currentContent
    
    Write-Info "Found $($structure.DateSections.Count) date section(s)"
    
    # Check each date section
    $issuesFound = $false
    $fixedSections = 0
    
    foreach ($section in $structure.DateSections) {
        if ($section.Entries.Count -eq 0) {
            Write-Info "$($section.Date): No entries"
            $section.SortedEntries = @()
            continue
        }
        
        Write-Info "$($section.Date): $($section.Entries.Count) entries"
        
        # Check if sorting is needed
        if (-not (Test-ChronologicalOrder -Entries $section.Entries)) {
            $issuesFound = $true
            Write-Warning "  ⚠️ Entries out of chronological order!"
            
            # Show current order
            Write-Info "  Current order:"
            foreach ($entry in $section.Entries) {
                Write-Host "    - $($entry.Time) ($($entry.TotalMinutes) min)" -ForegroundColor Gray
            }
            
            # Get sorted entries
            $sorted = Get-SortedEntries -Entries $section.Entries
            
            Write-Fix "  Correct order:"
            foreach ($entry in $sorted) {
                Write-Host "    - $($entry.Time) ($($entry.TotalMinutes) min)" -ForegroundColor Green
            }
            
            $section.SortedEntries = $sorted
            $fixedSections++
        } else {
            Write-Success "  ✅ Already in chronological order"
            $section.SortedEntries = $section.Entries
        }
    }
    
    # Report findings
    Write-Phase "Analysis Results"
    
    if (-not $issuesFound) {
        Write-Success "No chronological issues found! Session log is properly ordered."
        return
    }
    
    Write-Warning "Found $fixedSections date section(s) with chronological issues"
    
    if ($ValidateOnly) {
        Write-Info "Validation complete. Use without -ValidateOnly to fix issues."
        return
    }
    
    # Apply fixes
    Write-Phase "Applying Fixes"
    
    if (-not $NoBackup) {
        if ($DryRun) {
            Write-DryRun "Would create backup of current session log"
        } else {
            $backupPath = New-SessionLogBackup
            Write-Success "Created backup: $([System.IO.Path]::GetFileName($backupPath))"
        }
    }
    
    # Generate fixed content
    $fixedContent = New-SessionLogContent -Structure $structure
    
    if ($DryRun) {
        Write-DryRun "Would rewrite session log with corrected chronological order"
        Write-Info "Preview of changes:"
        
        # Show a diff-like preview (first few entries)
        $originalLines = $currentContent -split "`n"
        $fixedLines = $fixedContent -split "`n"
        
        $diffCount = 0
        for ($i = 0; $i -lt [Math]::Min($originalLines.Count, $fixedLines.Count); $i++) {
            if ($originalLines[$i] -ne $fixedLines[$i]) {
                if ($diffCount -lt 5) {
                    Write-Host "  Line $($i+1):" -ForegroundColor Yellow
                    Write-Host "    - $($originalLines[$i])" -ForegroundColor Red
                    Write-Host "    + $($fixedLines[$i])" -ForegroundColor Green
                    $diffCount++
                }
            }
        }
        
        if ($diffCount -ge 5) {
            Write-Info "  ... and more changes"
        }
    } else {
        # Write fixed content
        Set-Content -Path $script:SessionLogPath -Value $fixedContent -Encoding UTF8
        Write-Success "Session log has been fixed!"
        Write-Info "Original backed up to: backups/"
    }
    
    # Integration suggestion
    Write-Phase "Integration Options"
    Write-Info "To prevent future issues, consider:"
    Write-Info "1. Run this check in embody.ps1:"
    Write-Host "   ./scripts/utils/fix-session-log-order.ps1 -ValidateOnly" -ForegroundColor White
    Write-Info "2. Add to git pre-commit hook for automatic validation"
    Write-Info "3. Schedule periodic checks with Memory Bank rotation"
    
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  ✅ Session log order check complete!" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

# Execute main function
Main