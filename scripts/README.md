# BlockLife Scripts

Essential automation for builds, git workflow, and persona management.

## ⚡ Quick Start

```bash
# Essential commands (Windows)
./scripts/test/quick.ps1                         # Architecture tests only (1.3s)
./scripts/test/full.ps1                          # Complete test suite (3-5s)
./scripts/core/build.ps1 test                    # Build + test (before commits)
./scripts/git/branch-status-check.ps1            # Check current branch
./scripts/persona/embody.ps1 [persona]           # Switch personas with auto-sync

# Essential commands (Linux/Mac)  
./scripts/core/build.sh test                     # Build + test (before commits)
./scripts/git/branch-status-check.sh             # Check current branch
```

## 📁 Script Categories

| Category | Purpose | Key Scripts |
|----------|---------|-------------|
| **core/** | Build & test | `build.ps1`, `build.sh` |
| **test/** | Test execution | `quick.ps1` (1.3s), `full.ps1` (staged), `incremental.ps1` (coming) |
| **git/** | Git workflows | `branch-status-check.ps1`, `smart-sync.ps1` |
| **persona/** | Persona system | `embody.ps1` (v4 with auto-sync) |

## 📚 Documentation

- **[Quick Reference](QUICK-REFERENCE.md)** ⭐ Print-friendly cheat sheet  
- **[Complete Guide](GUIDE.md)** ⭐ Workflows, troubleshooting, hooks
- **[Contributing](CONTRIBUTING.md)** - For script maintainers

**New to the project?** Start with [Quick Reference](QUICK-REFERENCE.md), then [Complete Guide](GUIDE.md) for details.

**Need help quickly?** The Quick Reference card has everything you need for daily work.