# Branch and Commit Decision Protocols for AI Personas

**Document Type**: Design Specification  
**Created**: 2025-08-21  
**Owner**: DevOps Engineer  
**Status**: Implemented  
**Related Work**: TD_055

## 🎯 Purpose

This document defines clear, actionable decision protocols for AI personas regarding git branching and commit strategies. It addresses the fundamental question: **"When should an AI persona create a new branch vs. continue on the current branch?"**

## 🚨 Problem Statement

The guidance **"new work = new branch"** is oversimplified and undefined. Real development scenarios require nuanced decisions:

- Is fixing a typo "new work"?
- What about continuing yesterday's TD work?
- How to handle discovered bugs while working on features?
- When is work "atomic" enough for a single commit?

**Previous State**: These decisions were claimed to be "obvious" but were actually undefined, causing inconsistent AI persona behavior.

## 🏗️ Architecture Overview

Our solution implements a **three-layer decision system**:

1. **Pre-commit Guidance** - Atomic commit education at decision time
2. **Branch Status Intelligence** - Context-aware branch lifecycle management  
3. **Persona Embodiment Protocols** - Strategic branch decisions at workflow boundaries

### Design Principles

- **Left-shift quality** - Provide guidance at decision points, not validation points
- **Context-aware decisions** - Consider PR status, branch freshness, work item alignment
- **Educational over restrictive** - Guide AI personas rather than block them
- **Automation where safe** - Automate cleanup, guide decisions

## 📋 Atomic Commit Decision Protocol

### Definition: Atomic Commit for BlockLife

**An atomic commit does exactly ONE logical thing that can be described in a single sentence.**

### Pre-commit Guidance System

**Implementation**: `.husky/pre-commit` hook provides educational checklist:

```bash
💡 AI Persona Reminder: Ensure Atomic Commits
   ✓ This commit does exactly ONE logical thing
   ✓ All staged files relate to the same change  
   ✓ Could be described in a single sentence
   ✓ Tests updated for this specific change only
```

### Atomic Commit Examples

#### ✅ Good Atomic Commits
```bash
feat(VS_003): add user authentication model
fix(BR_012): resolve null pointer in login validation  
tech(TD_042): extract authentication utilities
test(VS_003): add unit tests for authentication service
docs: update installation guide with new requirements
```

#### ❌ Poor Atomic Commits (Multiple Things)
```bash
feat(VS_003): add authentication + fix login bug + update tests
fix: resolve multiple issues in user service  
tech: refactor authentication and update documentation
```

### Commit Breakdown Strategy

**When staged changes do multiple things:**

1. **Use `git add -p`** - Stage changes interactively by hunks
2. **Separate concerns** - Create multiple commits for different logical changes
3. **Order logically** - Dependencies first, then features, then tests

**Example workflow:**
```bash
# Multiple changes staged
git add -p src/Auth/UserService.cs     # Stage only authentication logic
git commit -m "feat(VS_003): add user authentication model"

git add tests/Auth/UserServiceTests.cs  # Stage related tests
git commit -m "test(VS_003): add authentication model tests"

git add src/Auth/LoginController.cs     # Stage integration changes
git commit -m "feat(VS_003): integrate authentication with login flow"
```

## 🌿 Branch Decision Protocol

### Core Decision Tree

```
START: AI Persona needs to do work
│
├─ Currently on MAIN branch?
│  ├─ YES → Always create feature branch for work items
│  └─ NO → Continue to branch analysis...
│
├─ Is this a QUICK FIX (<30min, <3 files)?
│  ├─ YES → Stay on current branch (document scope in commit)
│  └─ NO → Continue...
│
├─ Is this a DIFFERENT WORK ITEM than current branch?
│  ├─ YES → Create new branch  
│  └─ NO → Continue...
│
├─ Is this a DIFFERENT PERSONA than current branch context?
│  ├─ YES → Create new branch
│  └─ NO → Continue...
│
├─ Will this work take MULTIPLE SESSIONS (>1 day)?
│  ├─ YES → Create new branch
│  └─ NO → Consider current branch
│
├─ DISCOVERED ISSUE while working on something else?
│  ├─ YES → Create new branch (fix/BR_XXX-description)
│  └─ NO → Continue current work
│
└─ DEFAULT → Use current branch but verify alignment
```

### Branch Naming Conventions

**Format**: `<type>/<work-item>-<description>`

#### Feature Branches
```bash
feat/VS_003-user-authentication
feat/VS_004-inventory-system
```

#### Technical Debt Branches  
```bash
tech/TD_042-consolidate-archives
tech/TD_055-branch-protocols
```

#### Bug Fix Branches
```bash
fix/BR_012-null-pointer-login
fix/BR_013-memory-leak-rendering
```

