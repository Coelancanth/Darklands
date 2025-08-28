# BlockLife Scripts Quick Reference Card

> **Print-friendly cheat sheet for daily development**

## ⚡ Essential Commands

### Build System
```bash
./scripts/core/build.ps1 test     # Safe default before commits ⭐
./scripts/core/build.ps1 build    # Fast iteration
./scripts/core/build.ps1 clean    # Clean build artifacts
```

### Persona System  
```bash
# One-time setup (deprecated - single-repo now)
# Old multi-clone: ./scripts/persona/setup-personas.ps1

# Daily sync (deprecated - single-repo now uses embody.ps1)
# Old: ./scripts/persona/sync-personas.ps1

# Navigate to persona
cd ../blocklife-dev-engineer      # Dev work
cd ../blocklife-tech-lead         # Architecture  
cd ../blocklife-test-specialist   # Testing
```

## 🚨 Emergency Commands

```bash
# Bypass hooks (use sparingly)
git commit --no-verify
git push --no-verify

# Fix build issues
./scripts/core/build.ps1 clean
./scripts/core/build.ps1 test

# Manual hook testing
.husky/pre-commit
.husky/pre-push
```

## 🔍 Verification

```bash
# Verify subagent work
./scripts/verify-subagent.ps1 -Type backlog

# Verify backlog updates  
./scripts/verify-backlog-update.ps1 -ItemNumber "TD_042"
```

## 📋 Daily Workflow

1. `cd ../blocklife-[persona]` - Switch to persona workspace
2. Work on features...
3. `./scripts/core/build.ps1 test` - Test before commit
4. `git commit` - Auto-formatting + validation
5. `git push` - Build validation + tests

## 🛡️ Quality Gates

- **Pre-commit**: Instant validation (~0.3s) + commit message validation  
- **Pre-push**: Build (enforces formatting) + analysis + fast tests
- **Architecture**: Eliminate redundancy - build system handles formatting

## 💡 Pro Tips

- Use `test` (not `test-only`) before commits
- Let hooks do the formatting automatically
- Sync handled automatically by: `./scripts/persona/embody.ps1`
- Update Memory Bank when hooks remind you

---
**Full Guide**: [GUIDE.md](GUIDE.md)