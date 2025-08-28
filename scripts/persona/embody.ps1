#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Embody a BlockLife persona with v4.0 Intelligent Auto-Sync
.DESCRIPTION
    Complete persona embodiment with automatic git state resolution:
    - NOW USES sync-core.psm1 MODULE FOR CONSISTENCY
    - Detects and handles squash merges automatically
    - Resolves conflicts intelligently
    - Preserves uncommitted work
    - Ensures clean persona switches every time
.PARAMETER Persona
    The persona to embody (dev-engineer, tech-lead, etc.)
.EXAMPLE
    embody tech-lead
    Embodies Tech Lead with automatic sync resolution
.NOTES
    Migrated to use sync-core.psm1: 2025-08-27
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev-engineer', 'tech-lead', 'test-specialist', 'debugger-expert', 'product-owner', 'devops-engineer')]
    [string]$Persona
)

# Capture timestamp at script start for consistent timestamps throughout (TD_078)
$scriptStartTime = Get-Date
$timestampFormatted = $scriptStartTime.ToString("yyyy-MM-dd HH:mm")

# Import sync-core module
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$gitScripts = Join-Path (Split-Path $scriptRoot) "git"
$syncModule = Join-Path $gitScripts "sync-core.psm1"
Import-Module $syncModule -Force -DisableNameChecking

# Color functions for embody-specific output (module has its own)
function Write-Phase($message) { 
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
    Write-Host "  $message" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
}

# Wrapper function for embody-specific sync behavior
function Resolve-EmbodyGitState {
    Write-Phase "Intelligent Git Sync v4.0"
    
    # Get current state
    $branch = git branch --show-current
    $hasUncommitted = [bool](git status --porcelain)
    
    # EMBODY-SPECIFIC: Include untracked files in stash
    if ($hasUncommitted) {
        Write-Warning "Stashing uncommitted changes (including untracked)..."
        $stashMessage = "embody-auto-stash-$($scriptStartTime.ToString('yyyyMMdd-HHmmss'))"
        git stash push -m $stashMessage --include-untracked
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to stash changes"
            return $false
        }
    }
    
    # Use module's sync function
    $syncResult = Sync-GitBranch -Branch $branch -Verbose:$false
    
    if ($syncResult) {
        # EMBODY-SPECIFIC: Set upstream tracking after sync
        git branch --set-upstream-to=origin/$branch $branch 2>$null
        
        # Restore stash if we created one
        if ($hasUncommitted) {
            Write-Status "Restoring stashed changes..."
            git stash pop --quiet
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "Couldn't auto-restore stash. Run 'git stash pop' manually"
            } else {
                Write-Success "Restored uncommitted changes"
            }
        }
        
        return $true
    } else {
        # Sync failed - try to restore stash before exiting
        if ($hasUncommitted) {
            Write-Warning "Attempting to restore stash after sync failure..."
            git stash pop --quiet 2>$null
        }
        return $false
    }
}

# Main embodiment flow
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  🎭 EMBODYING: $($Persona.ToUpper())" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Step 1: Intelligent Sync using module
$syncSuccess = Resolve-EmbodyGitState

if (-not $syncSuccess) {
    Write-Error "Sync failed - please resolve manually and try again"
    exit 1
}

# Step 2: Set Git Identity
Write-Phase "Setting Persona Identity"

$identities = @{
    'dev-engineer' = @('Dev Engineer', 'dev-engineer@blocklife')
    'tech-lead' = @('Tech Lead', 'tech-lead@blocklife')
    'test-specialist' = @('Test Specialist', 'test-specialist@blocklife')
    'debugger-expert' = @('Debugger Expert', 'debugger-expert@blocklife')
    'product-owner' = @('Product Owner', 'product-owner@blocklife')
    'devops-engineer' = @('DevOps Engineer', 'devops-engineer@blocklife')
}

$identity = $identities[$Persona]
git config user.name $identity[0]
git config user.email $identity[1]

Write-Success "Git identity set to: $($identity[0])"