#### Hotfix Branches (Production Issues)
```bash
hotfix/critical-security-patch
hotfix/data-corruption-fix
```

### Work Item Alignment Rules

| Current Branch | New Work Item | Decision | Rationale |
|----------------|---------------|----------|-----------|
| `feat/VS_003-auth` | Continue VS_003 | ✅ Stay | Same work item |
| `feat/VS_003-auth` | Start TD_042 | ❌ New branch | Different work type |
| `feat/VS_003-auth` | Fix bug in VS_003 | ✅ Stay | Related to current work |
| `feat/VS_003-auth` | Fix unrelated bug | ❌ New branch | Separate concern |
| `main` | Any work item | ❌ New branch | Never work directly on main |

## 🔍 Branch Status Intelligence System

### Implementation

**Tools**: 
- `scripts/branch-status-check.ps1` - Comprehensive branch analysis
- `scripts/branch-cleanup.ps1` - Automated cleanup for merged PRs

### Branch Lifecycle States

#### 1. **Fresh Branch** (Just Created)
```
Status: New feature branch, no PR
Actions: Continue work, create PR when ready
```

#### 2. **Active Development** (Open PR)
```
Status: Feature branch with open PR
Actions: Continue work, respond to PR feedback
Considerations: Check for merge conflicts, review comments
```

#### 3. **Merged Branch** (PR Merged)
```
Status: Feature branch with merged PR
Actions: Cleanup recommended - switch to main, delete branch
Automation: scripts/branch-cleanup.ps1
```

#### 4. **Closed Branch** (PR Closed, Not Merged)
```
Status: Feature branch with closed PR  
Actions: Investigate why - work abandoned? Needs rework?
Risk: High - understand closure reason before continuing
```

#### 5. **Stale Branch** (Behind Main)
```
Status: Branch significantly behind main (>10 commits)
Actions: Rebase or create fresh branch
Risk: Merge conflicts likely
```

### Branch Status Check Integration

**Persona Embodiment Workflow**:
```bash
1. AI persona embodied
2. Run: scripts/branch-status-check.ps1
3. Analyze output and recommendations
4. Make informed branch decision
5. Proceed with work
```

**Example Output**:
```
🌿 Branch Status Analysis:
   Current Branch: feat/VS_003-authentication
   📋 PR Found: Add user authentication system
   🔗 URL: https://github.com/user/repo/pull/42
   🟡 PR OPEN - Check before continuing work
   
   🤔 Consider:
      - Are you continuing work on this PR?
      - Has PR received feedback requiring changes?
      - Should this work be in a different branch?
   
   🔄 Checking branch freshness...
   ⚠️  Branch is 3 commits behind main
      💡 Consider updating: git rebase origin/main
```

## 👥 Persona-Specific Protocols

### Persona Embodiment Decision Points

**When embodying a persona, check:**

1. **Work Item Alignment**
   ```bash
   Current Branch: feat/VS_003-authentication  
   New Work: TD_032 (persona documentation)
   Decision: Create new branch (different work item type)
   ```

2. **Context Switching**
   ```bash
   Previous Persona: Dev Engineer (implementation)
   New Persona: Test Specialist (testing)  
   Decision: May continue same branch if testing VS_003 features
   ```

3. **Multi-day Work Continuity**
   ```bash
   Yesterday's Work: TD_055 (branch protocols)
   Today's Session: Continue TD_055
   Decision: Continue existing branch if work incomplete
   ```

### Memory Bank Integration

**activeContext.md Enhancement**:
```markdown
## Current Work Context
- **Active Item**: TD_032 (DevOps documentation improvements)
- **Current Branch**: feat/TD_032-persona-routing
- **Branch Status**: Fresh, no PR yet
- **Persona**: DevOps Engineer  
- **Session Type**: Implementation

## Branch Alignment Check  
✅ Branch aligns with work item
✅ Branch created for this specific work
✅ No PR conflicts or merge issues
```

## 🛠️ Implementation Details

### Pre-commit Hook Enhancement

**File**: `.husky/pre-commit`

**Purpose**: Provide atomic commit guidance at decision time

**Implementation**:
```bash
#!/bin/sh
. "$(dirname "$0")/_/husky.sh"

# AI Persona Guidance - Atomic Commit Reminder
echo "💡 AI Persona Reminder: Ensure Atomic Commits"
echo "   ✓ This commit does exactly ONE logical thing"
echo "   ✓ All staged files relate to the same change"  
echo "   ✓ Could be described in a single sentence"
echo "   ✓ Tests updated for this specific change only"
echo ""
```

### Branch Status Checking

**Files**: 
- `scripts/branch-status-check.ps1` (Windows)
- `scripts/branch-status-check.sh` (Linux/Mac)

