#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Auto-fix common development issues with zero friction
.DESCRIPTION
    Detects and automatically fixes common problems developers encounter.
    Designed to eliminate manual toil and provide helpful solutions.
    
    This script embodies the DevOps philosophy: If it can be fixed automatically, it should be.
.PARAMETER Issue
    Specific issue to fix. If not specified, runs diagnostic and fixes all found issues.
.PARAMETER Check
    Only diagnose issues without fixing them
.EXAMPLE
    ./scripts/fix/common-issues.ps1
    Auto-detect and fix all issues
.EXAMPLE
    ./scripts/fix/common-issues.ps1 -Issue build
    Fix build-related issues specifically
.EXAMPLE
    ./scripts/fix/common-issues.ps1 -Check
    Diagnose issues without fixing
#>

param(
    [ValidateSet('all', 'build', 'format', 'packages', 'git', 'hooks', 'env')]
    [string]$Issue = 'all',
    [switch]$Check
)

$ErrorActionPreference = "Continue"  # Keep going even if one fix fails
$script:FixCount = 0
$script:IssueCount = 0

# Color functions
function Write-Phase($message) { 
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
    Write-Host "  $message" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Magenta
}

function Write-Success($message) { Write-Host "✅ $message" -ForegroundColor Green }
function Write-Warning($message) { Write-Host "⚠️  $message" -ForegroundColor Yellow }
function Write-Info($message) { Write-Host "ℹ️  $message" -ForegroundColor Gray }
function Write-Error($message) { Write-Host "❌ $message" -ForegroundColor Red }
function Write-Fix($message) { Write-Host "🔧 $message" -ForegroundColor Cyan }

# Helper to execute fixes
function Invoke-Fix {
    param(
        [string]$Description,
        [scriptblock]$FixAction,
        [scriptblock]$CheckAction = { $true }
    )
    
    $script:IssueCount++
    
    if ($Check) {
        Write-Warning "Found: $Description"
        return
    }
    
    Write-Fix "Fixing: $Description"
    
    try {
        & $FixAction
        
        # Verify fix worked
        if (& $CheckAction) {
            Write-Success "Fixed: $Description"
            $script:FixCount++
        } else {
            Write-Warning "Partial fix: $Description (may need manual intervention)"
        }
    } catch {
        Write-Error "Failed to fix: $Description - $_"
    }
}

Write-Phase "🔧 Common Issues Auto-Fixer"

# Fix 1: Missing or outdated packages
if ($Issue -eq 'all' -or $Issue -eq 'packages') {
    Write-Info "Checking package dependencies..."
    
    if (-not (Test-Path "src/Darklands.Core.csproj")) {
        Invoke-Fix "Missing Core project file" {
            Write-Error "Core project structure is missing - needs manual setup"
        }
    } else {
        # Check for package restore issues
        $restoreOutput = dotnet restore src/Darklands.Core.csproj 2>&1
        if ($LASTEXITCODE -ne 0) {
            Invoke-Fix "Package restore needed" {
                dotnet restore src/Darklands.Core.csproj --force
                dotnet restore tests/Darklands.Core.Tests.csproj --force
                dotnet restore Darklands.csproj --force
            } {
                (dotnet restore src/Darklands.Core.csproj --verbosity quiet 2>&1) -and $LASTEXITCODE -eq 0
            }
        }
    }
}

# Fix 2: Build issues
if ($Issue -eq 'all' -or $Issue -eq 'build') {
    Write-Info "Checking build health..."
    
    # Clean stale build artifacts
    $staleBuildDirs = Get-ChildItem -Path . -Include bin,obj -Recurse -Directory 2>$null
    if ($staleBuildDirs.Count -gt 0) {
        Invoke-Fix "Stale build artifacts found ($($staleBuildDirs.Count) directories)" {
            $staleBuildDirs | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
            Write-Info "Cleaned bin/obj directories"
        } {
            (Get-ChildItem -Path . -Include bin,obj -Recurse -Directory 2>$null).Count -eq 0
        }
    }
    
    # Fix common MSBuild issues
    $buildTest = dotnet build src/Darklands.Core.csproj --no-restore --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Invoke-Fix "Build failing - attempting fresh rebuild" {
            # Clear NuGet cache for this project
            dotnet nuget locals temp -c | Out-Null
            
            # Restore and rebuild
            dotnet restore src/Darklands.Core.csproj --force | Out-Null
            dotnet build src/Darklands.Core.csproj --no-incremental | Out-Null
        } {
            $test = dotnet build src/Darklands.Core.csproj --no-restore --verbosity quiet 2>&1
            $LASTEXITCODE -eq 0
        }
    }
}

# Fix 3: Code formatting
if ($Issue -eq 'all' -or $Issue -eq 'format') {
    Write-Info "Checking code formatting..."
    
    $formatCheck = dotnet format src/Darklands.Core.csproj --verify-no-changes --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Invoke-Fix "Code formatting issues" {
            dotnet format src/Darklands.Core.csproj --verbosity quiet
            dotnet format tests/Darklands.Core.Tests.csproj --verbosity quiet
            Write-Info "Applied automatic formatting"
        } {
            $test = dotnet format src/Darklands.Core.csproj --verify-no-changes --verbosity quiet 2>&1
            $LASTEXITCODE -eq 0
        }
    }
}

