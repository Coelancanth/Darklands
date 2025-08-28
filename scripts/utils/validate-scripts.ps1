#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validates script consistency and standards compliance
.DESCRIPTION
    Checks all scripts for proper headers, error handling, and naming conventions.
    Ensures consistency across the codebase.
.EXAMPLE
    ./scripts/utils/validate-scripts.ps1
    ./scripts/utils/validate-scripts.ps1 -Fix
#>

param(
    [switch]$Fix = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = 'Stop'

# Validation results
$issues = @()
$warnings = @()
$fixed = 0

Write-Host "🔍 Script Consistency Validator" -ForegroundColor Cyan
Write-Host ""

# Find all PowerShell scripts
$scripts = Get-ChildItem -Path "scripts" -Recurse -Filter "*.ps1" | Where-Object { $_.FullName -notmatch "deprecated" }

Write-Host "📊 Found $($scripts.Count) PowerShell scripts to validate" -ForegroundColor Gray
Write-Host ""

foreach ($script in $scripts) {
    $relativePath = $script.FullName.Replace("$PWD\", "").Replace("\", "/")
    $content = Get-Content $script.FullName -Raw
    $lines = Get-Content $script.FullName
    
    if ($Verbose) {
        Write-Host "  Checking: $relativePath" -ForegroundColor Gray
    }
    
    # Check 1: Shebang line
    if ($lines.Count -gt 0 -and -not $lines[0].StartsWith("#!/usr/bin/env pwsh")) {
        if ($Fix) {
            $newContent = "#!/usr/bin/env pwsh`n" + $content
            Set-Content -Path $script.FullName -Value $newContent
            $fixed++
            Write-Host "  ✅ Fixed shebang: $relativePath" -ForegroundColor Green
        } else {
            $issues += "Missing shebang: $relativePath"
        }
    }
    
    # Check 2: Error handling
    if ($content -notmatch '\$ErrorActionPreference\s*=\s*[''"]Stop[''"]') {
        $warnings += "No error handling: $relativePath (missing `$ErrorActionPreference = 'Stop')"
    }
    
    # Check 3: Naming convention (verb-noun)
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($script.Name)
    if ($fileName -notmatch '^[a-z]+-[a-z]+') {
        if ($fileName -ne "README" -and $fileName -ne "QUICK_REFERENCE") {
            $warnings += "Naming convention: $relativePath (should be verb-noun format)"
        }
    }
    
    # Check 4: Documentation header
    if ($content -notmatch '<#[\s\S]*?\.SYNOPSIS[\s\S]*?\.DESCRIPTION[\s\S]*?#>') {
        $warnings += "Missing documentation: $relativePath (no .SYNOPSIS/.DESCRIPTION block)"
    }
}

# Check for shell script equivalents
Write-Host "🐧 Checking for cross-platform support..." -ForegroundColor Cyan
$criticalScripts = @("branch-status-check", "build")
foreach ($scriptName in $criticalScripts) {
    $ps1Files = Get-ChildItem -Path "scripts" -Recurse -Filter "$scriptName.ps1"
    $shFiles = Get-ChildItem -Path "scripts" -Recurse -Filter "$scriptName.sh"
    
    if ($ps1Files.Count -gt 0 -and $shFiles.Count -eq 0) {
        $warnings += "Missing Linux version: $scriptName.sh"
    }
}

# Report results
Write-Host ""
Write-Host "📋 Validation Results" -ForegroundColor Cyan
Write-Host "─────────────────────" -ForegroundColor Gray

if ($issues.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "✅ All scripts pass validation!" -ForegroundColor Green
} else {
    if ($issues.Count -gt 0) {
        Write-Host ""
        Write-Host "❌ Issues Found ($($issues.Count)):" -ForegroundColor Red
        foreach ($issue in $issues) {
            Write-Host "   • $issue" -ForegroundColor Red
        }
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host ""
        Write-Host "⚠️  Warnings ($($warnings.Count)):" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "   • $warning" -ForegroundColor Yellow
        }
    }
    
    if ($fixed -gt 0) {
        Write-Host ""
        Write-Host "✅ Fixed $fixed issues automatically" -ForegroundColor Green
    }
    
    if ($issues.Count -gt 0 -and -not $Fix) {
        Write-Host ""
        Write-Host "💡 Run with -Fix to automatically fix issues" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   Scripts checked: $($scripts.Count)" -ForegroundColor Gray
Write-Host "   Issues: $($issues.Count)" -ForegroundColor $(if ($issues.Count -eq 0) { "Green" } else { "Red" })
Write-Host "   Warnings: $($warnings.Count)" -ForegroundColor $(if ($warnings.Count -eq 0) { "Green" } else { "Yellow" })
Write-Host "   Fixed: $fixed" -ForegroundColor $(if ($fixed -gt 0) { "Green" } else { "Gray" })

# Exit with appropriate code
if ($issues.Count -gt 0) {
    exit 1
} elseif ($warnings.Count -gt 0) {
    exit 0  # Warnings don't fail the check
} else {
    exit 0
}