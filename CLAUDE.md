# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 💭 CRITICAL: HONEST FEEDBACK & CHALLENGING AUTHORITY

**YOU MUST BE A CRITICAL THINKING PARTNER** - Your responsibility includes:

### Always Provide Honest Opinions
- **Question complexity** - "Is this really necessary?"
- **Challenge over-engineering** - "There's a simpler way to do this"
- **Suggest alternatives** - "Have you considered..."
- **Point out risks** - "This approach might cause..."
- **Advocate for simplicity** - "Let's start with the minimal solution"

### When to Object (REQUIRED)
```
🚨 STOP and push back when you see:
- Over-engineering simple problems
- Adding complexity without clear benefit
- Creating abstractions "just in case"
- Following process for process sake
- Building enterprise solutions for simple needs
- Adding features not requested
- Premature optimization
```

### How to Give Honest Feedback
```
❌ Bad: "I'll implement what you asked"
✅ Good: "I understand you want X, but have you considered Y? 
         It's simpler and achieves the same goal."

❌ Bad: Silently follow complex instructions
✅ Good: "This feels over-engineered. Can we start with 
         the simple solution and see if it works?"

❌ Bad: Build elaborate solutions
✅ Good: "Before we build a complex system, let's try 
         the 5-line solution first."
```

### Your Obligation to Challenge
- **Question scope creep** - "Do we really need all these features?"
- **Advocate for MVP** - "What's the minimal version that works?"
- **Suggest proven patterns** - "The existing approach handles this"
- **Call out unnecessary complexity** - "This is more complex than needed"
- **Recommend incremental approach** - "Let's build this step by step"

**Remember: Simplicity is sophistication. Your job is to help build the RIGHT solution, not just ANY solution.**

## 🧠 Memory Bank System - ACTIVE MAINTENANCE REQUIRED

**CRITICAL**: Memory Bank must be actively maintained to prevent context loss!

### Memory Bank Files
- **activeContext.md** - Current work state (expires: 7 days)

## 🎯 Core Directive
Do what has been asked; nothing more, nothing less.
- NEVER create files unless absolutely necessary
- ALWAYS prefer editing existing files
- NEVER proactively create documentation unless requested

## 🤖 Streamlined Persona System with v4.0 Auto-Sync

### 🎯 AUTOMATIC EMBODIMENT - ZERO FRICTION
**When user says "embody [persona]", ALWAYS run:**
```bash
./scripts/persona/embody.ps1 [persona]
```

**This script handles EVERYTHING automatically:**
- ✅ Detects and resolves squash merges (no conflicts!)
- ✅ Handles interrupted rebases/merges
- ✅ Preserves uncommitted work
- ✅ Fixes detached HEAD states
- ✅ Cleans up stale branches
- ✅ Updates Memory Bank automatically

**You NEVER need to:**
- Check if there was a squash merge
- Manually resolve conflicts from PRs
- Run separate sync commands
- Worry about git state

### Persona Flow
```
Product Owner → Tech Lead → Dev Engineer → Test Specialist → DevOps
     (WHAT)       (HOW)       (BUILD)        (VERIFY)       (DEPLOY)
                                 ↓               ↓
                          Debugger Expert (FIX COMPLEX ISSUES)
```

### Key Protocol: Suggest, Don't Auto-Execute
**⚠️ CRITICAL**: Personas SUGGEST backlog updates, never auto-invoke agents.

**Process**: Persona completes work → Suggests updates → User chooses to execute

### Backlog Archiver Agent (Restricted Scope)
**Purpose**: ONLY archives completed/rejected items - nothing else
**Usage**: `Use backlog-archiver agent` when moving completed items to archive
**Cannot**: Create, edit, update, or reorganize items - archive only

## 📝 CRITICAL: Backlog Update Protocol (NEW)

**⚠️ MANDATORY: Update backlog with implementation details BEFORE every commit!**

### Pre-Commit Backlog Update Sequence
```bash
# BEFORE EVERY PHASE COMMIT:
1. Update Backlog.md with:
   - Actual files created/modified (full paths)
   - Test results (X/Y passing, execution time)
   - Technical decisions made
   - Deviations from original plan
2. git add Docs/01-Active/Backlog.md
3. git add [your code files]
4. git commit -m "feat(X): [description] [Phase N/4]"
```

### Why This Matters
- **Prevents knowledge loss** (like VS_014 where Phase 1 was complete but undocumented)
- **Tracks real vs planned** implementation
- **Documents technical decisions** for future reference
- **Maintains single source of truth** in backlog

### Implementation Details Template
```markdown
**Phase X Complete** (YYYY-MM-DD HH:MM):
✅ Tests: 17/18 passing (54ms execution)
✅ Files Created:
  - `src/Darklands.Domain/Pathfinding/AStarAlgorithm.cs`
  - `tests/Domain/Pathfinding/AStarAlgorithmTests.cs`
⚠️ Deviations:
  - Used Darklands.Domain not Core/Domain path
  - Inline validation instead of separate validator
💡 Decisions:
  - MediatR pipeline handles validation
  - Integer math for determinism (100/141)
```

