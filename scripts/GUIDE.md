# BlockLife Scripts Complete Guide

**Last Updated**: 2025-08-22  
**Maintained by**: DevOps Engineer

> **TL;DR**: Use `./scripts/core/build.ps1 test` before committing. Use persona clones for development. Git hooks handle quality automatically.

## 🚀 Quick Start (5 Minutes to Productivity)

### 1. Essential Build Commands
```bash
# Safe default before any commit (builds + tests)
./scripts/core/build.ps1 test        # Windows
./scripts/core/build.sh test         # Linux/Mac

# Just build (faster iteration)
./scripts/core/build.ps1 build       # Windows  
./scripts/core/build.sh build        # Linux/Mac
```

### 2. Set Up Persona Workspaces (One-time)
```bash
# Creates 6 isolated persona clones
# Deprecated - use embody.ps1 instead

# Navigate between personas
cd ../blocklife-dev-engineer     # Dev work
cd ../blocklife-tech-lead        # Architecture decisions
cd ../blocklife-test-specialist  # Testing focus
```

### 3. Git Workflow Tools
```bash
# Check branch status and PR info
./scripts/git/branch-status-check.ps1     # Windows
./scripts/git/branch-status-check.sh      # Linux/Mac

# Clean merged branches intelligently  
./scripts/git/branch-cleanup.ps1          # Uses git fetch --prune
```

## 📋 Script Reference by Task

| Task | Script | Location |
|------|--------|----------|
| **Build & Test** |
| Build project | `build.ps1 build` | `scripts/core/` |
| Run tests | `build.ps1 test` | `scripts/core/` |
| Clean build | `build.ps1 clean` | `scripts/core/` |
| **Git Workflow** |
| Check branch status | `branch-status-check.ps1` | `scripts/git/` |
| Clean merged branches | `branch-cleanup.ps1` | `scripts/git/` |
| **Persona Management** |
| Embody persona | `embody.ps1` | `scripts/persona/` |
| **Verification** |
| Verify subagent work | `verify-subagent.ps1` | `scripts/verification/` |
| Verify backlog updates | `verify-backlog-update.ps1` | `scripts/verification/` |

## 🎯 Daily Development Workflow

```bash
# 1. Start with persona workspace
cd ../blocklife-dev-engineer

# 2. Check branch status  
./scripts/git/branch-status-check.ps1

# 3. Work on features...
# Code, commit, repeat

# 4. Before pushing (safety check)
./scripts/core/build.ps1 test

# 5. Push when ready (hooks validate automatically)
git push
```

## 🛡️ Git Hooks & Quality Gates

### Automatic Quality Control
- **Pre-commit**: Instant atomic commit guidance (~0.3s)
- **Commit-msg**: Validates conventional format (`feat(VS_003): description`)
- **Pre-push**: Builds + tests + warnings (smart detection)

### Hook Details

#### Pre-commit Hook
- **Purpose**: Educational guidance for atomic commits
- **Performance**: Instant (~0.3s)
- **Output**: Displays atomic commit checklist

#### Commit Message Validation
**Valid formats:**
```bash
feat(VS_003): add save system        # Feature work
fix(BR_012): resolve race condition  # Bug fixes  
tech(TD_042): consolidate archives   # Technical debt
docs: update README                  # Documentation
test: add integration tests          # Testing
```

#### Pre-push Hook
- **Smart building**: Only builds when C# code changes
- **Quality checks**: Formatting + analysis + fast tests
- **Warnings**: Branch staleness, Memory Bank reminders
- **Performance**: ~10-15 seconds for full validation

### Common Hook Issues

**Build failed:**
```bash
# Fix build first
./scripts/core/build.ps1 test
git push  # Retry

# Emergency bypass
git push --no-verify
```

**Commit message format:**
```bash
# Fix format
git commit --amend -m "feat(VS_003): add user login"

# Or bypass (not recommended)
git commit --no-verify
```

## 🧪 Build System Deep Dive

