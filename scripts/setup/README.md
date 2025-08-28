# Setup & Configuration Scripts

Initial setup and environment configuration scripts.

## 🔧 Available Scripts

### install-claude-protection.ps1
**Purpose**: Protect main directory from accidental AI modifications
```powershell
./scripts/setup/install-claude-protection.ps1
```

**What it does**:
- Creates `.claude_protected` marker file in main directory
- Encourages use of persona-specific clones
- Prevents accidental modifications to main repository
- One-time setup per clone

**When to use**:
- After initial clone of main repository
- Before setting up persona clones
- To protect reference/backup copies

## 🎯 Setup Workflow

### Initial Repository Setup
```powershell
# 1. Clone main repository
git clone https://github.com/yourusername/BlockLife.git

# 2. Install protection on main
cd BlockLife
./scripts/setup/install-claude-protection.ps1

# 3. Setup persona clones
./scripts/persona/setup-personas.ps1

# 4. Work in persona-specific directories
cd ../BlockLife-DevEngineer
```

### Environment Verification
```powershell
# Check if protection is active
Test-Path .claude_protected

# Verify git hooks installed
dotnet husky install

# Check build environment
dotnet --info
```

## 🚀 Future Scripts (Planned)

### install-hooks.ps1 (TODO)
- Manual git hook installation
- For environments where Husky.NET isn't available
- Backup hook installation method

### configure-environment.ps1 (TODO)
- Set up development environment variables
- Configure IDE settings
- Install required tools

### check-prerequisites.ps1 (TODO)
- Verify .NET SDK version
- Check Godot installation
- Validate tool availability

## 📝 Notes

- Setup scripts are idempotent (safe to run multiple times)
- Protection is advisory - can be overridden if needed
- Designed for Windows (PowerShell) primarily

---
*Part of BlockLife development workflow*