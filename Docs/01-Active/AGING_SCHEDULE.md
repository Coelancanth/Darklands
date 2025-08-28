# Current Aging Schedule

> Based on BACKLOG_AGING_PROTOCOL.md with 2025-08-22 reset

## Active Items (as of 2025-08-22)

### Critical Priority
- **BR_013**: CI Workflow Fails (Status: Done) - Won't age (completed)

### Important Priority  
- **TD_048**: FsCheck Migration - Will move to Backup on 2025-08-25 if not started
- **VS_003A**: Match-3 with Attributes - Will move to Backup on 2025-08-25 if not started

### Ideas Priority
- **TD_065**: Automate Memory Bank Rotation - Will move to Backup on 2025-08-25 if not started

## Aging Timeline

### 2025-08-25 (Monday)
Items that will move to Backup if unchanged:
- TD_048 (unless Dev Engineer starts work)
- VS_003A (unless Dev Engineer starts work)  
- TD_065 (unless DevOps Engineer starts work)

### 2025-09-01 (Next Monday)
Items in Backup that will be DELETED if not rescued:
- Any items that moved to Backup on 2025-08-25

## How to Prevent Aging

To keep an item active:
1. **Start work**: Change status to "In Progress"
2. **Update it**: Any meaningful update resets the clock
3. **Block it**: Status "Blocked" prevents aging
4. **Rescue it**: If moved to Backup, can rescue with justification

## Current Stats
- Total active items: 4
- At risk of aging: 3
- Protected (Done/In Progress): 1
- Days until first aging check: 3

---

*This schedule auto-updates based on Last Updated dates in Backlog.md*