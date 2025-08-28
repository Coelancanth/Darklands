# Utility Scripts

General-purpose utility and helper scripts.

## 🔧 Available Scripts

### rotate-memory-bank.ps1
**Purpose**: Automated Memory Bank rotation for BlockLife personas
```powershell
# Check and perform due rotations
./scripts/utils/rotate-memory-bank.ps1

# Preview what would be rotated (safe to run anytime)
./scripts/utils/rotate-memory-bank.ps1 -DryRun

# Force rotate session log immediately
./scripts/utils/rotate-memory-bank.ps1 -Type session -Force

# Force rotate context files immediately
./scripts/utils/rotate-memory-bank.ps1 -Type context -Force
```

**Features**:
- 📅 Monthly rotation of session-log.md (first 3 days of month)
- 📊 Quarterly rotation of active context files (Jan/Apr/Jul/Oct)
- 🗑️ Archive cleanup with configurable retention
- 📏 Size-based early rotation (1000 lines session, 200 lines context)
- 🔍 Dry-run mode for safe testing

**Retention Policy**:
- Session logs: 6 months
- Context files: 2 quarters
- Automatic cleanup of expired archives

### setup-rotation-schedule.ps1
**Purpose**: Set up automated scheduling for Memory Bank rotation
```powershell
# Set up automatic daily checks
./scripts/utils/setup-rotation-schedule.ps1

# Remove automatic rotation
./scripts/utils/setup-rotation-schedule.ps1 -Remove
```

**Features**:
- 🖥️ Windows scheduled task (Task Scheduler)
- 🐧 Unix/Linux cron job support
- 🔔 Daily checks at 9 AM local time
- 🔗 Git hook integration for reminders
- ✅ Test run on setup

### fix-session-log-order.ps1
**Purpose**: Fix chronological ordering of Memory Bank session log entries
```powershell
# Fix ordering issues (with automatic backup)
./scripts/utils/fix-session-log-order.ps1

# Preview changes without modifying
./scripts/utils/fix-session-log-order.ps1 -DryRun

# Only check for issues
./scripts/utils/fix-session-log-order.ps1 -ValidateOnly
```

**Features**:
- 🔍 Detects out-of-order timestamps
- 📅 Sorts entries chronologically within each date
- 💾 Automatic backup before changes
- 🎯 Preserves all content and formatting
- ✅ Safe dry-run mode

### check-session-log-health.ps1
**Purpose**: Comprehensive health check for Memory Bank session log
```powershell
# Run all health checks
./scripts/utils/check-session-log-health.ps1

# Only show output if issues exist
./scripts/utils/check-session-log-health.ps1 -QuietMode

# Auto-fix fixable issues
./scripts/utils/check-session-log-health.ps1 -AutoFix
```

**Health Checks**:
- ⏰ Chronological ordering validation
- 📏 Size limit checking (warns at 80%, fails at 100%)
- 🔢 Duplicate timestamp detection
- 📝 Entry format consistency
- 🔧 Optional auto-fix for supported issues

### validate-scripts.ps1
**Purpose**: Validate script consistency and standards compliance
```powershell
# Check all scripts for issues
./scripts/utils/validate-scripts.ps1

# Auto-fix issues where possible
./scripts/utils/validate-scripts.ps1 -Fix

# Verbose output
./scripts/utils/validate-scripts.ps1 -Verbose
```

**What it checks**:
- ✅ Shebang line (`#!/usr/bin/env pwsh`)
- ✅ Error handling (`$ErrorActionPreference = 'Stop'`)
- ✅ Naming convention (verb-noun format)
- ✅ Documentation headers (.SYNOPSIS/.DESCRIPTION)
- ✅ Cross-platform support for critical scripts

## Planned Utilities

### Cross-Platform Functions
- `common.ps1/.sh` - Shared functions for script standardization
- `colors.ps1/.sh` - Consistent color output across scripts
- `validation.ps1/.sh` - Input validation and error handling helpers

### Common Patterns
- Error handling and logging
- User input validation  
- Cross-platform path handling
- Process execution with proper error codes

## Current Status

**Status**: Placeholder - Utilities to be extracted from existing scripts

**Current Approach**: Each script handles common tasks independently

## Benefits of Shared Utilities

### Code Quality
- Consistent error handling across all scripts
- Standardized user feedback and messaging
- Reduced code duplication

### Maintainability  
- Single place to update common functionality
- Easier testing of shared logic
- Consistent behavior across automation

### Developer Experience
- Predictable script behavior
- Standardized color coding and messaging
- Common validation patterns

## Implementation Strategy

1. **Extract Common Patterns**: Identify repeated code in existing scripts
2. **Create Utility Functions**: Build reusable function libraries  
3. **Update Existing Scripts**: Refactor to use shared utilities
4. **Document Usage**: Provide clear examples for new scripts