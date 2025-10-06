#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Validates Darklands entity templates and translation keys.

.DESCRIPTION
    VS_021 Phase 4 - Template validation script for pre-push hook and CI.

    Validates:
    1. All translation keys (NameKey, DescriptionKey) exist in en.csv
    2. Template IDs are unique (no duplicates)
    3. Required fields present (MaxHealth > 0, Sprite set)
    4. Godot .tres file format is valid

    Exit Codes:
    - 0: All validations passed
    - 1: Validation errors found

.EXAMPLE
    ./scripts/validate-templates.ps1

.EXAMPLE
    # Run with verbose output
    ./scripts/validate-templates.ps1 -Verbose
#>

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$script:ErrorCount = 0

# ====================
# Helper Functions
# ====================

function Write-ValidationError {
    param([string]$Message)
    Write-Host "❌ ERROR: $Message" -ForegroundColor Red
    $script:ErrorCount++
}

function Write-ValidationSuccess {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-ValidationInfo {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

# ====================
# Validation Functions
# ====================

function Test-TranslationKeysExist {
    Write-Host "`n🔍 Checking translation keys..." -ForegroundColor Yellow

    # Load translation keys from en.csv
    $translationFile = "translations/en.csv"
    if (-not (Test-Path $translationFile)) {
        Write-ValidationError "Translation file not found: $translationFile"
        return
    }

    $translationKeys = @{}
    Get-Content $translationFile | Select-Object -Skip 1 | ForEach-Object {
        if ($_ -match '^([^,]+),') {
            $translationKeys[$matches[1]] = $true
        }
    }

    Write-ValidationInfo "Loaded $($translationKeys.Count) translation keys from $translationFile"

    # Check all .tres templates
    $templates = Get-ChildItem -Path "data/entities/*.tres" -ErrorAction SilentlyContinue
    if (-not $templates) {
        Write-ValidationInfo "No templates found in data/entities/ (OK for fresh setup)"
        return
    }

    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw

        # Extract NameKey
        if ($content -match 'NameKey\s*=\s*"([^"]+)"') {
            $nameKey = $matches[1]
            if ($nameKey -and -not $translationKeys.ContainsKey($nameKey)) {
                Write-ValidationError "Template '$($template.Name)' has missing translation key: '$nameKey' (not in $translationFile)"
            }
        }

        # Extract DescriptionKey
        if ($content -match 'DescriptionKey\s*=\s*"([^"]+)"') {
            $descKey = $matches[1]
            if ($descKey -and -not $translationKeys.ContainsKey($descKey)) {
                Write-ValidationError "Template '$($template.Name)' has missing translation key: '$descKey' (not in $translationFile)"
            }
        }
    }

    if ($script:ErrorCount -eq 0) {
        Write-ValidationSuccess "All translation keys exist in $translationFile"
    }
}

function Test-TemplateIdsUnique {
    Write-Host "`n🔍 Checking template ID uniqueness..." -ForegroundColor Yellow

    $templates = Get-ChildItem -Path "data/entities/*.tres" -ErrorAction SilentlyContinue
    if (-not $templates) {
        Write-ValidationInfo "No templates found in data/entities/ (OK for fresh setup)"
        return
    }

    $idMap = @{}

    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw

        # Extract Id field
        if ($content -match 'Id\s*=\s*"([^"]+)"') {
            $id = $matches[1]

            if ($idMap.ContainsKey($id)) {
                Write-ValidationError "Duplicate template ID '$id': found in '$($template.Name)' and '$($idMap[$id])'"
            } else {
                $idMap[$id] = $template.Name
            }
        } else {
            Write-ValidationError "Template '$($template.Name)' missing Id field"
        }
    }

    if ($script:ErrorCount -eq 0) {
        Write-ValidationSuccess "All template IDs are unique ($($idMap.Count) templates checked)"
    }
}