### Available Commands
```bash
./scripts/core/build.ps1 build      # Build only (fastest)
./scripts/core/build.ps1 test       # Build + tests (safe default) ⭐
./scripts/core/build.ps1 test-only  # Tests only (dev iteration)
./scripts/core/build.ps1 clean      # Clean build artifacts
./scripts/core/build.ps1 run        # Run game (requires Godot)
./scripts/core/build.ps1 all        # Clean + build + test
```

**Critical**: Use `test` (not `test-only`) before committing - catches Godot compilation issues.

### Performance Tips
- **Development**: Use `build` for fast iteration
- **Pre-commit**: Always use `test` (validates Godot integration)
- **CI/CD**: Uses same scripts for consistency

## 👥 Persona System

### Initial Setup
```bash
# Run once to set up all 6 personas
# Deprecated - use embody.ps1 instead

# Creates these directories (in parent folder):
# blocklife-dev-engineer/
# blocklife-tech-lead/
# blocklife-product-owner/
# blocklife-test-specialist/
# blocklife-debugger-expert/
# blocklife-devops-engineer/
```

### Daily Usage
```bash
# Sync all personas with remote (weekly)
# Deprecated - now handled by embody.ps1

# Work in specific persona
cd ../blocklife-dev-engineer
# Your work happens here...

# Switch personas for different tasks
cd ../blocklife-tech-lead
# Architecture decisions here...
```

### Benefits
- **Isolated workspaces**: No merge conflicts between different work types
- **Context switching**: Each persona has specific focus and tools
- **Clean handoffs**: Use Memory Bank activeContext.md for continuity

## 🚨 Troubleshooting

### Build Issues
```bash
# General build problems
./scripts/core/build.ps1 clean
./scripts/core/build.ps1 build

# Verbose diagnostics
dotnet build BlockLife.sln --verbosity normal

# Reset environment
./scripts/core/build.ps1 clean
rm -rf bin/ obj/
./scripts/core/build.ps1 build
```

### Git Workflow Issues
```bash
# Hook failures (usually build/format issues)
git status                    # See what's modified
./scripts/core/build.ps1 test # Fix build/test issues
git add -A && git commit      # Retry commit

# Persona setup issues
git --version                 # Verify git available
# Deprecated - use embody.ps1 instead -SkipExisting  # Clean retry
```

### Performance Issues
```bash
# Slow builds
./scripts/core/build.ps1 clean  # Clear artifacts
# Check for large files in bin/obj directories

# Slow tests
./scripts/core/build.ps1 test-only  # Skip build
# Review test categories in .csproj files
```

## 💡 Pro Tips

### Efficiency
```bash
# Shell aliases for common commands
alias bt='./scripts/core/build.ps1 test'
alias bb='./scripts/core/build.ps1 build' 
alias embody='./scripts/persona/embody.ps1'
```

### Workflow Optimization
- **Hot reloading**: Use `build` command during development
- **Commit prep**: Use `test` command before committing  
- **Persona sync**: Run weekly or when switching major work items
- **Hook bypass**: Only use `--no-verify` for urgent hotfixes

### Memory Bank Integration
- Update `activeContext.md` when hooks remind you
- Focus on significant context (not every small change)
- Use for persona handoffs and complex debugging sessions

## 🔧 Advanced Usage

### Cross-Platform Development
- Primary development on Windows (PowerShell scripts)
- Linux/Mac support via .sh versions of core scripts
- Test on both platforms before committing cross-platform changes

### CI/CD Integration
- GitHub Actions uses same build scripts for consistency
- Pre-push hooks mirror CI validation locally
- Same quality gates in development and production

### Custom Script Development
See [Contributing Guide](CONTRIBUTING.md) for:
- Script standards and conventions
- Testing requirements
- Documentation standards
- Integration guidelines

---

**Need More Help?**
- **Quick tasks**: Check [Quick Reference](QUICK-REFERENCE.md)
- **Script development**: See [Contributing](CONTRIBUTING.md)  
- **Complex issues**: Ask the DevOps Engineer persona