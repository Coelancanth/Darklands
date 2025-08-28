#!/usr/bin/env pwsh
# Generic Build Script
# Customize the commands below for your project

param(
    [Parameter(Position=0)]
    [ValidateSet('build', 'test', 'test-only', 'clean', 'run', 'all')]
    [string]$Command = 'build'
)

$ErrorActionPreference = "Stop"

# PROJECT CONFIGURATION - CUSTOMIZE THESE
$ProjectName = "YourProject"  # TODO: Update with your project name
$SolutionFile = "YourProject.sln"  # TODO: Update with your solution/project file
$BuildCommand = "dotnet build"  # TODO: Update with your build command (npm run build, make, etc.)
$TestCommand = "dotnet test"  # TODO: Update with your test command
$CleanCommand = "dotnet clean"  # TODO: Update with your clean command

function Write-Step {
    param([string]$Message)
    Write-Host "`n→ $Message" -ForegroundColor Cyan
}

function Execute-Command {
    param([string]$Cmd)
    Write-Host "  $Cmd" -ForegroundColor Gray
    Invoke-Expression $Cmd
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Command failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

switch ($Command) {
    'clean' {
        Write-Step "Cleaning build artifacts"
        Execute-Command "$CleanCommand $SolutionFile"
        # TODO: Add any project-specific clean steps here
        Write-Host "✓ Clean complete" -ForegroundColor Green
    }
    
    'build' {
        Write-Step "Building $ProjectName"
        Execute-Command "$BuildCommand $SolutionFile --configuration Debug"
        Write-Host "✓ Build successful" -ForegroundColor Green
    }
    
    'test' {
        Write-Step "Building and running tests"
        Write-Host "  Building first..." -ForegroundColor Yellow
        Execute-Command "$BuildCommand $SolutionFile --configuration Debug"
        Write-Host "✓ Build successful" -ForegroundColor Green
        Write-Step "Running tests"
        Execute-Command "$TestCommand $SolutionFile --configuration Debug --verbosity normal"
        Write-Host "✓ Build and test complete - safe to commit" -ForegroundColor Green
        Write-Host "  💡 Tip: For faster testing, use ../test/quick.ps1 (1.3s) or ../test/full.ps1 (staged)" -ForegroundColor DarkGray
    }
    
    'test-only' {
        Write-Step "Running tests only (development iteration)"
        Write-Host "  ⚠️  Note: This doesn't validate Godot compilation" -ForegroundColor Yellow
        Execute-Command "dotnet test BlockLife.sln --configuration Debug --verbosity normal"
        Write-Host "✓ All tests passed" -ForegroundColor Green
        Write-Host "  Remember to run 'test' (not 'test-only') before committing!" -ForegroundColor Yellow
        Write-Host "  💡 Tip: Use ../test/quick.ps1 for architecture tests only (1.3s)" -ForegroundColor DarkGray
    }
    
    'run' {
        Write-Step "Running BlockLife"
        Write-Host "  Note: This requires Godot to be installed" -ForegroundColor Yellow
        if (Get-Command godot -ErrorAction SilentlyContinue) {
            Execute-Command "godot"
        } else {
            Write-Host "✗ Godot not found in PATH" -ForegroundColor Red
            Write-Host "  Please install Godot 4.4 or add it to your PATH" -ForegroundColor Yellow
        }
    }
    
    'all' {
        & $PSCommandPath clean
        & $PSCommandPath build
        & $PSCommandPath test
        Write-Host "`n✓ All steps completed successfully" -ForegroundColor Green
    }
}
