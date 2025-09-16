# Archive Management Protocol

## Purpose
Maintain a searchable, manageable archive system for completed work items without letting files grow unwieldy.

## Core Principles
1. **Size-based rotation** (1000 lines per archive file)
2. **Searchable index** for quick reference
3. **Manual rotation** with agent assistance
4. **Preserve complete context** when archiving

## 📊 Archive Thresholds

### Line Count Triggers
- **<800 lines**: Normal operation
- **800-999 lines**: Warning zone - prepare for rotation
- **1000 lines**: STOP - rotation required
- **>1000 lines**: Archive blocked until rotation

### Why 1000 Lines?
- Optimal for Git diffs
- Fast to search with grep
- Quick to load in editors
- ~40 items per file (manageable)

## 📁 File Structure

```
Docs/07-Archive/
├── ARCHIVE_INDEX.md           # Quick reference index
├── Completed_Backlog.md        # Active archive (current)
├── Completed_Backlog_001.md    # Rotated archive 1 (full)
├── Completed_Backlog_002.md    # Rotated archive 2 (full)
└── Completed_Backlog_003.md    # Rotated archive 3 (full)
```

## 🔄 Archive Workflow

### Step 1: Check Archive Size
```bash
wc -l Docs/07-Archive/Completed_Backlog.md
```

### Step 2: If Approaching 1000 Lines
1. **Finish current archiving batch**
2. **Check line count again**
3. **If >1000, proceed to rotation**

### Step 3: Archive Rotation Process
```bash
# 1. Determine next number
ls Docs/07-Archive/Completed_Backlog_*.md | tail -1
# Example: If 003 exists, next is 004

# 2. Rotate the file
mv Completed_Backlog.md Completed_Backlog_004.md

# 3. Create new active archive
echo "# Completed Backlog (Active)" > Completed_Backlog.md
echo "" >> Completed_Backlog.md
echo "Previous archives: See ARCHIVE_INDEX.md" >> Completed_Backlog.md
```

### Step 4: Update Archive Index
Add entry to ARCHIVE_INDEX.md:
```markdown
### Completed_Backlog_004.md (Lines 3001-4000)
**Date Range**: [Start] to [End]
**Notable Items**:
- [List major items archived]
```

## 🤖 Backlog-Archiver Agent Role (AUTO-ROTATION!)

### What the Agent Does Automatically
1. **Monitors archive size** before archiving
2. **AUTO-ROTATES at 1000+ lines** (zero friction!)
3. **Creates new archive files** as needed
4. **Updates ARCHIVE_INDEX.md** after rotation
5. **Archives items** with proper formatting
6. **Continues seamlessly** with new file

### Auto-Rotation Process
When archive exceeds 1000 lines:
- Finds next sequential number (_001, _002, etc.)
- Moves current archive to numbered file
- Creates fresh `Completed_Backlog.md`
- Updates index with new archive info
- Archives items to the new file
- **All automatic - zero user intervention!**

### What the Agent CANNOT Do
- Make strategic decisions about what to archive
- Modify archived content
- Delete archives
- Change rotation threshold

## 📋 Quick Reference Commands

### Find an Item
```bash
# Search all archives
grep -r "TD_042" Docs/07-Archive/

# Search specific archive
grep "VS_001" Docs/07-Archive/Completed_Backlog_001.md
```

### Check Archive Health
```bash
# Line counts for all archives
wc -l Docs/07-Archive/Completed_Backlog*.md

# Total archived items (rough estimate)
grep -r "^### [A-Z][A-Z]_" Docs/07-Archive/ | wc -l
```

### Archive Statistics
```bash
# Items per archive file
for file in Docs/07-Archive/Completed_Backlog*.md; do
  echo "$file: $(grep "^### [A-Z][A-Z]_" $file | wc -l) items"
done
```

## 🚨 Archive Rules

### NEVER
- ❌ Let archive exceed 1500 lines (hard limit)
- ❌ Delete old archives
- ❌ Modify archived content
- ❌ Archive without preserving full context

### ALWAYS
- ✅ Check size before bulk archiving
- ✅ Update index after rotation
- ✅ Preserve complete item history
- ✅ Use consistent naming (Completed_Backlog_NNN.md)

## 📊 Archive Metrics

### Typical Item Sizes
- **Small TD**: 15-20 lines
- **Medium VS**: 25-35 lines
- **Large BR with details**: 40-50 lines
- **Average**: ~25 lines

### Archive Capacity
- **Per file**: ~40 items (at 1000 lines)
- **Search time**: <1 second per file
- **Rotation frequency**: ~Every 2-3 weeks

## 🔍 Finding Archived Items

### By Item Number
1. Check ARCHIVE_INDEX.md for ranges
2. Open specific archive file
3. Search for item number

### By Date
1. Check ARCHIVE_INDEX.md date ranges
2. Narrow to 1-2 files
3. Search within those files

### By Feature/Topic
1. Use grep across all archives
2. Check notable items in index
3. Follow cross-references

## Example Archive Entry

```markdown
### TD_042: Over-Engineered DDD Implementation
**Archived**: 2025-09-16
**Final Status**: Completed
---
[Original complete item content preserved here]
---
```

## Maintenance Schedule

### Weekly
- Check active archive size
- Update index if needed

### Monthly
- Review archive health
- Consolidate if needed
- Update statistics

### Quarterly
- Archive metrics review
- Process improvements
- Index regeneration if needed