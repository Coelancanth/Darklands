# Backlog Aging Protocol

## Purpose
Prevent backlog accumulation of stale items that will never be implemented. Force honest prioritization through time-based aging.

## Protocol Start Date
**Effective**: 2025-08-22
**Initial Reset**: All existing items reset to Day 0 on 2025-08-22

## The 3-10 Rule

### Stage 1: Active (0-3 days in Backlog.md)
- **Location**: `Docs/01-Active/Backlog.md`
- **Status**: Active consideration
- **Action**: Work on it or let it age

### Stage 2: Backup (3-10 days total)
- **Location**: `Docs/01-Active/Backup.md`  
- **Status**: On probation - prove it's worth keeping
- **Action**: Rescue back to Backlog or let it expire

### Stage 3: Deleted (10+ days total)
- **Location**: Git history only
- **Status**: Expired - wasn't important enough
- **Action**: Can be recreated if truly needed

## Aging Rules by Item Type

### VS (Vertical Slices)
- **Age trigger**: Last status update date
- **3-day move**: If status unchanged for 3 days → Backup
- **10-day delete**: If untouched for 7 more days → Delete
- **Exception**: Status = "In Progress" doesn't age

### TD (Technical Debt)
- **Age trigger**: Created date (not status)
- **3-day move**: If not started within 3 days → Backup
- **10-day delete**: If not rescued within 7 more days → Delete
- **Exception**: Complexity score ≥8 gets double time (6 days before move)

### BR (Bug Reports)
- **Age trigger**: Created date
- **NO AGING**: Bugs don't expire - they're either fixed or not
- **Exception**: Can manually move to Backup if determined "won't fix"

## Status That Prevents Aging

Items with these statuses NEVER age out:
- `In Progress` - Someone is actively working on it
- `Blocked` - Waiting on external dependency
- `Testing` - In verification phase
- `Review` - Awaiting review/approval

## Rescue Protocol

### How to Rescue from Backup
1. **Justify**: Add note explaining why it's still needed
2. **Re-prioritize**: Must be Important or Critical (not Ideas)
3. **Assign owner**: Must have clear ownership
4. **Update status**: Must show progress plan

Example:
```markdown
**Rescued**: 2025-08-25 - Still needed for Q4 feature launch
**New Priority**: Important
**Owner**: Dev Engineer
**Next Step**: Start implementation Monday
```

## Implementation Timestamps

Every item MUST have these dates:
```markdown
**Created**: 2025-08-22        # When first added
**Last Updated**: 2025-08-22   # When status/content changed  
**Reset**: 2025-08-22          # If aging clock was reset (optional)
**Moved to Backup**: 2025-08-25 # When aged out (if applicable)
```

### Aging Calculation
- Use **Last Updated** date for aging (not Created)
- If item has **Reset** date, use that instead
- Status changes reset the aging clock

## Manual Aging Process

### Daily Check (takes 2 minutes)
```bash
# Check items older than 3 days
grep -B2 "Created.*2025-08-19" Docs/01-Active/Backlog.md

# Check backup items older than 10 days total
grep -B2 "Created.*2025-08-12" Docs/01-Active/Backup.md
```

### Weekly Cleanup (every Monday)
1. Move 3+ day old items from Backlog → Backup
2. Delete 10+ day old items from Backup
3. Update "Last Cleaned" timestamp in both files

## Backup.md Structure

```markdown
# Backlog Backup

> Items on probation - will be deleted after 10 days total unless rescued
> Last Cleaned: 2025-08-22

## Aging Items (3-10 days old)

### [Item moved from Backlog]
**Created**: [original date]
**Moved to Backup**: [date moved]
**Will Delete**: [date + 7 days]
[Original item content]

## Won't Fix / Deprecated

### [Items explicitly marked won't fix]
**Reason**: [Why we're not doing this]
```

## Exceptions and Edge Cases

### Items That NEVER Age
1. **Compliance/Legal**: Regulatory requirements
2. **Security**: CVEs and security vulnerabilities  
3. **Data Loss**: Anything that could lose user data
4. **Commitments**: Items promised to stakeholders

Mark these with: `**No-Age**: [Reason]`

### Fast-Track Deletion
Items can be deleted immediately if:
- Duplicate of existing item
- No longer relevant (requirement changed)
- Was created in error

### Resurrection
Deleted items CAN be recreated if:
- New information makes them relevant
- Priority genuinely changed
- Was deleted by mistake

But must include: `**Resurrected**: Previously TD_XXX, deleted on [date]`

## Metrics to Track

### Health Indicators
- **Good**: <20 total items across Backlog + Backup
- **Warning**: 20-40 items (getting cluttered)
- **Bad**: >40 items (not being honest about priorities)

### Aging Velocity
- **Healthy**: 30% of items complete within 3 days
- **OK**: 50% move to backup (some speculation is fine)
- **Unhealthy**: >70% age out (creating too many items)

## Automation Opportunity

Future TD: Create script to:
1. Auto-move items based on dates
2. Generate aging report
3. Send reminders for rescue decisions
4. Archive deleted items to separate file

## Philosophy

> "If it's not worth doing in 3 days, it's probably not worth doing at all."

The aging protocol forces honest questions:
- Is this really important?
- Will we actually do this?
- Are we being realistic about capacity?

Better to have 10 items you'll actually do than 100 items that make you feel guilty.

## Common Patterns

### The "Someday" Trap
❌ Creating items for "future considerations"
✅ Only create items you'll act on this week

### The "Might Need" Trap
❌ Keeping items "just in case"
✅ Delete it - you can recreate if truly needed

### The "Too Big" Trap
❌ Items that never start because they're overwhelming
✅ Break into smaller items that can be done in 3 days

### The "Pet Feature" Trap
❌ Keeping your favorite idea alive forever
✅ If no one's building it, it's not that important

---

*Protocol Version: 1.0*
*Last Updated: 2025-08-22*
*Remember: A clean backlog is a productive backlog*