# Active Working Documents

This folder contains the most frequently accessed documents for daily development work.

## 📋 Contents

### [Backlog.md](Backlog.md)
**Purpose**: Single source of truth for all development work
- Current work items (VS, BR, TD)
- Priority levels (Critical, Important, Ideas)
- Owner assignments
- Status tracking

**Update Frequency**: Daily

### [Workflow.md](Workflow.md)
**Purpose**: Complete development workflow and processes
- Persona responsibilities
- Development phases
- Git workflow
- Testing procedures

**Update Frequency**: As processes evolve

## 🎯 Quick Actions

### Check What to Work On
```bash
# View current backlog items
cat Backlog.md | grep "^###"
```

### Find Your Items (as persona)
```bash
# Example for Dev Engineer
grep "Owner: Dev Engineer" Backlog.md
```

### Understand Process
- New to project? Start with Workflow.md
- Need specific process? Search within Workflow.md

## 📝 Maintenance

- **Backlog**: Updated by personas during work
- **Workflow**: Updated by Tech Lead when processes change
- Both documents should remain actionable and current

---

*These are living documents. Keep them lean, accurate, and actionable.*