# Step 3: Quick Reference Card
Write-Phase "Quick Reference Card"

# Define quick references per persona
$quickRefs = @{
    'dev-engineer' = @(
        "**Context7**: mcp__context7__get-library-docs '/louthy/language-ext' --topic 'Error Fin'",
        "**Glossary**: Core/shared terms: Turn, Merge, Money (not Round, Match, Credits)",
        "**Move pattern**: src/Features/Block/Move/ - copy this approach"
    )
    'tech-lead' = @(
        "**Complexity**: Rate features TD=1-2h, VS=<2d, Epic=2-6mo",
        "**ADRs**: Create in Docs/03-Reference/ADR/ for big decisions",
        "**VS Slicing**: Split stories into <2 day thin slices"
    )
    'test-specialist' = @(
        "**Categories**: Architecture | Unit | Performance | ThreadSafety",
        "**FsCheck**: QuickCheck patterns in PatternFrameworkTests.cs",
        "**Quick test**: ./scripts/test/quick.ps1 (1.3s architecture only)"
    )
    'debugger-expert' = @(
        "**Post-Mortems**: Create in Docs/06-PostMortems/Inbox/",
        "**Root Cause**: Always ask 'why' 5 times to find real cause",
        "**Reflog**: git reflog to recover lost commits"
    )
    'product-owner' = @(
        "**Backlog**: VS=Vision, BR=Bug, TD=Tech Debt",
        "**Game Design**: Sacred docs in Docs/02-Design/Game/",
        "**Priority**: Critical (blockers) > Important (features) > Ideas"
    )
    'devops-engineer' = @(
        "**Build & Test**: ./scripts/core/build.ps1 test - mandatory before commit",
        "**Git Sync**: git sync or pr sync - handles squash merges automatically",
        "**CI Pipeline**: .github/workflows/ci.yml - runs on PR and main push"
    )
}

Write-Info "Top 3 Quick References for $($identity[0]):"
$refs = $quickRefs[$Persona]
for ($i = 0; $i -lt $refs.Count; $i++) {
    Write-Host "  $($i+1). $($refs[$i])" -ForegroundColor White
}

$personaFile = Join-Path $PSScriptRoot "..\..\" "Docs" "04-Personas" "$Persona.md"
if (Test-Path $personaFile) {
    Write-Host ""
    Write-Host "  📖 Full reference card in: Docs/04-Personas/$Persona.md" -ForegroundColor Gray
}

# Step 4: Critical Reminders (Persona-Specific)
Write-Phase "Critical Reminders for $($identity[0])"

