# Toolchain & Development Tools Roadmap

**Purpose**: Designer/Developer tooling roadmap - Odin Inspector-inspired editor tools for content creation without programmer intervention.

**Last Updated**: 2025-10-10 (Product Owner: Updated to component-based architecture + auto-wired i18n - enables flexible item composition via checkboxes, zero manual CSV editing)

**Parent Document**: [Roadmap.md](Roadmap.md#toolchain--development-tools) - Main project roadmap

---

## Quick Navigation

**Tools**:
- [Vision & Philosophy](#vision--philosophy)
- [Current State](#current-state)
- [Item Editor](#item-editor-odin-inspector-like)
- [Translation Manager](#translation-manager-visual-csv-editor)
- [Template Browser](#template-browser-advanced-search--batch-operations)
- [WorldGen Debug Panel](#worldgen-debug-panel-vs_031)

**Integration**:
- [Integration with Game Systems](#integration-with-game-systems)
- [Phasing Strategy](#phasing-strategy)

---

## Vision & Philosophy

**Vision**: Odin Inspector-inspired editor tools - empower designers to create 100+ items/actors/translations without programmer intervention, with seamless i18n integration and balance validation.

**Philosophy**:
- **Designer Autonomy** - No programmer bottleneck for content iteration
- **Fast Feedback** - Hot-reload works (<5 seconds from edit → test)
- **Context Awareness** - Show translations inline, balance comparisons, usage tracking
- **Validation First** - Catch errors BEFORE saving (missing keys, invalid stats, broken references)
- **Scalability** - Tools enable 100+ items where Godot Inspector fails

**Design Principles**:
1. **Visual-First** - Designers see sprites, translations, balance graphs (not just property names)
2. **Inline Integration** - Edit translations WITHOUT leaving Item Editor (no tool juggling)
3. **Intelligent Defaults** - Copy existing item → auto-suggest similar stats
4. **Batch-Aware** - Create 10 variants with one operation (not 10 manual operations)

---

## Validation Strategy (Spans All Tools)

**Philosophy**: **Fail-fast at every layer** - catch errors at design-time (Godot Inspector), edit-time (Item Editor), and build-time (CI/CD). Never allow invalid data to reach production.

### Three-Layer Validation

**Layer 1: Design-Time Validation** (Godot Inspector + ADR-006):
- **What**: Type checking via `[Export]` attributes
- **When**: While designer edits .tres files in Godot Inspector
- **Examples**:
  - Can't assign string to int property (type mismatch)
  - Color picker for Color properties (visual validation)
  - Resource picker for Texture2D (ensures valid sprite paths)
- **Enforced By**: Godot Engine (automatic)

**Layer 2: Edit-Time Validation** (Item Editor - Phase 2):
- **What**: Game-specific business rule validation
- **When**: Before [✓ Save] button in Item Editor
- **Examples**:
  - Weapon requires Equippable component (dependency validation)
  - NameKey "ITEM_IRON_SWORD" not duplicate (uniqueness check)
  - English translation required (fallback language check)
  - Damage range 1-100 (stat bounds validation)
- **Enforced By**: Item Editor validation logic (real-time feedback)

**Layer 3: Build-Time Validation** (CI/CD):
- **What**: Cross-file integrity + schema validation
- **When**: Pre-commit hooks, CI/CD pipeline
- **Examples**:
  - All NameKeys exist in en.csv (reference integrity)
  - No duplicate template IDs across files (global uniqueness)
  - All MaxHealth > 0 (data sanity checks)
  - Optional: JSON Schema validation (hybrid approach - see below)
- **Enforced By**: Bash scripts (`.husky/pre-commit` or GitHub Actions)

**Current State**: Layers 1 + 3 implemented (ADR-006), Layer 2 planned for Phase 2 (Item Editor).

---

### JSON Schema Integration (Optional - Hybrid Approach)

**Challenge**: Godot Resources (.tres format) ≠ JSON (can't directly use JSON Schema)

**Hybrid Solution** (adopt if template count > 100):
1. **Keep .tres format** (designer UX, hot-reload, Godot integration)
2. **Export .tres → JSON** (temp conversion for validation)
3. **Validate JSON** against schema (ActorTemplate.schema.json, ItemTemplate.schema.json)
4. **Report schema errors** (integrated into build-time validation)

**Workflow**:
```bash
# scripts/validate-templates-schema.sh (NEW - Phase 4+)

for template in data/**/*.tres; do
    # Convert .tres → temp JSON
    godot --headless --script scripts/export-tres-to-json.gd "$template"

    # Validate JSON against schema
    jsonschema -i "$template.json" schemas/ItemTemplate.schema.json

    # Cleanup temp files
    rm "$template.json"
done
```

**JSON Schema Benefits**:
- ✅ **Standardized validation** (industry-standard approach)
- ✅ **Self-documenting** (schema IS the authoritative spec)
- ✅ **Tooling ecosystem** (JSON Schema validators, generators)
- ✅ **CI/CD integration** (many pre-built GitHub Actions)

**JSON Schema Trade-Offs**:
- ✅ **PRO**: More robust validation than bash scripts
- ✅ **PRO**: Schema serves as documentation
- ❌ **CON**: Requires .tres → JSON conversion step (adds complexity)
- ❌ **CON**: Doesn't replace Layer 2 validation (business rules need custom code)
- ❌ **CON**: Schema files to maintain (ItemTemplate.schema.json, ActorTemplate.schema.json)

**Decision Rule**: Adopt JSON Schema when:
- Template count > **100** (validation complexity justifies schema overhead)
- Multiple tools need validation (Template Browser, external modding tools)
- Team wants standardized documentation (schema as source of truth)

**Current Approach** (sufficient for Phase 1-3):
- Bash scripts for build-time validation (`.husky/pre-commit`)
- Code-based validation in GodotTemplateService (fail-fast loading)
- Item Editor validation in Phase 2 (real-time feedback)

**Related**: See [ADR-006 Validation Strategy](../../03-Reference/ADR/ADR-006-data-driven-entity-design.md#validation-strategy) for implementation details.

---

## Current State

**Foundation Complete**:
- ✅ **ADR-006: Data-Driven Entity Design** - Godot Resources (.tres files), hot-reload works, template inheritance
- ✅ **ADR-005: Internationalization Architecture** - Translation keys, en.csv system, tr() in Presentation
- ✅ **ActorTemplate System** - GodotTemplateService, validation, caching (VS_021 complete)

**Designer Experience TODAY** (Limited UX):
- **Create Actor**: Godot Inspector (works, but no balance comparison, no translation preview)
- **Edit Translation**: Text editor (en.csv) - no context, no validation, no search
- **Find Templates**: File browser (manual .tres file opening, no search/filter)
- **Batch Operations**: Manual (create 10 enemies = 10 manual .tres files)

**Designer Experience GOAL** (Odin Inspector-level):
- **Create Item**: Visual Item Editor (grouped properties, inline translations, live preview, DPS calculator)
- **Edit Translation**: Translation Manager (visual editor, shows which templates use key, validates duplicates)
- **Find Templates**: Template Browser (search by stat range, filter by type, tag system)
- **Batch Operations**: Batch wizard (create 10 rusty variants with one command)

---

## Item Editor (Odin Inspector-Like)

**Status**: Proposed (Build when 20+ items exist) | **Priority**: Critical (blocks content creation at scale)

**What**: Visual template editor for creating/editing **component-based ItemTemplates** with Odin Inspector-level UX. Items are composed from flexible components (Equippable, Weapon, Armor, Consumable, LightSource, Container) enabling maximum flexibility.

**Component-Based Architecture**:
```
ItemTemplate (base container)
    ├─ Base Properties: Id, NameKey, Sprite, Weight, GoldValue (always present)
    ├─ Optional Components (designer selects which ones to add):
    │   ☑ EquippableComponent? (can be equipped in slot)
    │   ☑ WeaponComponent? (deals damage in combat)
    │   ☑ ArmorComponent? (provides defense when equipped)
    │   ☑ ConsumableComponent? (can be consumed for effects)
    │   ☑ LightSourceComponent? (emits light radius)
    │   ☑ ContainerComponent? (provides extra inventory slots)
    │   ☑ FuelComponent? (consumes charges over time)
    └─  ☑ MagicComponent? (enchantments, special effects)

Examples:
  Iron Sword   = Equippable + Weapon
  Plate Armor  = Equippable + Armor
  Shield       = Equippable + Armor + Weapon (defense + bash attack!)
  Torch        = Equippable + Weapon + LightSource + Consumable (burns out)
  Backpack     = Equippable + Container (extra inventory space)
  Healing Pot  = Consumable ONLY (no Equippable = can't be equipped)
```

**Why Component-Based** (vs Inheritance):
- ✅ **Maximum flexibility** - Shield is BOTH armor AND weapon (can't do with inheritance)
- ✅ **Designer empowerment** - Create complex items by checking boxes (no code changes)
- ✅ **Emergent complexity** - Combine components to create ANY item behavior
- ✅ **Scales better** - 100+ items with varied behaviors (torch, shield, backpack)

**Why Item Editor Needed**:
- **Godot Inspector is generic** - Doesn't understand Darklands context (no balance comparison, no DPS calculator)
- **No translation preview** - Designer sets "NameKey: ITEM_IRON_SWORD" but can't see what it translates to
- **No validation** - Designer can save with missing sprite, invalid stats, broken translation keys (fails at runtime!)
- **No balance tools** - Is 12 damage balanced vs other swords? Designer guesses.

**Designer Pain Point Solved**:
> "I want to create a shield that provides defense AND can bash enemies. I also want to see how it compares to other defensive items WITHOUT opening 10 .tres files manually."

---

### Designer Workflow: Creating an Iron Sword

**Step 1: Open Item Editor**
```
Designer: Opens Godot Editor → Bottom panel → "Item Editor" tab
```

**Step 2: Create New Item**
```
Item Editor shows:

┌─────────────────────────────────────────────────────────────────┐
│ ITEM EDITOR                                            [? Help] │
├──────────────────┬──────────────────────────────────────────────┤
│ Template Browser │ [Create New Item]                            │
│                  │                                              │
│ 🔍 [Search...]   │ All items use ItemTemplate (component-based) │
│                  │ Select which components to add:              │
│ ▼ Weapons (5)    │                                              │
│   ● Iron Sword   │ Quick Templates:                             │
│   ○ Steel Sword  │ ○ Weapon (Equippable + Weapon)              │
│   ○ Rusty Dagger │ ○ Armor (Equippable + Armor)                │
│                  │ ○ Shield (Equippable + Armor + Weapon)      │
│ ▼ Armor (3)      │ ○ Torch (Equippable + Weapon + Light + Fuel)│
│   ○ Leather Cap  │ ● Custom (Select components manually)       │
│   ○ Plate Armor  │                                              │
│                  │ [Create] [Cancel]                            │
└──────────────────┴──────────────────────────────────────────────┘

Designer: Selects "Weapon" quick template → Click [Create]
(Editor auto-checks ☑ Equippable + ☑ Weapon components)
```

**Step 3: Component Selection & Property Editing with Auto-Wired i18n**
```
Item Editor shows:

┌─────────────────────────────────────────────────────────────────┐
│ New Item Template                                      [✓ Save] │
├─────────────────────────────────────────────────────────────────┤
│ ▼ BASE PROPERTIES (always present)                             │
├─────────────────────────────────────────────────────────────────┤
│ Template ID:   [iron_sword                    ]                 │
│                ↑ Designer types identifier (snake_case)         │
│                                                                 │
│ Display Name: ← USER-FACING TEXT (what player sees)            │
│   English:     [Iron Sword                    ] ✓ Required     │
│   Chinese:     [铁剑                          ] (Optional)      │
│   German:      [Eisenschwert                  ] (Optional)      │
│                                                                 │
│                Translation Key: ITEM_IRON_SWORD ← AUTO-GENERATED│
│                (Editor generates from Template ID + prefix)     │
│                                                                 │
│ Description:                                                    │
│   English:     [A sturdy iron blade, balanced ]                 │
│                [and reliable.                  ]                 │
│   Chinese:     [一把坚固的铁剑，平衡可靠。    ]                 │
│                                                                 │
│                Translation Key: DESC_ITEM_IRON_SWORD ← AUTO-GEN │
│                                                                 │
│ Sprite:        [Browse...] ← Drag PNG or browse                │
│ Weight:        [2.5 kg] (Slider 0.1───●──50.0)                 │
│ Gold Value:    [150   ] (Slider 0───●──10000)                  │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ ▼ COMPONENTS (Select which behaviors to add)                   │
├─────────────────────────────────────────────────────────────────┤
│ ☑ Equippable   [Configure ▼] ← Designer checked this           │
│     Equipment Slot: [MainHand ▼]                                │
│     Two-Handed:     ☐ No                                        │
│     Item Shape:     [0,0;1,0;1,1] (3-cell L-shape)             │
│     Stat Modifiers: STR +2, DEX 0, END 0, INT 0                │
│                                                                 │
│ ☑ Weapon       [Configure ▼] ← Designer checked this           │
│     Damage:        [12    ] (Slider 1───●──100)                │
│     Attack Time:   [100   ] (Slider 50───●──200)               │
│     Range:         [1     ] (Slider 1───●──15)                 │
│     Crit Chance:   [8%    ] (Slider 0.0───●──100.0)            │
│     Weapon Type:   [Sword ▼]                                    │
│     Damage Type:   [Physical ▼]                                 │
│                                                                 │
│ ☐ Armor        [+ Add Component]                                │
│ ☐ Consumable   [+ Add Component]                                │
│ ☐ Light Source [+ Add Component]                                │
│ ☐ Container    [+ Add Component]                                │
│ ☐ Fuel         [+ Add Component]                                │
│                                                                 │
│ [+ Add Custom Component...]                                     │
└─────────────────────────────────────────────────────────────────┘

Designer: Fills properties, clicks [✓ Save]

BEHIND THE SCENES (Auto-Wiring):
✅ Editor generates NameKey: "ITEM_IRON_SWORD" (from Template ID)
✅ Editor writes to iron_sword.tres with SubResources for components:
   - EquippableComponent (Slot: MainHand, Shape: "0,0;1,0;1,1")
   - WeaponComponent (Damage: 12, Range: 1, Type: Sword)
✅ Editor writes to en.csv: ITEM_IRON_SWORD,Iron Sword
✅ Editor writes to zh_CN.csv: ITEM_IRON_SWORD,铁剑
✅ All files updated atomically - NO manual CSV editing!

✅ Saved iron_sword.tres (component-based) + translations!
```

**Step 4: Component Validation (Before Save)**
```
Editor validates component combinations:

✅ VALID COMBINATIONS:
  ☑ Equippable + Weapon → Standard weapon (sword, axe)
  ☑ Equippable + Armor → Standard armor (helmet, chest)
  ☑ Equippable + Armor + Weapon → Shield (defense + bash attack)
  ☑ Equippable + Weapon + LightSource → Torch (weapon that lights)
  ☑ Consumable ONLY → Potion (can't be equipped, only consumed)

⚠️ INVALID COMBINATIONS:
  ❌ Weapon WITHOUT Equippable → Can't attack with unequippable item!
  ❌ Armor WITHOUT Equippable → Can't get defense from backpack armor!
  ❌ Container WITHOUT Equippable → Can't access extra slots unequipped!

If designer tries invalid combination:

⚠️ VALIDATION ERROR:
┌─────────────────────────────────────────────────────────────────┐
│ Invalid component combination                                   │
├─────────────────────────────────────────────────────────────────┤
│ ❌ WeaponComponent requires EquippableComponent!                │
│    → Can't attack with an item that can't be equipped          │
│                                                                 │
│ [Auto-Add Equippable] [Remove Weapon] [Cancel]                 │
└─────────────────────────────────────────────────────────────────┘

Designer: Clicks [Auto-Add Equippable] → Editor checks ☑ Equippable → ✅ Valid

If Template ID conflicts:

⚠️ VALIDATION ERROR:
┌─────────────────────────────────────────────────────────────────┐
│ Cannot save template                                            │
├─────────────────────────────────────────────────────────────────┤
│ ❌ Translation key 'ITEM_IRON_SWORD' already exists!            │
│    Conflicting template: iron_sword.tres                        │
│    → Choose a different Template ID                             │
│                                                                 │
│ [Fix Template ID] [Cancel]                                      │
└─────────────────────────────────────────────────────────────────┘

Designer: Changes Template ID to "steel_sword" → [✓ Save]
Editor: Auto-generates ITEM_STEEL_SWORD (no conflict!) → ✅ Saved
```

**Step 5: View in Context (Balance Comparison)**
```
Item Editor shows saved iron_sword.tres:

┌─────────────────────────────────────────────────────────────────┐
│ Iron Sword (WeaponTemplate)                  [Edit] [Duplicate] │
├───────────────────┬─────────────────────────────────────────────┤
│ ▼ LIVE PREVIEW    │ ▼ BALANCE COMPARISON                        │
│                   │                                             │
│  ┌──────┐         │ Damage vs Other Swords:                     │
│  │ /│   │  Iron   │   Rusty:  [▓▓░░░░░░░░] 5                   │
│  │/ │   │  Sword  │   Iron:   [▓▓▓▓▓▓░░░░] 12 ← YOU ARE HERE  │
│  └──────┘         │   Steel:  [▓▓▓▓▓▓▓░░░] 15                  │
│                   │   Master: [▓▓▓▓▓▓▓▓▓▓] 22                  │
│  12 DMG  │ 1 RNG  │                                             │
│  2.5kg   │ 8% CRT │ DPS (Damage ÷ Time):                        │
│                   │   Iron Sword: 0.12 DPS                      │
│ ✓ ITEM_IRON_SWORD │   Rank: 3rd / 5 weapons                     │
│   → "Iron Sword"  │                                             │
│   → "铁剑"        │ Gold Value vs Weight:                       │
│                   │   150g / 2.5kg = 60 g/kg                    │
│ ✓ All Valid!      │   (Average for tier-1 weapons)             │
├───────────────────┴─────────────────────────────────────────────┤
│ ▼ USAGE (ActorTemplates referencing this item)                 │
│   • warrior.tres (StartingMainHandId)                           │
│   • soldier.tres (StartingMainHandId)                           │
└─────────────────────────────────────────────────────────────────┘

Designer: Sees iron_sword compares well vs other swords (balanced!)
```

---

### Designer Workflow: Creating a Shield (Multi-Component Item)

**Goal**: Create a shield that provides defense AND can bash enemies (Armor + Weapon components).

**Step 1: Create Shield with Multiple Components**
```
Item Editor → [Create New Item] → Select "Shield" quick template → [Create]

Editor shows:

┌─────────────────────────────────────────────────────────────────┐
│ New Item Template (Shield)                             [✓ Save] │
├─────────────────────────────────────────────────────────────────┤
│ ▼ BASE PROPERTIES                                               │
│   Template ID:   [iron_shield            ]                      │
│   Display Name:  [Iron Shield] / [铁盾]                         │
│   Sprite:        [Browse...] → shield.png                       │
│   Weight:        [5 kg]                                          │
│   Gold Value:    [100]                                           │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│ ▼ COMPONENTS (Auto-selected from "Shield" template)            │
├─────────────────────────────────────────────────────────────────┤
│ ☑ Equippable   [Configure ▼]                                    │
│     Equipment Slot: [OffHand ▼] ← Shields go in off-hand       │
│     Two-Handed:     ☐ No                                        │
│     Item Shape:     [0,0;1,0;0,1] (L-shape, 3 cells)           │
│     Stat Modifiers: STR 0, DEX +1, END 0, INT 0                │
│                                                                 │
│ ☑ Armor        [Configure ▼] ← Provides defense!               │
│     Defense:       [8     ] (Slider 0───●──50)                 │
│     Armor Type:    [Mail ▼]                                     │
│     Fatigue:       [5     ] (Lower than plate armor)           │
│     Resistances:   Physical 15%, Fire 0%, Ice 0%, Poison 0%    │
│                                                                 │
│ ☑ Weapon       [Configure ▼] ← Can bash enemies!               │
│     Damage:        [3     ] (Slider - lower than sword)        │
│     Attack Time:   [120   ] (Slider - slower than sword)       │
│     Range:         [1     ]                                     │
│     Crit Chance:   [2%    ] (Very low)                          │
│     Weapon Type:   [Blunt ▼] ← Shield bash is blunt damage    │
│     Damage Type:   [Physical ▼]                                 │
│                                                                 │
│ ☐ Consumable   [+ Add Component]                                │
│ ☐ Light Source [+ Add Component]                                │
└─────────────────────────────────────────────────────────────────┘

Designer: [✓ Save]

✅ Created iron_shield.tres with 3 components:
   - EquippableComponent (OffHand slot)
   - ArmorComponent (8 defense, 5 fatigue)
   - WeaponComponent (3 bash damage)
✅ Translations saved to en.csv, zh_CN.csv
```

**Step 2: Test Shield in Game**
```
Player equips iron_shield:
  ✅ OffHand slot filled (can't dual-wield with shield)
  ✅ +8 Defense bonus (reduces incoming damage)
  ✅ Can attack with shield (bash for 3 blunt damage)
  ✅ +5 Fatigue penalty (action economy cost)

Combat scenario:
  - Enemy attacks: 15 damage - 8 defense = 7 damage taken (shield works!)
  - Player attacks with shield: 3 blunt damage (shield bash!)
  - Player attacks with sword: 12 slashing damage (MainHand weapon)
```

**Key Insight**: Component composition created a shield that's BOTH armor AND weapon, without creating a "ShieldTemplate" class. Designer checked 3 boxes (Equippable, Armor, Weapon) and got complex behavior!

---

### Designer Workflow: Bulk Creating Rusty Variants

**Problem**: Need to create 5 "rusty" weapon variants (rusty_sword, rusty_axe, rusty_dagger, rusty_spear, rusty_mace) with similar stats (low damage, cheap gold value).

**Solution: Batch Operations**

```
Item Editor → Select iron_sword.tres → Right-click → [Create Variants...]

┌─────────────────────────────────────────────────────────────────┐
│ CREATE VARIANTS FROM: iron_sword.tres                           │
├─────────────────────────────────────────────────────────────────┤
│ Variant Count: [5      ] (How many variants to create)         │
│                                                                 │
│ Naming Pattern:                                                 │
│   Prefix:  [rusty_   ]                                          │
│   Suffix:  [        ]                                           │
│                                                                 │
│ Property Adjustments:                                           │
│   Damage:      [×0.5] (Multiply) OR [-5] (Subtract)            │
│   Gold Value:  [×0.3] (Rusty items = 30% of original value)    │
│   Weight:      [×1.0] (No change)                               │
│                                                                 │
│ Translation Keys:                                               │
│   ○ Auto-generate (ITEM_RUSTY_IRON_SWORD, etc.)               │
│   ● Manual (I'll fill translations after)                      │
│                                                                 │
│ [Preview Changes] [Create Variants] [Cancel]                   │
└─────────────────────────────────────────────────────────────────┘

Designer: Clicks [Create Variants]

✅ Created 5 variants:
   - rusty_iron_sword.tres (Damage: 6, Gold: 45)
   - rusty_steel_sword.tres (Damage: 7.5, Gold: 60)
   - rusty_dagger.tres (Damage: 2.5, Gold: 15)
   - rusty_spear.tres (Damage: 5, Gold: 30)
   - rusty_mace.tres (Damage: 7, Gold: 50)

⚠️ Warning: 5 translation keys missing (ITEM_RUSTY_*, DESC_ITEM_RUSTY_*)
[Open Translation Manager] [I'll Do It Later]
```

**Result**: Designer created 5 variants in 30 seconds (vs 10+ minutes manually).

---

### Auto-Wiring i18n Architecture

**The Problem**: Manual translation key management is error-prone (typos, inconsistency, forgetting prefixes).

**The Solution**: Designer works with **user-facing text** (Display Name: "Iron Sword"), editor auto-generates **technical keys** (ITEM_IRON_SWORD) and auto-syncs **CSV files**.

**Auto-Generation Rules**:
```
Pattern: {PREFIX}_{TEMPLATE_ID_UPPERCASE}

Examples:
- ItemTemplate:  iron_sword → ITEM_IRON_SWORD
- WeaponTemplate: battle_axe → ITEM_BATTLE_AXE (inherits ItemTemplate prefix)
- ArmorTemplate:  plate_armor → ITEM_PLATE_ARMOR
- ActorTemplate:  goblin → ACTOR_GOBLIN
- Description:    iron_sword → DESC_ITEM_IRON_SWORD
```

**Designer Input** (what they type):
1. Template ID: `iron_sword` (identifier, snake_case)
2. Display Name (English): `Iron Sword` (user-facing text)
3. Display Name (Chinese): `铁剑` (optional, can add later)

**Auto-Generated** (behind the scenes):
1. NameKey: `ITEM_IRON_SWORD` ← Generated from Template ID + prefix
2. DescriptionKey: `DESC_ITEM_IRON_SWORD` ← Generated from Template ID + prefix
3. en.csv entry: `ITEM_IRON_SWORD,Iron Sword` ← Written automatically
4. zh_CN.csv entry: `ITEM_IRON_SWORD,铁剑` ← Written automatically

**Validation Before Save**:
```
✅ Check Template ID not empty
✅ Check English translation provided (fallback language required)
✅ Check no duplicate translation keys (prevents conflicts)
✅ Warn if duplicate display names (potential duplicate item)
```

**Key Insight**: Template ID is the **single source of truth**. From `iron_sword`, we deterministically generate translation keys, file paths, and CSV entries. Designer types the ID once, then focuses on content (display names, descriptions). Everything else is auto-wired.

---

### Features Summary

**Core Features**:
- ✅ **Component-based architecture** (select Equippable, Weapon, Armor, etc. via checkboxes)
- ✅ **Quick templates** (Weapon, Armor, Shield, Torch presets auto-select components)
- ✅ **Component validation** (e.g., Weapon requires Equippable, auto-fix suggestions)
- ✅ **Auto-wired i18n** (designer types "Iron Sword", editor generates ITEM_IRON_SWORD)
- ✅ **Auto-sync CSV** (writes to en.csv, zh_CN.csv automatically - NO manual editing!)
- ✅ Live preview (sprite + key stats + active components)
- ✅ Validation (invalid components, missing translations, duplicate keys)

**Advanced Features**:
- ✅ Balance comparison (DPS calculator, damage vs other weapons)
- ✅ Usage tracking (which ActorTemplates reference this item)
- ✅ Batch operations (create N variants with stat adjustments + component inheritance)
- ✅ Intelligent defaults (copy existing item → preserves component configuration)

**Component Examples**:
- Iron Sword = Equippable + Weapon (standard weapon)
- Plate Armor = Equippable + Armor (standard armor)
- **Shield = Equippable + Armor + Weapon** (defense + bash attack!)
- **Torch = Equippable + Weapon + LightSource + Consumable** (burns out after use)
- Backpack = Equippable + Container (extra inventory slots)
- Healing Potion = Consumable ONLY (can't be equipped)

**Integration**:
- Reads/writes: data/items/**/*.tres (ItemTemplate with component SubResources)
- Auto-generates: Translation keys (ITEM_*, DESC_ITEM_*) from Template ID
- Auto-syncs: en.csv, zh_CN.csv, de.csv (writes Display Name → CSV automatically)
- Validates: Component combinations, no duplicate keys, English required (fallback)
- Shows: Which ActorTemplates use this item (StartingMainHandId references)

---

## Translation Manager (Visual CSV Editor)

**Status**: Proposed (Build when 50+ keys exist) | **Priority**: Important (quality of life)

**What**: Visual CSV editor for godot_project/translations/en.csv with context awareness, usage tracking, and validation.

**Why**:
- **CSV is text-based** - Hard for non-programmers (no search, no validation, merge conflicts)
- **No context** - Can't see which templates use "ITEM_IRON_SWORD"
- **No validation** - Duplicate keys, missing translations not caught until runtime
- **Hard to collaborate** - Send CSV to translator → manual merge → error-prone

**Designer Pain Point Solved**:
> "I want to see ALL translation keys, search for 'ITEM_*' keys, see which templates use each key, and edit translations WITHOUT opening a text editor. I also want warnings for missing translations (e.g., key exists in en.csv but not zh_CN.csv)."

---

### Designer Workflow: Managing Translations

**Step 1: Open Translation Manager**
```
Godot Editor → Bottom panel → "Translation Manager" tab

┌─────────────────────────────────────────────────────────────────┐
│ TRANSLATION MANAGER                                   [? Help] │
├─────────────────┬───────────────────────────────────────────────┤
│ Key Browser     │ Translation Details                           │
│                 │                                               │
│ 🔍 [Search...]  │ (Select a key from browser to edit)          │
│                 │                                               │
│ ▼ ITEM_* (42)   │                                               │
│   ✓ ITEM_IRON_  │                                               │
│     SWORD       │                                               │
│   ⚠ ITEM_STEEL_ │                                               │
│     SWORD       │                                               │
│   ✓ ITEM_DAGGER │                                               │
│                 │                                               │
│ ▼ ACTOR_* (18)  │                                               │
│   ✓ ACTOR_GOB   │                                               │
│     LIN         │                                               │
│   ✓ ACTOR_PLAY  │                                               │
│     ER          │                                               │
│                 │                                               │
│ ▼ UI_* (12)     │                                               │
│ ▼ ERROR_* (8)   │                                               │
│                 │                                               │
│ Stats:          │                                               │
│ 80 keys total   │                                               │
│ 75 complete ✓   │                                               │
│ 5 missing ⚠     │                                               │
│                 │                                               │
│ [+ Add Key]     │                                               │
│ [📤 Export CSV] │                                               │
│ [📥 Import CSV] │                                               │
└─────────────────┴───────────────────────────────────────────────┘
```

**Step 2: Edit Translation**
```
Designer: Clicks "ITEM_STEEL_SWORD" (⚠ warning icon)

Translation Details panel shows:

┌─────────────────────────────────────────────────────────────────┐
│ Translation Details                                             │
├─────────────────────────────────────────────────────────────────┤
│ Key: ITEM_STEEL_SWORD                                           │
│                                                                 │
│ English (en):     [Steel Sword                             ] ✓  │
│ Chinese (zh_CN):  [                                        ] ⚠  │
│                   ← MISSING! Click to add                       │
│ German (de):      [Stahlschwert                            ] ✓  │
│                                                                 │
│ ▼ USED BY (2 templates)                                         │
│   • steel_sword.tres (NameKey)                                  │
│   • knight.tres (StartingMainHandId → steel_sword.tres)         │
│                                                                 │
│ [Save] [Revert] [Delete Key]                                   │
└─────────────────────────────────────────────────────────────────┘

Designer: Fills Chinese translation: "钢剑" → [Save]

✅ Saved to en.csv and zh_CN.csv
✅ Warning cleared (⚠ → ✓)
```

**Step 3: Bulk Export for Translator**
```
Designer: Wants to send translations to Chinese translator

Translation Manager → [📤 Export CSV]

┌─────────────────────────────────────────────────────────────────┐
│ EXPORT TRANSLATIONS                                             │
├─────────────────────────────────────────────────────────────────┤
│ Export Format:                                                  │
│ ● CSV (for translators)                                         │
│ ○ JSON (for localization services)                             │
│                                                                 │
│ Languages:                                                      │
│ ☑ English (en) - Source language                               │
│ ☑ Chinese (zh_CN) - Include                                    │
│ ☐ German (de) - Exclude                                        │
│                                                                 │
│ Filter:                                                         │
│ ○ All keys                                                      │
│ ● Missing translations only (5 keys)                           │
│                                                                 │
│ Output:                                                         │
│ [translations_missing_zh_CN.csv] [Browse...]                    │
│                                                                 │
│ [Export] [Cancel]                                               │
└─────────────────────────────────────────────────────────────────┘

Designer: [Export] → Sends translations_missing_zh_CN.csv to translator
Translator: Fills Chinese column → sends back
Designer: [📥 Import CSV] → Selects file → ✅ Imported 5 translations
```

---

### Features Summary

**Core Features**:
- ✅ Visual CSV editor (not raw text)
- ✅ Search/filter by key prefix (ITEM_*, ACTOR_*, UI_*)
- ✅ Context awareness (shows which templates use each key)
- ✅ Validation (duplicate keys, missing translations highlighted)

**Advanced Features**:
- ✅ Export/import for translator collaboration
- ✅ Bulk operations (add prefix to selected keys, bulk delete)
- ✅ Usage tracking (reverse lookup: key → templates)
- ✅ Diff view (see what changed since last commit)

**Integration**:
- Reads/writes: godot_project/translations/*.csv (en, zh_CN, de, etc.)
- Scans: data/items/**/*.tres, data/entities/**/*.tres (finds NameKey, DescriptionKey usage)
- Validates: Keys exist, no duplicates, no missing values

---

## Template Browser (Advanced Search & Batch Operations)

**Status**: Proposed (Build when 100+ templates exist) | **Priority**: Ideas (polish)

**What**: Advanced search and batch operations for ALL templates (ActorTemplate, ItemTemplate, etc.).

**Why**:
- **File browser doesn't scale** - Finding "all weapons with Damage > 10" requires opening 50+ .tres files manually
- **No batch operations** - Adjusting 20 weapons' gold values = 20 manual edits
- **No tagging** - Can't mark "tier-1", "rare", "heavy" items

**Designer Pain Point Solved**:
> "I want to find all heavy items (Weight > 10kg), tag them as 'heavy', then bulk adjust their gold values (+50% for heavy items). I can't do this with file browser."

---

### Designer Workflow: Bulk Stat Adjustment

**Step 1: Search for Heavy Items**
```
Template Browser → Search

┌─────────────────────────────────────────────────────────────────┐
│ TEMPLATE BROWSER                                      [Search] │
├─────────────────────────────────────────────────────────────────┤
│ Filters:                                                        │
│   Template Type: [WeaponTemplate ▼] [ArmorTemplate ▼] [All]    │
│   Weight:        [> 10kg        ]                               │
│   Damage:        [Any           ]                               │
│   Gold Value:    [Any           ]                               │
│   Tags:          [None          ]                               │
│                                                                 │
│ [Apply Filters]                                                 │
└─────────────────────────────────────────────────────────────────┘

Results (12 items):
☑ battle_axe.tres (Weight: 12kg, Gold: 200)
☑ warhammer.tres (Weight: 15kg, Gold: 250)
☑ greatsword.tres (Weight: 18kg, Gold: 300)
☑ plate_armor.tres (Weight: 25kg, Gold: 500)
... (8 more)

Designer: Selects all → Right-click → [Bulk Edit...]
```

**Step 2: Bulk Stat Adjustment**
```
Bulk Edit Dialog:

┌─────────────────────────────────────────────────────────────────┐
│ BULK EDIT (12 selected items)                                   │
├─────────────────────────────────────────────────────────────────┤
│ Property Adjustments:                                           │
│   Gold Value:  [× 1.5] (Multiply by 1.5 = +50%)                │
│   Weight:      [No change]                                      │
│   Damage:      [No change]                                      │
│                                                                 │
│ Tags:                                                           │
│   Add Tags:    [heavy, tier-2]                                 │
│   Remove Tags: []                                               │
│                                                                 │
│ Preview Changes:                                                │
│   battle_axe.tres: Gold 200 → 300 (+50%)                       │
│   warhammer.tres:  Gold 250 → 375 (+50%)                       │
│   greatsword.tres: Gold 300 → 450 (+50%)                       │
│   ... (9 more)                                                  │
│                                                                 │
│ [Apply] [Cancel]                                                │
└─────────────────────────────────────────────────────────────────┘

Designer: [Apply]

✅ Updated 12 items
✅ Tagged 12 items as "heavy", "tier-2"
```

---

### Features Summary

**Core Features**:
- ✅ Advanced search (by name, type, stat range, tags)
- ✅ Batch operations (bulk edit, bulk tag, bulk delete)
- ✅ Tag system (tier-1, heavy, rare, magical)
- ✅ Usage tracking (which ActorTemplates reference these items)

**Advanced Features**:
- ✅ Smart filters (combine: Damage > 10 AND Weight < 5kg)
- ✅ Saved searches (quick filter: "High DPS Light Weapons")
- ✅ Diff view (see what changed since last commit)
- ✅ Export selected (to CSV for external analysis)

**Integration**:
- Reads: data/items/**/*.tres, data/entities/**/*.tres
- Searches: By name, type, properties, tags
- Batches: Duplicate, bulk edit, bulk tag, export

---

## WorldGen Debug Panel (VS_031)

**Status**: Planned (After VS_029 Erosion) | **Priority**: Important (essential for worldgen tuning)

**What**: Real-time parameter tuning for world generation with stage-based incremental regeneration.

**Why**:
- **Parameter tuning is tedious** - Edit code → recompile → regenerate world (minutes per iteration)
- **Full regen is slow** - 2s for 512×512 world (vs 0.5s erosion-only regen)
- **No presets** - Designer guesses RiverDensity, Meandering, ValleyDepth values

**Detailed Roadmap**: See [Roadmap_World_Generation.md](WorldGen/0_Roadmap_World_Generation.md#vs_031-worldgen-debug-panel-real-time-parameter-tuning--planned)

**Features**:
- Real-time sliders (RiverDensity, Meandering, ValleyDepth)
- Stage-based regen (Erosion Only vs Full World)
- Preset system (Earth, Mountains, Desert, Islands)
- Layer toggles (Elevation, Temperature, Precipitation, Rivers)

---

## Integration with Game Systems

### Item Editor ↔ Equipment System (VS_032)

**Integration Points**:
- **Creates**: ItemTemplate, EquipmentTemplate, WeaponTemplate, ArmorTemplate (.tres files)
- **Edits**: All template properties (Item / Equipment / Weapon sections)
- **Validates**: Required fields (Sprite, NameKey), stat ranges (Damage 1-100), translation key existence
- **Shows**: Which ActorTemplates use this item (StartingMainHandId → iron_sword.tres)
- **Warns**: "Deleting iron_sword.tres will break 3 ActorTemplates!"

**Data Flow**:
```
Item Editor
    ↓ (creates/edits .tres files)
data/items/weapons/iron_sword.tres
    ↓ (loaded at startup via GodotTemplateService)
ITemplateService<ItemTemplate>
    ↓ (used by ActorFactory)
Actor entity (Domain) with equipped WeaponComponent
```

---

### Translation Manager ↔ i18n System (ADR-005)

**Integration Points**:
- **Reads/Writes**: godot_project/translations/en.csv (primary), zh_CN.csv, de.csv (others)
- **Validates**: Keys exist in en.csv, no duplicate keys, no missing values
- **Shows**: Which templates use each key (reverse lookup: ITEM_IRON_SWORD → iron_sword.tres)
- **Creates**: New translation keys (inline from Item Editor)

**Data Flow**:
```
Translation Manager
    ↓ (reads/writes CSV files)
godot_project/translations/en.csv
    ↓ (loaded by Godot at startup)
TranslationServer (Godot)
    ↓ (used by Presentation layer)
tr("ITEM_IRON_SWORD") → "Iron Sword" (or "铁剑" if locale=zh_CN)
```

---

### Template Browser ↔ All Templates

**Integration Points**:
- **Reads**: data/items/**/*.tres, data/entities/**/*.tres (all Godot Resource templates)
- **Searches**: By name, type, properties (Damage > 10), tags (heavy, tier-1)
- **Batches**: Duplicate selected, bulk stat adjustment, bulk tag assignment
- **Exports**: Selected templates to CSV (for external analysis)

**Data Flow**:
```
Template Browser
    ↓ (scans .tres files via GodotTemplateService)
ITemplateService<ItemTemplate>, ITemplateService<ActorTemplate>
    ↓ (provides read-only access to cached templates)
In-memory template cache (Dictionary<string, Template>)
```

---

## Phasing Strategy

### Phase 1: Foundation (After VS_032 Complete)

**Timeline**: Immediately after VS_032 (Equipment & Stats System) complete
**Effort**: ~0h (no tool development yet, just template creation)

**Deliverables**:
- **Component-based template system** (ItemTemplate + component classes)
- Component definitions: EquippableComponent, WeaponComponent, ArmorComponent, ConsumableComponent, LightSourceComponent, ContainerComponent
- Example templates (10 weapons, 5 armor, 2 shields, 2 torches, 5 consumables)
- Validation: Templates load correctly with SubResources, hot-reload works

**Designer Experience**:
- Uses Godot Inspector (limited UX, but functional)
- Creates items by adding SubResources for each component
- Edits component properties manually (no visual checkboxes yet)
- Manually creates translation keys in en.csv (no auto-wiring yet)

**Example .tres file (created manually)**:
```gdscript
# data/items/weapons/iron_sword.tres
[sub_resource type="EquippableComponent" id="1"]
Slot = 0  # MainHand

[sub_resource type="WeaponComponent" id="2"]
Damage = 12

[resource]
Id = "iron_sword"
NameKey = "ITEM_IRON_SWORD"
Equippable = SubResource("1")
Weapon = SubResource("2")
```

**Success Criteria**:
- ✅ 20+ component-based item templates created (proves system scales)
- ✅ Hot-reload works (<5 seconds edit → test)
- ✅ Shield with Armor + Weapon components works in-game (validates multi-component)
- ✅ Designer reports "Inspector is tedious but workable"

---

### Phase 2: Item Editor (When 20+ Items)

**Timeline**: When designer reports "Inspector is too tedious" (estimated: 20-30 items created)
**Effort**: ~20-30h (Godot EditorPlugin, custom UI, validation)

**Deliverables**:
- Item Editor plugin (bottom panel tab in Godot)
- **Component selection UI** (checkboxes for Equippable, Weapon, Armor, etc.)
- **Quick templates** (Weapon, Armor, Shield, Torch presets)
- **Component validation** (e.g., Weapon requires Equippable - auto-fix)
- **Auto-wired i18n** (designer types "Iron Sword", editor generates ITEM_IRON_SWORD + syncs CSV)
- **Auto-sync CSV files** (writes to en.csv, zh_CN.csv automatically - NO manual editing)
- Live preview (sprite + key stats + active components)
- Validation (invalid components, missing translations, duplicate keys)

**Designer Experience**:
- Opens Item Editor tab (no more Inspector juggling)
- Checks ☑ Equippable + ☑ Weapon boxes (component selection via UI)
- Sees component-specific properties (Equippable section, Weapon section)
- Types "Iron Sword" (Display Name) → Editor auto-generates ITEM_IRON_SWORD + writes to CSV
- **NEVER types translation keys manually** (auto-wired from Template ID)
- Gets validation BEFORE saving (invalid components, duplicate keys, missing English)

**Creating a Shield** (multi-component):
- Checks ☑ Equippable + ☑ Armor + ☑ Weapon (3 components!)
- Configures: OffHand slot, 8 defense, 3 bash damage
- [✓ Save] → Shield provides defense AND attacks enemies!

**Success Criteria**:
- ✅ Designer creates 10 items in 10 minutes (vs 30+ minutes with Inspector)
- ✅ Zero runtime errors from missing translation keys (auto-wiring + validation prevents errors)
- ✅ Zero manual CSV editing (auto-sync writes translations automatically)
- ✅ Designer reports "Item Editor is much faster and I never touch CSV files!"

---

### Phase 3: Translation Manager (When 50+ Keys)

**Timeline**: When en.csv becomes hard to manage manually (estimated: 50-70 keys)
**Effort**: ~15-20h (CSV parser, visual editor, usage tracking)

**Deliverables**:
- Translation Manager plugin (bottom panel tab)
- Visual CSV editor (search/filter by key prefix)
- Usage tracking (shows which templates use each key)
- Validation (duplicate keys, missing translations highlighted)
- Export/import for translator collaboration

**Designer Experience**:
- Opens Translation Manager tab (no more text editor)
- Searches for ITEM_* keys (instant filter)
- Sees which templates use "ITEM_IRON_SWORD" (context!)
- Edits translations inline (saves to en.csv automatically)

**Success Criteria**:
- ✅ Designer finds/edits translation in 30 seconds (vs 2+ minutes with text editor)
- ✅ Zero missing translation errors (validation catches before save)
- ✅ Translator collaboration works (export → edit → import workflow)

---

### Phase 4: Advanced Features (When 100+ Items)

**Timeline**: When template count exceeds 100 (estimated: late Phase 2 or Phase 3 of game development)
**Effort**: ~30-40h (batch operations, balance calculator, template browser)

**Deliverables**:
- Template Browser (advanced search, batch operations)
- Batch operations (create N variants, bulk stat adjustment)
- Balance calculator (DPS, gold value curves, power creep detection)
- Tag system (tier-1, heavy, rare, magical)

**Designer Experience**:
- Uses Template Browser for complex queries ("all heavy weapons with Damage > 15")
- Batch creates 10 rusty variants with one command
- Sees balance graphs (power curve, DPS distribution)
- Tags items for organization (tier-1, tier-2, rare)

**Success Criteria**:
- ✅ Designer manages 100+ items without overwhelming (search/filter essential)
- ✅ Batch operations save hours (10 variants in 1 minute vs 20+ minutes manual)
- ✅ Balance validation catches power creep (warnings for outliers)

---

### Phase 5+: Future Growth Systems (When Game Systems Expand)

**DEFERRED**: Not needed for initial MVP. Build when game design requires these systems.

#### Skill Tree Editor (When Skill System Added)

**Trigger**: When game design adds skill/ability system (not in current roadmap)
**Effort**: ~40-50h (node-based editor, dependency graph, effect library)

**What**: Visual node editor for designing skill trees/ability progressions
- Node-based UI (skills as nodes, dependencies as edges)
- Effect library (reusable ability effects: DealDamage, ApplyStatus, SpawnProjectile)
- Skill composition (combine effects like: Fireball = SpawnProjectile + AreaOfEffect + DealDamage)
- Complex dependencies ("unlock if Level ≥ 10 AND STR ≥ 15")

**Why Needed**:
- Skill trees are complex graphs (100+ nodes, interdependencies)
- Visual editor essential for non-programmers (text/JSON unworkable)
- Effect reusability critical (avoid "Fireball I", "Fireball II", "Fireball III" duplication)

**Inspired By**: tmp.md Section 3.2 - Modular ability systems (Diablo, Path of Exile)

---

#### Growth Curve Editor (When Character Progression Added)

**Trigger**: When game design adds leveling/XP system (not in current roadmap)
**Effort**: ~25-35h (curve visualization, formula editor, simulation tools)

**What**: Visual editor for character stat growth curves (HP, damage, XP requirements)
- Curve visualization (see HP growth 1-50, not just numbers)
- Formula editor (define growth as `BaseHP * (Level ^ 1.2)`)
- Real-time preview (adjust curve, see graph update immediately)
- Simulation mode (simulate 1-50 leveling, show TTK at each level)

**Why Needed**:
- Growth curves define pacing (flat numbers hide leveling feel)
- Visual feedback essential (designers iterate on curves, not formulas)
- Simulation validates balance (catch "Level 20 too weak" before implementation)

**Inspired By**: tmp.md Section 3.3 - Curve management with simulation (visualize balance issues)

---

**Decision**: Defer both systems until game design requires them. Focus toolchain on **items + combat** (current core loop). Add skill/growth tools ONLY when those systems validated and needed.

---

## Summary

**Toolchain Vision**: Odin Inspector-level UX for Darklands - designers create 100+ items, actors, translations without programmer bottleneck using **component-based architecture** + **auto-wired i18n**.

**Key Tools**:
1. **Item Editor** - **Component-based** visual editor (select Equippable + Weapon + Armor via checkboxes) with **auto-wired i18n** (designer types "Iron Sword", editor generates ITEM_IRON_SWORD + syncs CSV automatically)
2. **Translation Manager** - Visual CSV editor (context awareness, usage tracking, translator collaboration)
3. **Template Browser** - Advanced search & batch operations (search by stats, bulk edits, tagging)
4. **WorldGen Debug Panel** - Real-time parameter tuning (VS_031 in worldgen roadmap)

**Component-Based Architecture** (Critical Innovation #1):
- Items composed from **optional components** (Equippable, Weapon, Armor, Consumable, LightSource, Container)
- Designer **selects components via checkboxes** (no rigid class hierarchies!)
- **Maximum flexibility**: Shield = Equippable + Armor + Weapon (defense + bash attack!)
- **Emergent complexity**: Torch = Equippable + Weapon + LightSource + Consumable (burns out)
- **Component validation**: Weapon requires Equippable (auto-fix suggestions)

**Auto-Wiring i18n** (Critical Innovation #2):
- Designer types **Template ID** (`iron_sword`) + **Display Name** (`Iron Sword`, `铁剑`)
- Editor **auto-generates** translation keys (`ITEM_IRON_SWORD`, `DESC_ITEM_IRON_SWORD`)
- Editor **auto-syncs** CSV files (writes to en.csv, zh_CN.csv automatically)
- Designer **NEVER manually edits** CSV files or types translation keys
- **Validation before save** prevents duplicate keys, missing translations, runtime errors

**Integration**: Tools read/write .tres files with component SubResources, auto-generate translation keys from Template ID, validate component combinations, show usage tracking (which templates reference what).

**Phasing**: Build when designer pain becomes bottleneck:
- Phase 1: Foundation (0h - create component-based templates in Inspector)
- Phase 2: Item Editor (20-30h - when 20+ items exist) **← COMPONENTS + AUTO-WIRING HERE**
- Phase 3: Translation Manager (15-20h - when 50+ keys exist)
- Phase 4: Advanced Features (30-40h - when 100+ items exist)

**Success Metrics**:
- ✅ Designer creates 10 items in 10 minutes (vs 30+ minutes with Inspector)
- ✅ **Designer creates multi-component items** (shield, torch) by checking boxes (no code!)
- ✅ **Zero manual CSV editing** (auto-sync writes translations automatically)
- ✅ **Zero translation key typos** (auto-generated from Template ID, deterministic)
- ✅ **Zero runtime errors** (validation catches invalid components, missing keys, duplicates)

---

**Last Updated**: 2025-10-10
**Status**: Roadmap complete with component-based architecture, awaiting VS_032 (Equipment System) completion to begin Phase 1
**Owner**: Product Owner (roadmap), Tech Lead (Phase 2-4 implementation planning), Dev Engineer (implementation)

**Key Architectural Decision**: **Component-based composition** (vs inheritance hierarchies) enables maximum flexibility - designers create complex items (shield, torch, backpack) by selecting components via checkboxes, no code changes needed

---

*This roadmap provides product-level details for Darklands toolchain. For game system details, see [Roadmap.md](Roadmap.md).*
