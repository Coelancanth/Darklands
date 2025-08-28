# Persona Worktree System

**Version**: 1.0.0  
**Status**: Complete ✅  
**Created**: 2025-08-20

## What It Is

A workspace isolation system that gives each development persona their own directory. No more git conflicts, no more file conflicts, no more "which persona was I?" confusion.

## Quick Setup (One Time)

```powershell
# From BlockLife root directory
.\scripts\persona\setup-aliases.ps1 -AddToProfile

# Recommended: Install Claude protection
.\scripts\protection\install-claude-protection.ps1
```

That's it! Restart PowerShell and you're ready.

## Daily Usage

### Switch Personas
```powershell
blocklife dev       # Switch to dev-engineer workspace
blocklife tech      # Switch to tech-lead workspace  
blocklife test      # Switch to test-specialist workspace
# ... etc for all 6 personas
```

### Even Faster
```powershell
bl-dev              # Instant switch to dev-engineer
bl-tech             # Instant switch to tech-lead
bl-test             # Instant switch to test-specialist
```

### Utility Commands
```powershell
bl-status           # Show all active workspaces
bl-return           # Return to main directory
bl-clean            # Clean up unused worktrees
```

## Complete Workflow Example

```powershell
# Morning: Start work as dev engineer
blocklife dev       # Switches workspace AND launches Claude automatically
embody dev-engineer
# ... do implementation work ...

# Afternoon: Review as tech lead
blocklife tech      # Switches workspace AND launches Claude automatically
embody tech-lead
# ... review code in complete isolation ...

# Evening: Return to main
bl-return
```

### Skip Claude Launch (if needed)
```powershell
# Switch without launching Claude
.\scripts\persona\switch-persona.ps1 dev-engineer -NoLaunchClaude
```

## How It Works

```
blocklife/                           # Main repository
├── personas/                        # Isolated workspaces (auto-created)
│   ├── dev-engineer/               # Complete project copy
│   ├── tech-lead/                  # Complete project copy
│   └── [other personas]/           # As needed
```

Each workspace:
- Has its own files (no conflicts)
- Has its own git branch (no merge issues)
- Has its own build outputs (no interference)
- Works exactly like the main directory

## Supported Personas

| Alias | Full Name | Purpose |
|-------|-----------|---------|
| `dev` | dev-engineer | Code implementation |
| `tech` | tech-lead | Architecture & review |
| `test` | test-specialist | Quality validation |
| `debug` | debugger-expert | Complex debugging |
| `devops` | devops-engineer | CI/CD & automation |
| `product` | product-owner | Feature definition |

## Flexible Commands

The `blocklife` command accepts many variations:
- `dev`, `engineer` → dev-engineer
- `tech`, `lead` → tech-lead
- `test`, `tester` → test-specialist
- `debug`, `debugger` → debugger-expert
- `devops`, `ops` → devops-engineer
- `product`, `owner`, `po` → product-owner

## FAQ

**Q: Do I need to create branches manually?**  
A: No, each persona gets their own workspace branch automatically.

**Q: Can I work on the same feature in different personas?**  
A: Yes! Create feature branches within each workspace:
```powershell
# In dev-engineer workspace
git checkout -b feat/save-system

# In tech-lead workspace  
git fetch origin
git checkout feat/save-system  # Review the same feature
```

**Q: How much disk space does this use?**  
A: ~80MB per persona (uses git worktrees, very efficient).

**Q: What if I forget where I am?**  
A: Your prompt shows it: `@Coel dev-engineer git(persona/dev-engineer/workspace)`

**Q: Can I delete a workspace?**  
A: Yes, use `git worktree remove personas/[persona-name]`

## Claude Protection System (TD_029)

The protection system intercepts the `claude` command to guide you toward persona workspaces:

### How It Works

```powershell
# After installing protection:
claude                      # Shows reminder in main directory
claude                      # Works normally in persona workspaces
```

### Installation

```powershell
# Install protection (one time):
.\scripts\protection\install-claude-protection.ps1

# Activate it:
. $PROFILE  # Or restart PowerShell

# Remove if needed:
.\scripts\protection\install-claude-protection.ps1 -Uninstall
```

### Protection Features

- **Automatic**: Intercepts the real `claude` command
- **Context-aware**: Only triggers in BlockLife main directory
- **Non-intrusive**: Easy bypass options
- **Persistent choice**: Can disable per-project with .claude-protection

### The Protection Flow

```
User types: claude
         ↓
┌─────────────────────────┐
│  In BlockLife main?     │──No──→ Launch Claude normally
└────────┬────────────────┘
        Yes
         ↓
┌─────────────────────────┐
│  Protection disabled?   │──Yes──→ Launch Claude normally
│  (.claude-protection)   │
└────────┬────────────────┘
         No
         ↓
┌─────────────────────────┐
│  Show friendly reminder │
│  with persona benefits  │
└────────┬────────────────┘
         ↓
    [P] Switch to persona
    [C] Continue this time
    [D] Disable permanently
```
## Troubleshooting

**"Command not found"**  
Run the setup again: `.\scripts\persona\setup-aliases.ps1 -AddToProfile`

**"Not in a git repository"**  
Run commands from the BlockLife project root.

**"Failed to create worktree"**  
Check disk space and git version (needs Git 2.5+).

**"Claude protection not working"**  
Install it: `.\scripts\protection\install-claude-protection.ps1`
Then restart PowerShell or run: `. $PROFILE`
## Benefits Summary

✅ **Zero Conflicts** - Complete isolation between personas  
✅ **Fast Switching** - Under 5 seconds  
✅ **Simple Commands** - Just `blocklife dev`  
✅ **No Learning Curve** - Works like normal git  
✅ **Clean Mental Model** - One persona = one directory

---

*TD_023 Implementation - Solving real developer friction with elegant simplicity*