#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Automated environment verification and setup for Darklands development
.DESCRIPTION
    Comprehensive environment check that validates all prerequisites, installs missing
    components where possible, and ensures the development environment is ready.
    
    This script embodies zero-friction philosophy: fix what can be fixed automatically,
    provide exact instructions for what can't.
.PARAMETER Fix
    Attempt to automatically fix issues where possible
.PARAMETER SkipOptional
    Skip optional dependency checks (Godot, GitHub CLI)
.EXAMPLE
    ./scripts/setup/verify-environment.ps1
    Full environment verification with auto-fix
.EXAMPLE
    ./scripts/setup/verify-environment.ps1 -SkipOptional
    Check only required dependencies
#>

param(
    [switch]$Fix = $true,
    [switch]$SkipOptional
)

$ErrorActionPreference = "Continue"
$script:ChecksPassed = 0
$script:ChecksFailed = 0
$script:FixesApplied = 0

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

function Test-Requirement {
    param(
        [string]$Name,
        [scriptblock]$TestAction,
        [scriptblock]$FixAction = $null,
        [string]$FixInstructions = "",
        [switch]$Optional
    )
    
    Write-Host "`n🔍 Checking $Name..." -ForegroundColor Gray
    
    $result = & $TestAction
    
    if ($result.Success) {
        Write-Success "$($result.Message)"
        $script:ChecksPassed++
        return $true
    } else {
        if ($Optional) {
            Write-Warning "$($result.Message)"
        } else {
            Write-Error "$($result.Message)"
            $script:ChecksFailed++
        }
        
        if ($Fix -and $FixAction) {
            Write-Fix "Attempting to fix..."
            $fixResult = & $FixAction
            if ($fixResult.Success) {
                Write-Success "Fixed: $($fixResult.Message)"
                $script:FixesApplied++
                $script:ChecksPassed++
                return $true
            } else {
                Write-Warning "Auto-fix failed: $($fixResult.Message)"
            }
        }
        
        if ($FixInstructions) {
            Write-Info "To fix manually: $FixInstructions"
        }
        
        return $false
    }
}

# Main verification
Write-Host "🎮 Darklands Development Environment Verification" -ForegroundColor Cyan
Write-Host "Checking prerequisites and setting up zero-friction development..." -ForegroundColor Gray

# Check 1: .NET SDK
Test-Requirement -Name ".NET SDK 8.0+" -TestAction {
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0 -and $dotnetVersion) {
            $version = [Version]($dotnetVersion.Split('-')[0])
            if ($version.Major -ge 8) {
                return @{ Success = $true; Message = ".NET SDK $dotnetVersion installed" }
            } else {
                return @{ Success = $false; Message = ".NET SDK $dotnetVersion found, but version 8.0+ required" }
            }
        } else {
            return @{ Success = $false; Message = ".NET SDK not found" }
        }
    } catch {
        return @{ Success = $false; Message = ".NET SDK not found or not in PATH" }
    }
} -FixAction {
    try {
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            winget install Microsoft.DotNet.SDK.8 --silent --accept-package-agreements --accept-source-agreements
            # Refresh environment
            $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
            
            # Test again
            $dotnetVersion = dotnet --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                return @{ Success = $true; Message = ".NET SDK installed via winget" }
            } else {
                return @{ Success = $false; Message = "Installation completed but dotnet not in PATH" }
            }
        } else {
            return @{ Success = $false; Message = "winget not available for automatic installation" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to install .NET SDK: $_" }
    }
} -FixInstructions "Download from https://dotnet.microsoft.com/download/dotnet/8.0"

