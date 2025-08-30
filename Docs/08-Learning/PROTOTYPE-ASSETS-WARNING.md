# ⚠️ CRITICAL: Prototype Assets Usage Warning

**Created**: 2025-08-30
**Status**: TEMPORARY PROTOTYPE ONLY

## ⚡ IMPORTANT LEGAL NOTICE

### Temporary Asset Usage for VS_008 Prototype

**Current Status**: 
- Using Shattered Pixel Dungeon sprites for INTERNAL PROTOTYPE ONLY
- These assets are GPL-3.0 licensed
- Must be replaced BEFORE any distribution

### Critical Rules:

1. **NEVER COMMIT SPD ASSETS TO GIT**
   - Add to .gitignore immediately
   - Keep in local `temp_assets/` folder only

2. **NEVER DISTRIBUTE**
   - No builds with SPD assets to anyone
   - No screenshots with SPD assets in public
   - Internal testing only

3. **MUST REPLACE BEFORE**:
   - Any public demo
   - Any git commit of VS_008
   - Any team distribution
   - Any video recording

### Replacement Plan:

**Phase 1 (Current)**: SPD sprites for rapid prototyping
- Test grid visualization
- Test movement mechanics  
- Test turn order display

**Phase 2 (Before Commit)**: Simple geometric placeholders
- Colored squares for characters
- Simple shapes for terrain
- Basic programmer art

**Phase 3 (Production)**: Original Darklands assets
- Commission original sprites
- Develop unique art style
- Full asset replacement

### Asset Tracking:

**SPD Assets Used (MUST REPLACE)**:
- [ ] warrior.png - Hero sprite
- [ ] rat.png - Basic enemy
- [ ] tiles_sewers.png - Grid tiles
- [ ] Any other SPD assets

**Replacement Status**:
- [ ] Hero placeholder created
- [ ] Enemy placeholder created  
- [ ] Tile placeholders created
- [ ] All SPD assets removed

### Verification Checklist:

Before ANY distribution or commit:
```bash
# Check for SPD assets
grep -r "shattered-pixel-dungeon" .
find . -name "*.png" | grep -E "(warrior|mage|rogue|rat)"

# Ensure temp_assets is gitignored
cat .gitignore | grep temp_assets
```

## 🔴 DEADLINE

**All SPD assets must be removed by**: Before first commit of VS_008 implementation

**Responsible**: Dev Engineer implementing VS_008

---

**THIS IS A LEGAL REQUIREMENT, NOT A SUGGESTION**