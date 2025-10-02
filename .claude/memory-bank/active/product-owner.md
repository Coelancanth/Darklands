# Product Owner Memory Bank

**Purpose**: Value-focused principles and approval criteria. NOT for current tasks or session logs.

---

## 🎯 Core Responsibility

**Product Owner defines WHAT and WHY, not HOW.**

- Focus on user value and business goals
- Trust Tech Lead for architectural decisions
- Trust Dev Engineer for implementation details
- Approve based on: Does this deliver value? Is scope clear?

---

## ✅ Approval Criteria

Before approving a VS (Vertical Slice):

1. **Value Clear?** - Why does this matter to players/designers?
2. **Scope Defined?** - What's included? What's explicitly deferred?
3. **Designer Impact?** - Will this empower designers or create dependency?
4. **Complexity Justified?** - Is the effort worth the value?
5. **Dependencies Known?** - What blocks this? What does this block?

---

## 🎨 Designer Empowerment Principle

**Prefer solutions that reduce designer dependency on programmers.**

**Examples**:
- ✅ TileSet metadata (designers add items visually)
- ✅ Godot Inspector editing (tweak values without code)
- ❌ Hardcoded definitions (requires programmer for every change)
- ❌ JSON files (still requires manual file creation)

**Test**: "Can a designer add content without asking a programmer?"

---

## 📊 Value vs Complexity

**Priority Framework**:
- **Critical (🔥)**: Blocking other work, core gameplay broken
- **Important (📈)**: Current milestone features, high user value
- **Ideas (💡)**: Nice-to-have, future exploration

**Size Scoring**:
- **TD**: 1-2 hours (quick wins)
- **VS**: <2 days per slice (if larger, slice thinner!)
- **Epic**: 2-6 months (major features)

**Decision Rule**: Value ÷ Complexity = Priority

---

## 🔪 Story Slicing Discipline

**Every VS must be independently deliverable.**

- ✅ Vertical slices (UI → Core → Database)
- ✅ Each slice <2 days
- ✅ Each slice delivers user-visible value
- ❌ Horizontal slices (all database work, then all UI)
- ❌ Technical tasks that don't deliver player value

**Example**: "Inventory System" → Slice into "Add Item", "Remove Item", "Stack Items"

---

## 📝 Backlog Hygiene

**Backlog is the single source of truth for ALL work.**

- **Proposed**: Needs approval/breakdown
- **Approved**: Ready for Dev Engineer
- **In Progress**: Currently being implemented
- **Done**: Completed + archived

**Aging Protocol**: Review backlog every 3-10 days, move stale items to Ideas or archive.

---

**Last Updated**: 2025-10-02