# Check 2: Git
Test-Requirement -Name "Git" -TestAction {
    try {
        $gitVersion = git --version 2>$null
        if ($LASTEXITCODE -eq 0 -and $gitVersion) {
            return @{ Success = $true; Message = "$gitVersion installed" }
        } else {
            return @{ Success = $false; Message = "Git not found" }
        }
    } catch {
        return @{ Success = $false; Message = "Git not found or not in PATH" }
    }
} -FixAction {
    try {
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            winget install Git.Git --silent --accept-package-agreements --accept-source-agreements
            # Refresh environment
            $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("PATH", "User")
            
            # Test again
            $gitVersion = git --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                return @{ Success = $true; Message = "Git installed via winget" }
            } else {
                return @{ Success = $false; Message = "Installation completed but git not in PATH" }
            }
        } else {
            return @{ Success = $false; Message = "winget not available for automatic installation" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to install Git: $_" }
    }
} -FixInstructions "Download from https://git-scm.com/"

# Check 3: PowerShell Version
Test-Requirement -Name "PowerShell 7+" -Optional:$SkipOptional -TestAction {
    $psVersion = $PSVersionTable.PSVersion
    if ($psVersion.Major -ge 7) {
        return @{ Success = $true; Message = "PowerShell $psVersion installed" }
    } else {
        return @{ Success = $false; Message = "PowerShell $psVersion found, recommend 7+ for best experience" }
    }
} -FixInstructions "Install with: winget install Microsoft.PowerShell"

# Check 4: Project Structure
Test-Requirement -Name "Project Structure" -TestAction {
    $requiredPaths = @(
        "src/Darklands.Core.csproj",
        "tests/Darklands.Core.Tests.csproj",
        "Darklands.csproj",
        "scripts/core/build.ps1",
        ".husky"
    )
    
    $missing = @()
    foreach ($path in $requiredPaths) {
        if (-not (Test-Path $path)) {
            $missing += $path
        }
    }
    
    if ($missing.Count -eq 0) {
        return @{ Success = $true; Message = "All required project files found" }
    } else {
        return @{ Success = $false; Message = "Missing project files: $($missing -join ', ')" }
    }
} -FixInstructions "Ensure you're in the correct Darklands project directory"

