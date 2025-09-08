# Reference Documentation

**Last Updated**: 2025-09-08  
**Purpose**: Navigate to the right documentation quickly

## 🎯 Quick Decision Tree

```
Need something?
     ↓
┌─────────────────────────────┐
│ Term or concept definition? │──Yes──→ 📖 GLOSSARY.md
└─────────┬───────────────────┘
         No
          ↓
┌─────────────────────────────┐
│ How to code/test/debug?     │──Yes──→ 📘 HANDBOOK.md
└─────────┬───────────────────┘
         No
          ↓
┌─────────────────────────────┐
│ Major architecture choice?  │──Yes──→ 📐 ADR/
└─────────┬───────────────────┘
         No
          ↓
    Check HANDBOOK.md
    (it's probably there)
```

## 📚 The Core Documents

### 1. **[HANDBOOK.md](HANDBOOK.md)** - Your Daily Companion
**~870 lines of streamlined daily reference**
- Architecture patterns & reference implementation
- Development workflow & git commands
- Testing patterns (especially LanguageExt)
- Common bug patterns & solutions
- Persona routing & work items
- Memory Bank usage
- Subagent verification

### 2. **[GLOSSARY.md](Glossary.md)** - Terms & Definitions
**The authoritative vocabulary**
- Game concepts (Turn, Tier, Merge)
- Technical terms
- Domain language
- **Rule**: Check here before naming anything

### 3. **[ADR Directory](ADR/)** - Architectural Decisions
**Major technical choices with rationale**
- ADR-001: Pattern Recognition Framework
- ADR-002: Persona System Architecture
- Check before implementing related features

### 4. **[PRODUCTION-PATTERNS.md](PRODUCTION-PATTERNS.md)** - Implementation Patterns
**Battle-tested patterns from production**
- Value object factories, DI containers, presenter coordination
- Technical debt patterns and grid system designs
- Extracted from HANDBOOK.md for focused reference

## 🗂️ Specialized References

### Testing Documentation
- **[Testing.md](Testing.md)** - Complete testing patterns and troubleshooting
- **[TEST_CATEGORIES_GUIDE.md](TEST_CATEGORIES_GUIDE.md)** - Test categorization system

### LanguageExt Documentation
- **[LanguageExt-Usage-Guide.md](LanguageExt-Usage-Guide.md)** - Complete v5 guide

### Context7 Library Documentation
**Location**: `scripts/context7/README.md`
- LanguageExt patterns
- MediatR usage
- Library-specific guidance
- Use `mcp__context7__get-library-docs` tool

### Move Block Reference Implementation
**Location**: `src/Features/Block/Move/`
- The gold standard pattern
- Copy this for new features
- Shows proper architecture

## 📦 What Got Consolidated

The HANDBOOK.md merges and replaces these files (now in `Docs/99-Deprecated/`):
- QuickReference.md (668 lines)
- Architecture.md (380 lines)  
- Patterns.md (310 lines)
- Standards.md (318 lines)
- Testing.md (298 lines)
- GitWorkflow.md (353 lines)
- MemoryBankProtocol.md (284 lines)
- SubagentVerification.md (113 lines)
- ClaudeCodeBestPractices.md (266 lines)

**Impact**: 89% reduction in documentation volume, 100% value retained

## 🎯 Usage Examples

### "How do I test Fin<T> results?"
→ HANDBOOK.md > Testing Patterns > LanguageExt Testing

### "What's a Turn vs Round?"
→ GLOSSARY.md > Game Concepts

### "Why did we choose Husky.NET?"
→ ADR/ or check Memory Bank decisions.md

### "How do I add a new feature?"
→ HANDBOOK.md > Development Workflow

### "What's the grid coordinate system?"
→ HANDBOOK.md > Core Architecture > Grid Coordinate System

## 💡 Philosophy

**Less is More**: One comprehensive handbook beats ten scattered guides

**Single Source of Truth**: Each piece of information lives in exactly one place

**Practical Focus**: Only document what developers actually need daily

---

*If you can't find it in HANDBOOK.md or GLOSSARY.md, it probably wasn't important enough to keep.*