# Define critical reminders per persona
$criticalReminders = @{
    'dev-engineer' = @{
        Title = "🔬 Context7 & Patterns:"
        Points = @(
            "🎯 Context7 First:",
            "  • ALWAYS check LanguageExt docs before using Fin/Option/Error",
            "  • Use: mcp__context7__get-library-docs '/louthy/language-ext'",
            "",
            "📐 Pattern Copying:",
            "  • Move pattern: src/Features/Block/Move/",
            "  • Copy folder structure, naming, testing approach",
            "",
            "⚠️ Remember:",
            "  • Run ./scripts/core/build.ps1 test before commit",
            "  • Use GLOSSARY terms (Turn not Round, Merge not Match)"
        )
    }
    'tech-lead' = @{
        Title = "🏗️ Architecture & Complexity:"
        Points = @(
            "📊 Complexity Scoring:",
            "  • TD: 1-2 hours implementation",
            "  • VS: <2 days (or slice it thinner!)",
            "  • Epic: 2-6 months",
            "",
            "🔪 Story Slicing:",
            "  • Every VS must have <2 day slices",
            "  • Vertical slices (UI to DB)",
            "  • Each slice independently valuable",
            "",
            "📝 ADR Protocol:",
            "  • Major decisions need ADR",
            "  • Location: Docs/03-Reference/ADR/",
            "  • Include: Context, Decision, Consequences"
        )
    }
    'test-specialist' = @{
        Title = "🧪 Testing Excellence:"
        Points = @(
            "🏃 Test Categories (TD_071):",
            "  • Architecture: Structure & conventions (1.3s)",
            "  • Unit: Business logic (20s)",
            "  • Performance: Speed requirements",
            "  • ThreadSafety: Concurrency",
            "",
            "⚡ Quick Testing:",
            "  • ./scripts/test/quick.ps1 - Architecture only (1.3s)",
            "  • ./scripts/test/full.ps1 - All categories staged",
            "",
            "🎲 FsCheck Patterns:",
            "  • See: PatternFrameworkTests.cs",
            "  • Property-based testing for complex logic"
        )
    }
    'debugger-expert' = @{
        Title = "🔍 Debugging Protocol:"
        Points = @(
            "🎯 Root Cause Analysis:",
            "  • Ask 'why' 5 times",
            "  • Surface: What you see",
            "  • Root: What actually caused it",
            "  • Lesson: What to prevent next time",
            "",
            "📝 Post-Mortem Creation:",
            "  • Location: Docs/06-PostMortems/Inbox/",
            "  • Include: Timeline, Impact, 5-Whys, Prevention",
            "  • Focus on learning, not blame",
            "",
            "🔄 Recovery Tools:",
            "  • git reflog - recover lost commits",
            "  • git fsck - find dangling objects"
        )
    }
    'product-owner' = @{
        Title = "📋 Product Ownership:"
        Points = @(
            "🎮 Game Design is Sacred:",
            "  • Docs/02-Design/Game/ = source of truth",
            "  • You are the Game Designer",
            "  • Vision drives all decisions",
            "",
            "📊 Backlog Management:",
            "  • VS = Vision/Story (<2 days)",
            "  • BR = Bug Report",
            "  • TD = Tech Debt (1-2 hours)",
            "",
            "🎯 Priority Framework:",
            "  • Critical: Blockers, data loss, crashes",
            "  • Important: Core features, major bugs",
            "  • Ideas: Nice-to-have, future vision"
        )
    }
    'devops-engineer' = @{
        Title = "🤖 Zero-Friction Automation Standards:"
        Points = @(
            "🎯 Automation Criteria:",
            "  • Happens twice = Automate it",
            "  • Causes friction = Eliminate it",
            "  • Takes >15min/week = Script it",
            "",
            "✨ Script Excellence:",
            "  • Silent operation is best (use *>)",
            "  • Idempotent (safe to run multiple times)",
            "  • Self-documenting progress messages",
            "",
            "📊 Current Metrics:",
            "  • Build time: 2-3 min (target <5 min)",
            "  • Pre-commit: <0.5s",
            "  • Automation saves: ~60 min/month"
        )
    }
}

$reminder = $criticalReminders[$Persona]
Write-Host $reminder.Title -ForegroundColor Yellow
Write-Host ""
foreach ($point in $reminder.Points) {
    Write-Host "  $point" -ForegroundColor $(if ($point -match '^\s*[•📊🎯⚡🏃🔪🎲🔄📝🎮✨]') { "White" } else { "Gray" })
}
Write-Host ""

# Step 5: Final Status
Write-Phase "Ready to Work!"

$currentBranch = git branch --show-current
Write-Host ""
Write-Host "📍 Current branch: $currentBranch" -ForegroundColor Cyan

# Check for uncommitted changes
$status = git status --short
if ($status) {
    Write-Host "📝 You have uncommitted changes" -ForegroundColor Yellow
} else {
    Write-Host "✨ Working directory clean" -ForegroundColor Green
}

Write-Host ""
Write-Host "🎭 You are now: $($identity[0])" -ForegroundColor Yellow
Write-Host ""
Write-Host "💡 Tip: Your working branch is ready" -ForegroundColor Gray
Write-Host "   Commit frequently to prevent conflicts" -ForegroundColor Gray

# Success footer
Write-Host ""
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  ✅ $($identity[0]) embodiment complete!" -ForegroundColor Green  
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""