# Check 5: NuGet Packages
Test-Requirement -Name "NuGet Package Restore" -TestAction {
    try {
        dotnet restore src/Darklands.Core.csproj --verbosity quiet
        if ($LASTEXITCODE -eq 0) {
            return @{ Success = $true; Message = "NuGet packages restored successfully" }
        } else {
            return @{ Success = $false; Message = "Package restore failed" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to restore packages: $_" }
    }
} -FixAction {
    try {
        Write-Info "Clearing NuGet cache and retrying..."
        dotnet nuget locals all --clear
        dotnet restore --force
        if ($LASTEXITCODE -eq 0) {
            return @{ Success = $true; Message = "Packages restored after cache clear" }
        } else {
            return @{ Success = $false; Message = "Restore still failing" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to restore packages: $_" }
    }
}

# Check 6: Git Hooks
Test-Requirement -Name "Git Hooks (Husky)" -TestAction {
    if (Test-Path ".husky/_/husky.sh") {
        return @{ Success = $true; Message = "Husky hooks installed" }
    } else {
        return @{ Success = $false; Message = "Git hooks not installed" }
    }
} -FixAction {
    try {
        dotnet tool restore
        dotnet husky install
        if (Test-Path ".husky/_/husky.sh") {
            return @{ Success = $true; Message = "Husky hooks installed successfully" }
        } else {
            return @{ Success = $false; Message = "Hook installation failed" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to install hooks: $_" }
    }
}

# Check 7: Build System
Test-Requirement -Name "Build System" -TestAction {
    try {
        dotnet build src/Darklands.Core.csproj --verbosity quiet --no-restore
        if ($LASTEXITCODE -eq 0) {
            return @{ Success = $true; Message = "Core project builds successfully" }
        } else {
            return @{ Success = $false; Message = "Build failed" }
        }
    } catch {
        return @{ Success = $false; Message = "Build system error: $_" }
    }
} -FixAction {
    try {
        Write-Info "Cleaning and rebuilding..."
        dotnet clean
        dotnet build src/Darklands.Core.csproj --no-restore
        if ($LASTEXITCODE -eq 0) {
            return @{ Success = $true; Message = "Build successful after clean" }
        } else {
            return @{ Success = $false; Message = "Build still failing" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to fix build: $_" }
    }
}

# Check 8: Test Suite
Test-Requirement -Name "Test Suite" -TestAction {
    try {
        dotnet test tests/Darklands.Core.Tests.csproj --verbosity quiet --no-build --no-restore
        if ($LASTEXITCODE -eq 0) {
            return @{ Success = $true; Message = "All tests passing" }
        } else {
            return @{ Success = $false; Message = "Some tests failing" }
        }
    } catch {
        return @{ Success = $false; Message = "Test execution error: $_" }
    }
} -FixAction {
    try {
        Write-Info "Rebuilding and retesting..."
        dotnet build tests/Darklands.Core.Tests.csproj --no-restore
        dotnet test tests/Darklands.Core.Tests.csproj --verbosity normal --no-build
        if ($LASTEXITCODE -eq 0) {
            return @{ Success = $true; Message = "Tests passing after rebuild" }
        } else {
            return @{ Success = $false; Message = "Tests still failing - may need manual fix" }
        }
    } catch {
        return @{ Success = $false; Message = "Failed to fix tests: $_" }
    }
}

# Optional Checks
if (-not $SkipOptional) {
    # Check 9: Godot Engine
    Test-Requirement -Name "Godot Engine 4.4+" -Optional -TestAction {
        try {
            $godotVersion = godot --version 2>$null
            if ($LASTEXITCODE -eq 0 -and $godotVersion) {
                if ($godotVersion -match "4\.4") {
                    return @{ Success = $true; Message = "Godot $godotVersion installed" }
                } else {
                    return @{ Success = $false; Message = "Godot $godotVersion found, but 4.4.1+ recommended" }
                }
            } else {
                return @{ Success = $false; Message = "Godot not found in PATH" }
            }
        } catch {
            return @{ Success = $false; Message = "Godot not installed or not in PATH" }
        }
    } -FixInstructions "Download from https://godotengine.org/download/windows/ and add to PATH"
    
    # Check 10: GitHub CLI
    Test-Requirement -Name "GitHub CLI" -Optional -TestAction {
        try {
            $ghVersion = gh --version 2>$null
            if ($LASTEXITCODE -eq 0 -and $ghVersion) {
                return @{ Success = $true; Message = "GitHub CLI installed" }
            } else {
                return @{ Success = $false; Message = "GitHub CLI not found" }
            }
        } catch {
            return @{ Success = $false; Message = "GitHub CLI not installed" }
        }
    } -FixInstructions "Install with: winget install GitHub.cli"
}

# Final Summary
Write-Phase "Environment Verification Complete"

$totalChecks = $script:ChecksPassed + $script:ChecksFailed
Write-Host "📊 Results: $($script:ChecksPassed)/$totalChecks checks passed" -ForegroundColor $(if ($script:ChecksFailed -eq 0) { 'Green' } else { 'Yellow' })

if ($script:FixesApplied -gt 0) {
    Write-Success "Applied $($script:FixesApplied) automatic fixes"
}

if ($script:ChecksFailed -eq 0) {
    Write-Phase "🎉 Setup Complete!"
    Write-Success "Your Darklands development environment is ready!"
    Write-Host ""
    Write-Info "Next steps:"
    Write-Host "  1. Run: ./scripts/core/build.ps1 test"
    Write-Host "  2. Read: Docs/03-Reference/HANDBOOK.md"
    Write-Host "  3. Embody a persona: ./scripts/persona/embody.ps1 dev-engineer"
    Write-Host "  4. Start coding with zero friction! 🚀"
    
    exit 0
} else {
    Write-Warning "Setup incomplete - $($script:ChecksFailed) issues need attention"
    Write-Host ""
    Write-Info "To fix remaining issues:"
    Write-Host "  1. Follow the manual instructions above"
    Write-Host "  2. Re-run this script: ./scripts/setup/verify-environment.ps1"
    Write-Host "  3. Or run the auto-fixer: ./scripts/fix/common-issues.ps1"
    
    exit 1
}