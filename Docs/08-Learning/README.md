# Learning Resources Directory

**Purpose**: Extract patterns and insights from existing games to inform Darklands design

## 📚 Current Learning Sources

### Shattered Pixel Dungeon (SPD)
A successful open-source roguelike with time-unit combat system similar to our vision.

#### Documents Created:

1. **[SPD-High-Level-Flow.md](./SPD-High-Level-Flow.md)**
   - Control flow and data flow architecture
   - User stories and turn processing

2. **[SPD-Analysis.md](./SPD-Analysis.md)**
   - Detailed architecture patterns
   - Time-unit system breakdown
   - Buff/AI/Combat mechanics

3. **[SPD-Combat-Extraction.md](./SPD-Combat-Extraction.md)**
   - Specific combat code patterns
   - C# translations of key concepts
   - Implementation ordering suggestions

4. **[PROTOTYPE-ASSETS-WARNING.md](./PROTOTYPE-ASSETS-WARNING.md)** ⚠️
   - CRITICAL legal requirements
   - Asset replacement tracking
   - Verification checklist

5. **[VS_008-Sprite-Setup.md](./VS_008-Sprite-Setup.md)**
   - Temporary asset usage guide
   - Godot scene setup
   - Replacement strategy

## 🎯 Key Learnings from SPD

### Architecture Patterns:
- **Time-Unit System**: Float-based timing enables variable speeds naturally
- **Actor Processing**: Priority queue ensures deterministic turn order
- **State Machine AI**: Clean, testable enemy behavior
- **Buff as Actor**: Status effects that tick alongside characters

### What We SHOULD Adopt:
- Time-unit concept (if it fits our vision)
- Priority system for simultaneous actions
- Clean state machine patterns for AI
- Multi-stage damage pipeline

### What We MUST NOT Do:
- ❌ Copy any GPL-licensed code directly
- ❌ Use SPD assets in any distributed build
- ❌ Commit temporary prototype assets to Git
- ❌ Share screenshots with SPD sprites

## 📐 Application to Darklands

### For VS_008 (Grid Visualization):
1. Use temporary SPD sprites for rapid prototyping ONLY
2. Replace with geometric shapes before ANY commit
3. Focus on mechanics validation, not visuals
4. Document all temporary asset usage

### For Future Combat System:
- Reference SPD's time management patterns
- Consider float-based vs integer time units
- Design our own unique buff system
- Create original AI behavior patterns

## ⚠️ Legal Compliance

**GPL-3.0 Implications**:
- Learning from architecture = ✅ OK
- Using actual code = ❌ Makes entire project GPL
- Temporary local prototyping = ⚠️ Gray area, must replace
- Distribution with GPL assets = ❌ ILLEGAL without GPL compliance

## 🔍 Other Games to Study

Potential future learning sources:
- **Caves of Qud** - Complex turn-based combat
- **Into the Breach** - Deterministic tactical combat
- **Slay the Spire** - Card-based combat timing
- **FTL** - Real-time with pause mechanics

## 📝 How to Use This Directory

1. **For Design Decisions**: Reference patterns, don't copy
2. **For Implementation**: Use as inspiration, write original code
3. **For Prototyping**: Follow asset replacement protocol strictly
4. **For Learning**: Extract high-level concepts, not specifics

---

**Remember**: These are learning resources. Darklands must be its own unique game with original implementation and assets.