# Fix 4: Git issues
if ($Issue -eq 'all' -or $Issue -eq 'git') {
    Write-Info "Checking git state..."
    
    # Check for detached HEAD
    $gitStatus = git symbolic-ref HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        Invoke-Fix "Detached HEAD state" {
            # Get the branch that was likely intended
            $branch = git branch --show-current
            if (-not $branch) {
                $branch = "main"
            }
            git checkout $branch 2>$null
        } {
            git symbolic-ref HEAD 2>$null
            $LASTEXITCODE -eq 0
        }
    }
    
    # Check for uncommitted .orig files (merge artifacts)
    $origFiles = git status --porcelain | Where-Object { $_ -match '\.orig$' }
    if ($origFiles) {
        Invoke-Fix "Merge conflict artifacts (.orig files)" {
            Get-ChildItem -Path . -Filter "*.orig" -Recurse | Remove-Item -Force
            Write-Info "Removed .orig files"
        } {
            -not (git status --porcelain | Where-Object { $_ -match '\.orig$' })
        }
    }
    
    # Fix line ending issues (CRLF/LF)
    if ($IsWindows -or $env:OS -match "Windows") {
        $lineEndingIssue = git diff --check 2>$null | Select-String "trailing whitespace"
        if ($lineEndingIssue) {
            Invoke-Fix "Line ending or whitespace issues" {
                # Configure git to handle line endings
                git config core.autocrlf true
                
                # Fix existing files
                $files = git diff --name-only
                foreach ($file in $files) {
                    if (Test-Path $file) {
                        # Remove trailing whitespace
                        $content = Get-Content $file -Raw
                        $fixed = $content -replace '[ \t]+$', '' -replace '[ \t]+\r?\n', "`n"
                        Set-Content $file $fixed -NoNewline
                    }
                }
            }
        }
    }
}

# Fix 5: Git hooks
if ($Issue -eq 'all' -or $Issue -eq 'hooks') {
    Write-Info "Checking git hooks..."
    
    # Ensure Husky is installed
    if (-not (Test-Path ".husky/_/husky.sh")) {
        Invoke-Fix "Husky not initialized" {
            if (Get-Command npm -ErrorAction SilentlyContinue) {
                npx husky install 2>$null
            } elseif (Test-Path ".husky") {
                Write-Warning "Husky directory exists but not initialized - may need manual setup"
            }
        } {
            Test-Path ".husky/_/husky.sh"
        }
    }
    
    # Make hooks executable (Unix/Mac compatibility)
    if (-not $IsWindows) {
        $hooks = Get-ChildItem ".husky" -File | Where-Object { $_.Name -ne "husky.sh" }
        foreach ($hook in $hooks) {
            if (-not (Test-Path $hook.FullName -PathType Leaf)) { continue }
            
            $isExecutable = (Get-Item $hook.FullName).Mode -match 'x'
            if (-not $isExecutable) {
                Invoke-Fix "Hook not executable: $($hook.Name)" {
                    chmod +x $hook.FullName
                }
            }
        }
    }
}

# Fix 6: Environment setup
if ($Issue -eq 'all' -or $Issue -eq 'env') {
    Write-Info "Checking development environment..."
    
    # Check .NET SDK
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Error ".NET SDK not found - please install from https://dotnet.microsoft.com/download"
        $script:IssueCount++
    } elseif ($dotnetVersion -lt "8.0") {
        Write-Warning ".NET SDK version $dotnetVersion found - recommend upgrading to 8.0+"
        $script:IssueCount++
    }
    
    # Check Godot installation
    $godotInstalled = Get-Command godot -ErrorAction SilentlyContinue
    if (-not $godotInstalled) {
        Write-Warning "Godot not found in PATH - you won't be able to run the game"
        Write-Info "Download from: https://godotengine.org/download/windows/"
        $script:IssueCount++
    }
    
    # Check PowerShell version
    if ($PSVersionTable.PSVersion.Major -lt 7) {
        Write-Warning "PowerShell $($PSVersionTable.PSVersion) detected - recommend PowerShell 7+"
        Write-Info "Install: winget install Microsoft.PowerShell"
        $script:IssueCount++
    }
}

# Summary
Write-Phase "Auto-Fix Summary"

if ($Check) {
    if ($script:IssueCount -eq 0) {
        Write-Success "No issues found! Your environment is healthy."
    } else {
        Write-Warning "Found $($script:IssueCount) issue(s)"
        Write-Info "Run without -Check flag to auto-fix these issues"
    }
} else {
    if ($script:FixCount -eq 0 -and $script:IssueCount -eq 0) {
        Write-Success "No issues found! Your environment is healthy."
    } elseif ($script:FixCount -eq $script:IssueCount) {
        Write-Success "Successfully fixed all $($script:FixCount) issue(s)!"
    } else {
        Write-Warning "Fixed $($script:FixCount) of $($script:IssueCount) issue(s)"
        if ($script:FixCount -lt $script:IssueCount) {
            Write-Info "Some issues require manual intervention - check warnings above"
        }
    }
}

# Suggest next steps
if ($script:FixCount -gt 0) {
    Write-Host ""
    Write-Info "Recommended next steps:"
    Write-Host "  1. Run './scripts/core/build.ps1 test' to verify fixes"
    Write-Host "  2. Commit these improvements"
    Write-Host "  3. Continue with your work - friction eliminated! ✨"
}

exit ($script:IssueCount - $script:FixCount)