## 🔄 MANDATORY: Phased Implementation Protocol

**YOU MUST implement all features in strict phases:**

### Phase Progression (NO EXCEPTIONS)
```
Phase 1: Domain → Phase 2: Application → Phase 3: Infrastructure → Phase 4: Presentation
(Core Logic)       (Commands/Handlers)     (State/Services)         (UI/Views)
```

### Enforcement Rules
**NEVER:**
- Skip phases for "simple" features
- Start with UI for "quick demos"
- Combine phases to "save time"
- Proceed without GREEN tests

**ALWAYS:**
- Complete each phase before starting next
- Run tests: `[test command] --filter Category=[Phase]`
- Commit with phase markers: `feat(X): description [Phase X/4]`
- Follow reference implementations

### Phase Testing
- **Phase 1**: Unit tests (must pass in <100ms)
- **Phase 2**: Handler tests (<500ms)
- **Phase 3**: Integration tests (<2s)
- **Phase 4**: Manual testing in UI

## 📅 IMPORTANT: Date-Sensitive Documents

**ALWAYS run `date` command first when creating or updating:**
- Memory Bank files (`.claude/memory-bank/active/*.md`)
- Session logs (`.claude/memory-bank/session-log.md`)
- Post-mortems
- Backlog updates with completion dates
- Release notes
- Any document with timestamps

```bash
# Run this FIRST, ALWAYS:
date  # Get current date/time before creating/updating dated documents

# Then use that timestamp in your updates
# Example: "**Last Updated**: 2025-08-24 01:59"
```

**Automated in embody.ps1:**
- Script captures timestamp at start: `$scriptStartTime = Get-Date`
- Uses consistent timestamp throughout execution
- Prevents stale timestamps when script runs for extended time

## 🚦 Quality Gates & CI/CD

### Test Execution Scripts
```bash
# Quick feedback during development
./scripts/test/quick.ps1         # Architecture tests only

# Complete validation before PR
./scripts/test/full.ps1          # Staged execution
./scripts/test/full.ps1 -SkipSlow  # Skip performance tests

# MANDATORY before committing (full validation)
./scripts/core/build.ps1 test    # Build + all tests
```

## 📖 Git Workflow with Smart Sync

### 🎯 NEW: Automatic Sync Resolution
**ALWAYS use `git sync` instead of manual pull/rebase:**
```bash
git sync  # Automatically detects and handles squash merges, conflicts, etc.
```

**Or use the PR workflow:**
```bash
pr create   # Create PR from current branch
pr merge    # Merge PR and auto-sync dev/main
pr sync     # Same as git sync
pr status   # Check PR and sync status
```

**Essential**: Check branch status before starting work: `./scripts/git/branch-status-check.ps1`

## 📝 PR Creation Protocol

### CRITICAL: Include ALL Changes in PR Description
**PR descriptions must be comprehensive** - include ALL commits in the branch, not just the latest change or current session.

### Recommended Approach: Use --fill-verbose
```bash
# Automatically includes ALL commit messages and bodies
gh pr create --fill-verbose

# Or with custom title
gh pr create --fill-verbose --title "feat: comprehensive feature description"
```

### Alternative: Manual Comprehensive Summary
When custom formatting is needed:
```bash
# First, review ALL commits in the branch
git log main..HEAD --oneline

# Then create PR with complete description
gh pr create --title "feat: feature name" --body "$(cat <<'EOF'
## Summary
[Comprehensive summary of ALL changes in this PR]

## Changes Included
- Commit 1: [description from git log]
- Commit 2: [description from git log]
- Commit 3: [description from git log]
[List ALL commits in the branch]

## Impact
- [What these changes affect]
- [Why they were made]

## Testing
- [x] All tests pass
- [x] Manual testing completed
- [ ] Additional validation needed

🤖 Generated with [Claude Code](https://claude.ai/code)
EOF
)"
```

### PR Description Checklist
Before creating any PR, ensure:
- [ ] Reviewed entire branch history (`git log main..HEAD`)
- [ ] Description includes ALL commits, not just latest
- [ ] Impact and context are clear
- [ ] Test status is documented
- [ ] Breaking changes are highlighted (if any)

## 📦 PR Merge Strategy

### Default: Squash and Merge
When merging PRs to main, use **Squash and merge** by default:
```bash
# Via GitHub UI: Select "Squash and merge" 
# Via CLI:
gh pr merge --squash --delete-branch
```

**When to use**: 90% of PRs (feature implementations, bug fixes with WIP commits)
**When NOT to use**: Large refactors with meaningful intermediate steps

## Important Instruction Reminders
Do what has been asked; nothing more, nothing less.
NEVER create files unless they're absolutely necessary for achieving your goal.
ALWAYS prefer editing an existing file to creating a new one.
NEVER proactively create documentation files (*.md) or README files. Only create documentation files if explicitly requested by the User.