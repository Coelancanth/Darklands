#!/usr/bin/env pwsh
# Generic Build Script
# Customize the commands below for your project

param(
    [Parameter(Position=0)]
    [ValidateSet('build', 'test', 'test-only', 'clean', 'run', 'all')]
    [string]$Command = 'build'
)

$ErrorActionPreference = "Stop"

# PROJECT CONFIGURATION - DARKLANDS
$ProjectName = "Darklands"
$CoreProject = "src/Darklands.Core/Darklands.Core.csproj"
$TestProject = "tests/Darklands.Core.Tests/Darklands.Core.Tests.csproj"
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
        Execute-Command "$CleanCommand $CoreProject"
        Execute-Command "$CleanCommand $TestProject"
        Execute-Command "$CleanCommand $GodotProject"
        # Clean bin and obj directories
        Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✓ Clean complete" -ForegroundColor Green
    }

    'build' {
        Write-Step "Building $ProjectName"
        Execute-Command "$BuildCommand $CoreProject --configuration Debug"
        Execute-Command "$BuildCommand $TestProject --configuration Debug"
        Execute-Command "$BuildCommand $GodotProject --configuration Debug"
        Write-Host "✓ Build successful" -ForegroundColor Green
    }

    'test' {
        Write-Step "Building and running tests"
        Write-Host "  Building first..." -ForegroundColor Yellow
        Execute-Command "$BuildCommand $CoreProject --configuration Debug"
        Execute-Command "$BuildCommand $TestProject --configuration Debug"
        Execute-Command "$BuildCommand $GodotProject --configuration Debug"
        Write-Host "✓ Build successful" -ForegroundColor Green
        Write-Step "Running tests"
        Execute-Command "$TestCommand $TestProject --configuration Debug --verbosity normal"
        Write-Host "✓ Build and test complete - safe to commit" -ForegroundColor Green
        Write-Host "  💡 Tip: Feature-specific tests: add --filter `"Category=Combat`" (or Health/Inventory/Grid/etc)" -ForegroundColor DarkGray
    }

    'test-only' {
        Write-Step "Running tests only (development iteration)"
        Write-Host "  ⚠️  Note: This doesn't validate Godot compilation" -ForegroundColor Yellow
        Execute-Command "$TestCommand $TestProject --configuration Debug --verbosity normal --no-build"
        Write-Host "✓ All tests passed" -ForegroundColor Green
        Write-Host "  Remember to run 'test' (not 'test-only') before committing!" -ForegroundColor Yellow
        Write-Host "  💡 Tip: Feature-specific: add --filter `"Category=Combat`" (or Health/Inventory/Grid/etc)" -ForegroundColor DarkGray
    }

    'run' {
        Write-Step "Running $ProjectName"
        Write-Host "  Note: This requires Godot to be installed" -ForegroundColor Yellow
        if (Get-Command godot -ErrorAction SilentlyContinue) {
            # Build Core library first
            Execute-Command "$BuildCommand $CoreProject --configuration Debug"
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
