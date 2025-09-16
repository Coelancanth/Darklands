#!/usr/bin/env pwsh
# Darklands Build Script - Enhanced for Developer Experience
# Zero-friction automation for build, test, and development workflows

param(
    [Parameter(Position=0)]
    [ValidateSet('build', 'test', 'test-only', 'clean', 'run', 'all', 'help')]
    [string]$Command = 'help',
    
    # Test filtering support
    [Parameter(Position=1)]
    [string]$Filter,
    
    # Common flags
    [switch]$Release,
    [switch]$Detailed,
    [switch]$NoBuild,
    [switch]$Coverage,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"

# PROJECT CONFIGURATION - DARKLANDS
$ProjectName = "Darklands"
$SolutionFile = "Darklands.sln"
$DomainProject = "src/Darklands.Domain/Darklands.Domain.csproj"
$ApplicationProject = "src/Darklands.Application.csproj"
$PresentationProject = "src/Darklands.Presentation/Darklands.Presentation.csproj"
$TestProject = "tests/Darklands.Core.Tests.csproj"
$GodotProject = "Darklands.csproj"
$BuildCommand = "dotnet build"
$TestCommand = "dotnet test"
$CleanCommand = "dotnet clean"

function Write-Step {
    param([string]$Message)
    Write-Host "`n→ $Message" -ForegroundColor Cyan
}

function Execute-Command {
    param([string]$Cmd)
    if ($Detailed) {
        Write-Host "  $Cmd" -ForegroundColor Gray
    }
    Invoke-Expression $Cmd
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Command failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

function Show-Help {
    Write-Host "`n🎯 Darklands Build Script" -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  ./build.ps1 <command> [filter] [flags]" -ForegroundColor White
    Write-Host ""
    Write-Host "COMMANDS:" -ForegroundColor Yellow
    Write-Host "  build          Build all projects (Core + Tests + Godot)" -ForegroundColor White
    Write-Host "  test           Build + run all tests (recommended before commit)" -ForegroundColor White
    Write-Host "  test-only      Run tests without building (fast iteration)" -ForegroundColor White
    Write-Host "  clean          Clean all build artifacts" -ForegroundColor White
    Write-Host "  run            Launch Godot editor" -ForegroundColor White
    Write-Host "  all            Clean + Build + Test (full validation)" -ForegroundColor White
    Write-Host "  help           Show this help" -ForegroundColor White
    Write-Host ""
    Write-Host "TEST FILTERS:" -ForegroundColor Yellow
    Write-Host "  Category=Architecture    Run architecture tests only" -ForegroundColor Cyan
    Write-Host "  Category=CrossPlatform   Run cross-platform determinism tests" -ForegroundColor Cyan
    Write-Host "  Category=PropertyBased   Run property-based tests" -ForegroundColor Cyan
    Write-Host "  Category=Phase1          Run Phase 1 tests (Domain)" -ForegroundColor Cyan
    Write-Host "  *DeterministicRandom*    Run all deterministic random tests" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "FLAGS:" -ForegroundColor Yellow
    Write-Host "  -Release       Use Release configuration" -ForegroundColor White
    Write-Host "  -Detailed      Show detailed output" -ForegroundColor White
    Write-Host "  -NoBuild       Skip build step (test commands only)" -ForegroundColor White
    Write-Host "  -Coverage      Collect code coverage" -ForegroundColor White
    Write-Host ""
    Write-Host "EXAMPLES:" -ForegroundColor Yellow
    Write-Host "  ./build.ps1 test" -ForegroundColor Cyan
    Write-Host "    Build and run all tests" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  ./build.ps1 test ""Category=Architecture""" -ForegroundColor Cyan
    Write-Host "    Run only architecture tests" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  ./build.ps1 test-only ""*DeterministicRandom*"" -Detailed" -ForegroundColor Cyan
    Write-Host "    Run deterministic random tests with detailed output" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  ./build.ps1 test ""Category=CrossPlatform"" -Coverage" -ForegroundColor Cyan
    Write-Host "    Run cross-platform tests with coverage collection" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "💡 TIP: For ultra-fast architecture validation, use:" -ForegroundColor Yellow
    Write-Host "    ../test/quick.ps1" -ForegroundColor Cyan
    Write-Host ""
}

# Build configuration
$Configuration = if ($Release) { "Release" } else { "Debug" }
$VerbosityLevel = if ($Detailed) { "normal" } else { "minimal" }

switch ($Command) {
    'help' {
        Show-Help
        return
    }
    
    'clean' {
        Write-Step "Cleaning build artifacts"
        Execute-Command "$CleanCommand $SolutionFile"
        Execute-Command "$CleanCommand $TestProject"
        Execute-Command "$CleanCommand $GodotProject"
        # Clean bin and obj directories
        Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✓ Clean complete" -ForegroundColor Green
    }

    'build' {
        Write-Step "Building $ProjectName ($Configuration)"
        Execute-Command "$BuildCommand $SolutionFile --configuration $Configuration"
        Execute-Command "$BuildCommand $TestProject --configuration $Configuration"
        Execute-Command "$BuildCommand $GodotProject --configuration $Configuration"
        Write-Host "✓ Build successful" -ForegroundColor Green
    }

    'test' {
        $FilterText = if ($Filter) { " with filter: $Filter" } else { "" }
        Write-Step "Building and running tests$FilterText"
        
        if (-not $NoBuild) {
            if ($Detailed) { Write-Host "  Building projects..." -ForegroundColor Yellow }
            Execute-Command "$BuildCommand $SolutionFile --configuration $Configuration"
            Execute-Command "$BuildCommand $TestProject --configuration $Configuration"
            Execute-Command "$BuildCommand $GodotProject --configuration $Configuration"
            Write-Host "✓ Build successful" -ForegroundColor Green
        }
        
        Write-Step "Running tests"
        $TestArgs = "$TestProject --configuration $Configuration --verbosity $VerbosityLevel"
        
        if ($Filter) {
            $TestArgs += " --filter ""$Filter"""
        }
        
        if ($Coverage) {
            $TestArgs += " --collect:""XPlat Code Coverage"" --results-directory ./TestResults"
        }
        
        if ($NoBuild) {
            $TestArgs += " --no-build"
        }
        
        Execute-Command "$TestCommand $TestArgs"
        Write-Host "✓ Test execution complete" -ForegroundColor Green
        
        if (-not $Filter) {
            Write-Host "  💡 Safe to commit!" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️  Filtered tests only - run full test suite before commit" -ForegroundColor Yellow
        }
        
        if ($Coverage) {
            Write-Host "  📊 Coverage reports in ./TestResults/" -ForegroundColor Cyan
        }
    }

    'test-only' {
        $FilterText = if ($Filter) { " with filter: $Filter" } else { "" }
        Write-Step "Running tests only$FilterText (fast iteration)"
        Write-Host "  ⚠️  Note: This doesn't validate Godot compilation" -ForegroundColor Yellow
        
        $TestArgs = "$TestProject --configuration $Configuration --verbosity $VerbosityLevel --no-build"
        
        if ($Filter) {
            $TestArgs += " --filter ""$Filter"""
        }
        
        if ($Coverage) {
            $TestArgs += " --collect:""XPlat Code Coverage"" --results-directory ./TestResults"
        }
        
        Execute-Command "$TestCommand $TestArgs"
        Write-Host "✓ Tests passed" -ForegroundColor Green
        
        if (-not $Filter) {
            Write-Host "  Remember to run 'test' (not 'test-only') before committing!" -ForegroundColor Yellow
        } else {
            Write-Host "  ⚠️  Filtered tests only - run full test suite before commit" -ForegroundColor Yellow
        }
        
        Write-Host "  💡 Tip: Use ../test/quick.ps1 for ultra-fast architecture validation" -ForegroundColor DarkGray
    }

    'run' {
        Write-Step "Running $ProjectName ($Configuration)"
        Write-Host "  Note: This requires Godot to be installed" -ForegroundColor Yellow
        if (Get-Command godot -ErrorAction SilentlyContinue) {
            # Build Core library first
            Execute-Command "$BuildCommand $SolutionFile --configuration $Configuration"
            # Launch Godot
            Execute-Command "godot project.godot"
        } else {
            Write-Host "✗ Godot not found in PATH" -ForegroundColor Red
            Write-Host "  Please install Godot 4.4.1 or add it to your PATH" -ForegroundColor Yellow
            Write-Host "  Download from: https://godotengine.org/download" -ForegroundColor Cyan
        }
    }

    'all' {
        & $PSCommandPath clean
        & $PSCommandPath build
        & $PSCommandPath test
        Write-Host "`n✓ All steps completed successfully" -ForegroundColor Green
    }
}