**Features**:
- GitHub CLI integration for PR status
- Branch freshness analysis (ahead/behind main)
- Automated cleanup recommendations
- Context-aware decision guidance

**Usage**:
```bash
# Check current branch status
./scripts/branch-status-check.ps1

# Output provides actionable recommendations
# Based on PR status, branch freshness, work alignment
```

### Automated Branch Cleanup

**File**: `scripts/branch-cleanup.ps1`

**Safety Features**:
- Verifies PR is merged before deletion
- Prevents accidental main branch deletion
- Handles local and remote branch cleanup
- Provides clear feedback and error handling

**Usage**:
```bash
# Cleanup current branch (if merged)
./scripts/branch-cleanup.ps1

# Cleanup specific branch
./scripts/branch-cleanup.ps1 feat/VS_003-authentication
```

## 📊 Decision Examples

### Scenario 1: Feature Development
```
Situation: Dev Engineer working on VS_003 (user authentication)
Current Branch: main
Decision: Create feat/VS_003-authentication
Rationale: New work item, multi-session work expected
```

### Scenario 2: Bug Discovery
```
Situation: Working on VS_003, discover unrelated bug
Current Branch: feat/VS_003-authentication  
Decision: Create fix/BR_XXX-specific-bug
Rationale: Separate concern, should not mix with feature work
```

### Scenario 3: Quick Documentation Fix
```
Situation: Notice typo in README while working on TD_042
Current Branch: tech/TD_042-consolidate-archives
Decision: Stay on current branch, fix in single commit
Rationale: Quick fix (<30min), related to current improvement work
```

### Scenario 4: Persona Switch
```
Situation: Dev Engineer finished implementation, Test Specialist takes over
Current Branch: feat/VS_003-authentication (open PR)
Decision: Continue same branch
Rationale: Testing the same work item, PR context maintained
```

### Scenario 5: Stale Branch Recovery
```
Situation: Returning to work after week, branch 15 commits behind
Current Branch: feat/VS_003-authentication (stale)
Decision: Create fresh branch feat/VS_003-authentication-v2
Rationale: Rebase conflicts likely, fresh start safer
```

## 🚀 Benefits

### For AI Personas
- **Clear decision criteria** - No ambiguity about when to branch
- **Context awareness** - Understanding of PR states and branch health
- **Automated guidance** - Real-time recommendations at decision points
- **Consistent workflow** - Predictable branch and commit patterns

### For Development Workflow  
- **Clean git history** - Atomic commits, logical branch structure
- **Reduced conflicts** - Better branch lifecycle management
- **Automated cleanup** - Merged branches cleaned automatically
- **Improved collaboration** - Clear PR lifecycle understanding

### For Project Management
- **Work item traceability** - Branches aligned with backlog items
- **Progress visibility** - Branch status reflects work status
- **Risk reduction** - Prevents work on stale or closed branches
- **Quality gates** - Consistent commit and branch standards

## 🔄 Future Enhancements

### Phase 1: Enhanced Integration (Implemented)
- ✅ Pre-commit atomic commit guidance
- ✅ Branch status intelligence system
- ✅ Automated cleanup for merged PRs
- ✅ **Branch alignment validation** (TD_058) - Semantic workflow intelligence

### Phase 2: Advanced Automation (Partially Implemented)
- ✅ **Work item alignment checking** (TD_058) - Implemented in pre-commit
- ✅ **Work type consistency validation** (TD_058) - Branch/commit type matching
- 🔄 Memory Bank integration for work item tracking
- 🔄 Automated branch naming suggestions
- 🔄 Cross-persona work handoff protocols
- 🔄 Integration with backlog management

### Phase 3: Intelligence Layer (Future)
- 🔄 Machine learning for branch decision optimization
- 🔄 Predictive conflict detection
- 🔄 Advanced work item alignment with backlog integration
- 🔄 Performance metrics and optimization

## 📝 Summary

This comprehensive protocol system transforms AI personas from reactive tools to intelligent workflow partners. By providing clear decision criteria, automated status checking, and educational guidance, we eliminate the ambiguity around "new work" and "atomic commits" while maintaining flexibility for real-world development scenarios.

**Key Achievements**:
- ✅ **Atomic Commit Definition** - Clear, actionable criteria with real-time guidance
- ✅ **Branch Decision Tree** - Comprehensive logic for all development scenarios  
- ✅ **Status Intelligence** - Context-aware branch lifecycle management
- ✅ **Automation Tools** - Scripts for status checking and cleanup
- ✅ **Persona Integration** - Workflow protocols for AI embodiment

**Result**: AI personas now have the intelligence to make proper git workflow decisions, leading to cleaner history, better collaboration, and reduced conflicts.