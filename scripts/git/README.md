# Git Workflow Scripts

Scripts for managing git branches, PRs, and repository state.

## 🔧 Available Scripts

### branch-status-check.ps1 / .sh
**Purpose**: Check current branch status and PR state
```powershell
# Windows
./scripts/git/branch-status-check.ps1

# Linux/Mac
source ./scripts/git/branch-status-check.sh
```

**Features**:
- Shows current branch name
- Checks for associated PR
- Detects merged PRs needing cleanup
- Suggests next actions based on state

### branch-cleanup.ps1
**Purpose**: Intelligent branch cleanup after PR merge
```powershell
# Clean current branch
./scripts/git/branch-cleanup.ps1

# Clean specific branch
./scripts/git/branch-cleanup.ps1 feat/VS_003

# Force cleanup (skip safety checks)
./scripts/git/branch-cleanup.ps1 feat/VS_003 -Force
```

**Features**:
- Uses `git fetch --prune` for reliable state sync
- Detects merged PRs even after remote deletion
- Provides actionable guidance for edge cases
- Safe by default (requires -Force for uncommitted changes)

## 🎯 Common Workflows

### After PR Merge
```powershell
# 1. Check status
./scripts/git/branch-status-check.ps1

# 2. If merged, cleanup
./scripts/git/branch-cleanup.ps1

# 3. Start new work
git checkout -b feat/VS_004
```

### Daily Branch Hygiene
```powershell
# Sync and prune stale references
git fetch origin --prune

# Check all local branches
git branch -v

# Clean up merged branches
./scripts/git/branch-cleanup.ps1 [branch-name]
```

## 🚨 Safety Features

- **Never deletes main branch**
- **Warns about unmerged changes**
- **Checks PR state before cleanup**
- **Requires explicit confirmation or -Force flag**

## 📝 Notes

- Branch cleanup uses native git operations for reliability
- Scripts integrate with GitHub CLI for PR information
- Cross-platform support (PowerShell Core recommended)

---
*Part of BlockLife development workflow*