function Test-RequiredFields {
    Write-Host "`n🔍 Checking required template fields..." -ForegroundColor Yellow

    $templates = Get-ChildItem -Path "data/entities/*.tres" -ErrorAction SilentlyContinue
    if (-not $templates) {
        Write-ValidationInfo "No templates found in data/entities/ (OK for fresh setup)"
        return
    }

    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw
        $templateName = $template.Name

        # Check Id exists and not empty
        if ($content -match 'Id\s*=\s*"([^"]*)"') {
            $id = $matches[1]
            if ([string]::IsNullOrWhiteSpace($id)) {
                Write-ValidationError "Template '$templateName' has empty Id field"
            }
        }

        # Check NameKey exists and not empty
        if ($content -match 'NameKey\s*=\s*"([^"]*)"') {
            $nameKey = $matches[1]
            if ([string]::IsNullOrWhiteSpace($nameKey)) {
                Write-ValidationError "Template '$templateName' has empty NameKey field"
            }
        } else {
            Write-ValidationError "Template '$templateName' missing NameKey field"
        }

        # Check MaxHealth > 0
        if ($content -match 'MaxHealth\s*=\s*([0-9.]+)') {
            $maxHealth = [float]$matches[1]
            if ($maxHealth -le 0) {
                Write-ValidationError "Template '$templateName' has invalid MaxHealth: $maxHealth (must be > 0)"
            }
        } else {
            Write-ValidationError "Template '$templateName' missing MaxHealth field"
        }

        # Check Sprite is set (not null)
        if ($content -match 'Sprite\s*=\s*ExtResource\("([^"]+)"\)') {
            # Sprite is set via ExtResource - valid
        } elseif ($content -match 'Sprite\s*=\s*null') {
            Write-ValidationError "Template '$templateName' has null Sprite (texture required)"
        } elseif ($content -notmatch 'Sprite\s*=') {
            Write-ValidationError "Template '$templateName' missing Sprite field"
        }
    }

    if ($script:ErrorCount -eq 0) {
        Write-ValidationSuccess "All required fields present and valid"
    }
}

function Test-GodotFormat {
    Write-Host "`n🔍 Checking Godot .tres format..." -ForegroundColor Yellow

    $templates = Get-ChildItem -Path "data/entities/*.tres" -ErrorAction SilentlyContinue
    if (-not $templates) {
        Write-ValidationInfo "No templates found in data/entities/ (OK for fresh setup)"
        return
    }

    foreach ($template in $templates) {
        $content = Get-Content $template.FullName -Raw

        # Check for Godot resource header
        if ($content -notmatch '^\[gd_resource') {
            Write-ValidationError "Template '$($template.Name)' missing Godot resource header '[gd_resource...]'"
        }

        # Check for [resource] section
        if ($content -notmatch '\[resource\]') {
            Write-ValidationError "Template '$($template.Name)' missing [resource] section"
        }

        # Check script reference exists
        if ($content -notmatch 'script\s*=\s*ExtResource') {
            Write-ValidationError "Template '$($template.Name)' missing script reference (should reference ActorTemplate.cs)"
        }
    }

    if ($script:ErrorCount -eq 0) {
        Write-ValidationSuccess "All templates have valid Godot .tres format"
    }
}

# ====================
# Main Execution
# ====================

Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
Write-Host "  🛡️  Template Validation (VS_021 Phase 4)" -ForegroundColor Magenta
Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Magenta

# Run all validation checks
Test-TranslationKeysExist
Test-TemplateIdsUnique
Test-RequiredFields
Test-GodotFormat

# Summary
Write-Host "`n════════════════════════════════════════════════════════════════" -ForegroundColor Magenta

if ($script:ErrorCount -eq 0) {
    Write-Host "  ✅ All validations PASSED" -ForegroundColor Green
    Write-Host "  Templates are valid and ready to push!" -ForegroundColor Green
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    exit 0
} else {
    Write-Host "  ❌ Validation FAILED with $script:ErrorCount error(s)" -ForegroundColor Red
    Write-Host "  Fix errors before pushing to remote" -ForegroundColor Red
    Write-Host "════════════════════════════════════════════════════════════════" -ForegroundColor Magenta
    exit 1
}
