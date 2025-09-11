# Post-Mortem Archive Index

This index tracks all consolidated post-mortem archives. Each entry represents a batch of post-mortems that have been analyzed, had their lessons extracted, and been archived.

## Archive Format

Each archive follows this structure:
```
YYYY-MM-DD-Topic/
├── EXTRACTED_LESSONS.md or consolidation-summary.md
├── IMPACT_METRICS.md
└── [original post-mortem files]
```

## Archives

### 2025-09-11-UI-Architecture-Lessons
**Date**: 2025-09-11
**Count**: 9 post-mortems
**Period**: 2025-09-07 to 2025-09-11
**Key Lessons**: 
- Parent-child UI pattern for Godot
- Godot C# API casing requirements
- View consolidation opportunities

**Impact**: 
- 60+ lines of code eliminated
- 10-18 future bugs prevented
- 12-22x ROI on consolidation effort

**TD Items Created**: TD_034, TD_035, TD_036

---

## Consolidation Schedule

Per TD_036, post-mortems should be consolidated:
- Weekly (recommended: Fridays)
- Or when 5+ post-mortems accumulate
- Whichever comes first

## Quick Stats

- **Total Post-Mortems Archived**: 9
- **Total Archives**: 1
- **Average ROI**: 12-22x
- **Most Valuable Pattern Found**: Parent-child UI relationships