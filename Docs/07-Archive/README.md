# Archive Organization Guide

**Last Updated**: 2025-09-13

## 📂 Directory Structure

```
07-Archive/
├── README.md                    # This file - navigation & protocol
├── extraction-queue.md          # Items needing pattern extraction  
├── 2025/                       # Current year
│   ├── 2025-01-January.md     # Monthly completed items
│   ├── 2025-02-February.md
│   ├── ...
│   ├── 2025-09-September.md   # Current month
│   └── 2025-Rejected.md       # All rejected items for year
└── 2024/
    └── 2024-Legacy.md          # Historical items
```

## 🔄 Archive Protocol

### When to Archive
Items are moved here when:
- **Status**: ✅ COMPLETED or ❌ REJECTED
- **Location**: From `Docs/01-Active/Backlog.md`
- **Timing**: Immediately upon completion/rejection

### Where Items Go
- **Completed Items** → `YYYY/YYYY-MM-MonthName.md` (month of completion)
- **Rejected Items** → `YYYY/YYYY-Rejected.md` (consolidated by year)
- **Extraction Candidates** → Noted in `extraction-queue.md`

### Archive Rules
1. **APPEND-ONLY** - Never modify existing archived items
2. **PRESERVE CONTEXT** - Keep full item history and details
3. **DATE ACCURATELY** - Use actual completion/rejection date
4. **TRACK EXTRACTION** - Note patterns to extract

## 📋 File Format Standards

### Monthly Completed Files (`YYYY-MM-MonthName.md`)
```markdown
# Completed Items - Month YYYY

## Summary
- **Items Completed**: X
- **Key Achievements**: [Brief highlights]
- **Patterns Identified**: [Notable learnings]

---

### [Type]_[Number]: Title
**Completed**: YYYY-MM-DD HH:MM
**Actual Time**: Xh (vs estimated Yh)
**Key Learning**: [One-line insight]
---
[Original item content preserved]
---
**Extraction Candidates**:
- [ ] Pattern: [What to extract]
- [ ] ADR: [Architecture decision to document]
---
```

### Rejected Items File (`YYYY-Rejected.md`)
```markdown
# Rejected Items - YYYY

## Summary
- **Items Rejected**: X
- **Common Reasons**: [Patterns in rejections]

---

### [Type]_[Number]: Title
**Rejected**: YYYY-MM-DD
**Reason**: [Why rejected]
**Alternative**: [What we did instead]
**Resurrect If**: [Conditions for reconsideration]
---
[Original proposal preserved for context]
---
```

### Extraction Queue (`extraction-queue.md`)
```markdown
# Pattern Extraction Queue

**Last Reviewed**: YYYY-MM-DD
**Items Pending**: X

## High Priority Extractions

### From [Type]_[Number] (Completed YYYY-MM-DD)
**Location**: YYYY/YYYY-MM-MonthName.md
**Pattern Type**: [ADR|Handbook|Test Pattern]
**Extract**: [Specific learning to document]
**Target Document**: [Where to add it]
- [ ] Extraction completed

## Medium Priority Extractions
[...]

## Low Priority Extractions
[...]
```

## 🤖 Automation Support

The `backlog-assistant` agent handles:
1. **Moving completed items** to correct monthly file
2. **Consolidating rejected items** by year
3. **Adding to extraction queue** when patterns detected
4. **Maintaining summaries** in each file

## 📊 Quick Stats Script

```bash
# Count items by month
find . -name "2025-*.md" -exec grep -c "^### " {} \; | paste -sd+ | bc

# Find items needing extraction
grep -r "Extraction Candidates" --include="*.md" | wc -l

# List rejected items
grep "^### " 2025/2025-Rejected.md
```

## 🔍 Finding Items

### By Completion Date
Navigate to: `YYYY/YYYY-MM-MonthName.md`

### By Item Number
```bash
grep -r "TD_049" --include="*.md"
```

### By Pattern/Learning
Check `extraction-queue.md` first, then search archives

## 📈 Monthly Review Process

At month end:
1. Review completed items for patterns
2. Update extraction queue
3. Create next month's file
4. Update monthly summary

## ⚠️ Critical Rules

1. **NEVER DELETE** archived items
2. **NEVER MODIFY** completed item content
3. **ALWAYS PRESERVE** full context
4. **ALWAYS DATE** with actual completion time