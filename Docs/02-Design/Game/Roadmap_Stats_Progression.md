# Stats & Progression Roadmap

**Purpose**: Detailed technical roadmap for Darklands character stats and progression systems - Darklands (1992) inspired skill-based progression with no experience levels.

**Last Updated**: 2025-10-09 23:52 (Product Owner: Added Stat Training System - Modest use-based growth (+20% cap), authentic Darklands pattern!)

**Parent Document**: [Roadmap.md](Roadmap.md#progression) - Main project roadmap

---

## Quick Navigation

**Core Systems**:
- [Vision & Philosophy](#vision--philosophy)
- [Current State](#current-state)
- [Equipment & Stats System](#equipment--stats-system-vs_032)
- [Fatigue System](#fatigue-system-planned)
- [Healing & Regeneration System](#healing--regeneration-system-planned)
- [Stat Training System](#stat-training-system-planned)
- [Morale System](#morale-system-planned)
- [Renown & Experience Display](#renown--experience-display-planned)
- [Proficiency System](#proficiency-system-planned)
- [Character Aging](#character-aging--time-pressure-planned)

**Integration**:
- [Combat Integration](#combat-integration)
- [Inventory Integration](#inventory-integration)

---

## Vision & Philosophy

**Vision**: Darklands (1992) inspired progression - no experience levels, skill improves through use, equipment defines capabilities, aging creates natural run duration.

**Philosophy**:
- **No levels** - Characters don't have "Level 15", they have weapon proficiencies and equipment
- **Use-based progression** - Use a weapon type, get better with that weapon type (faster actions, more reliable)
- **Equipment > Stats** - A peasant with plate armor is tougher than a "Level 10 Warrior" in leather
- **Horizontal progression** - More options (new weapon types, abilities) not vertical (bigger damage numbers)
- **Time pressure** - Character aging creates natural 2-4 hour run duration (stat degradation forces retirement/death)

**Design Principles**:
1. **Equipment first** - Build variety comes from gear choices (heavy tank vs light skirmisher)
2. **Weight matters** - Heavy armor protects more but slows all actions (time-unit cost increase)
3. **Realistic simulation** - Armor layers (padding, mail, plate) interact with damage types
4. **Learning by doing** - Play more, get better through repetition (Mount & Blade pattern)

---

## Current State

**Completed Systems**:
- ✅ **Actor Component Pattern** (Actor.cs) - Flexible composition (health, weapons, equipment components)
- ✅ **Basic Combat** (VS_020) - Click-to-attack, damage application, range validation
- ✅ **Spatial Inventory** (VS_018) - Grid-based storage, multi-cell items, rotation
- ✅ **Data-Driven Templates** (VS_021) - ActorTemplate system with .tres files

**Missing Systems**:
- ❌ **Character Attributes** - No strength/dexterity/endurance/intelligence
- ❌ **Equipment Slots** - Can't equip items (no main hand, off hand, armor slots)
- ❌ **Stat Modifiers** - Equipment doesn't affect stats/combat
- ❌ **Fatigue System** - No action economy resource (Battle Brothers pattern)
- ❌ **Morale System** - No flee/surrender mechanics
- ❌ **Proficiency Tracking** - No weapon skill progression
- ❌ **Weight System** - No time-unit cost from heavy armor/weapons

**Architectural Foundation**:
- ✅ Component pattern supports stats (can add IAttributesComponent)
- ✅ Item system ready (Item.cs has properties, just needs stat modifiers)
- ✅ Combat system extensible (ExecuteAttackCommand can read equipment)
- ❌ **Blocker**: No equipment slots = can't affect combat yet!

---

## Equipment & Stats System (VS_032)

### Overview

**Status**: Proposed | **Size**: L (12-16h) | **Priority**: CRITICAL (blocks proficiency, armor, build variety)
**Owner**: Product Owner → Tech Lead (breakdown) → Dev Engineer (implement)

**What**: Character attribute system + equipment slot system + stat modifiers from equipped items - foundation for build variety and combat depth.

**Why**:
- **Combat depth NOW** - Armor affects defense, weapons affect damage (tactical gear choices)
- **Build variety** - Heavy tank vs light skirmisher (emergent playstyles)
- **Vision alignment** - Darklands core pillar (equipment defines capabilities, weight = time cost)
- **Unblocks future work** - Proficiency, realistic armor, item comparison UI

**Scope** (4 phases):
```
Phase 1: Character Attributes (strength, dexterity, endurance, intelligence)
Phase 2: Equipment Slots (main hand, off hand, head, torso, legs)
Phase 3: Stat Modifiers (equipment affects attributes + combat stats)
Phase 4: Weight System (heavy equipment increases action time costs)
```

---

### Phase 1: Character Attributes

**Size**: S (~3h)

**Core Attributes** (Darklands + Battle Brothers inspired):
```csharp
public class Attributes
{
    /// <summary>
    /// Physical power - affects melee damage, carrying capacity
    /// Base: 30-50 (peasant-warrior range), Equipment: +0 to +20
    /// </summary>
    public int Strength { get; private set; }

    /// <summary>
    /// Agility and coordination - affects ranged accuracy, dodge chance, initiative
    /// Base: 30-50, Equipment: +0 to +15
    /// </summary>
    public int Dexterity { get; private set; }

    /// <summary>
    /// Physical toughness - affects health, injury resistance, fatigue recovery rate
    /// Base: 30-50, Equipment: +0 to +10
    /// </summary>
    public int Endurance { get; private set; }

    /// <summary>
    /// Mental capacity - affects alchemy, perception, magic (future)
    /// Base: 30-50, Equipment: +0 to +10
    /// </summary>
    public int Intelligence { get; private set; }

    // Derived stats (calculated from base attributes + equipment modifiers)
    public int MaxHealth => Endurance * 2;  // Example: 60 END = 120 HP
    public int MaxFatigue => Endurance * 2;  // Example: 60 END = 120 fatigue points (Battle Brothers pattern)
    public int FatigueRecoveryPerTurn => 10 + (Endurance / 10);  // Example: 60 END = 16 recovery/turn
    public float DodgeChance => Dexterity * 0.01f;  // Example: 50 DEX = 50% dodge
    public int Morale { get; private set; }  // 0-100, affects willingness to fight (separate system)
}
```

**Why These Five Stats?**
- ✅ **Strength, Dexterity, Endurance, Intelligence** - Darklands (1992) core attributes
- ✅ **Fatigue** - Battle Brothers pattern (action economy resource, armor trade-off)
- ✅ **Morale** - Separate from attributes (combat-reactive, not character stat)
- ✅ Simple enough for tactical combat (5 stats total, not D&D 6-stat complexity)
- ✅ Each affects distinct gameplay: STR = melee, DEX = ranged, END = health/fatigue, INT = alchemy, Morale = flee threshold

**Why Fatigue as Derived Stat (NOT separate attribute)?**
- **Battle Brothers pattern**: MaxFatigue = Endurance derivative (not independent stat)
- **Tactical depth**: High Endurance = more actions per turn (tank can swing heavy weapons)
- **Build variety**: Low Endurance + light armor = fewer actions BUT faster (skirmisher)
- **Armor trade-off**: Heavy plate (-35 fatigue) requires high Endurance to remain effective

**Component Integration**:
```csharp
// Add to Actor via component pattern (VS_020 pattern)
public interface IAttributesComponent : IComponent
{
    Attributes BaseAttributes { get; }
    Attributes EffectiveAttributes { get; }  // Base + equipment modifiers
    Result ModifyAttribute(AttributeType type, int modifier);
}

// Actor creation (ActorFactory.cs)
var actor = new Actor(ActorId.NewId(), "ACTOR_PLAYER");
var attributes = new AttributesComponent(
    strength: 40,
    dexterity: 35,
    endurance: 45,
    intelligence: 30
);
actor.AddComponent<IAttributesComponent>(attributes);
```

**Data-Driven Templates** (ActorTemplate.tres) - **CRITICAL: Designer Empowerment**:
```gdscript
# resources/actors/warrior.tres
[resource]
script = ExtResource("ActorTemplate")

Id = "warrior"
NameKey = "ACTOR_WARRIOR"
MaxHealth = 100

# NEW: Stat properties (VS_032 Phase 1) - Designer-configurable!
BaseStrength = 45      # Strong melee fighter
BaseDexterity = 30     # Average agility
BaseEndurance = 40     # Tough, high HP
BaseIntelligence = 25  # Not scholarly

# Derived automatically in code:
# - MaxHealth = Endurance × 2 = 80 HP
# - MaxFatigue = Endurance × 2 = 80 fatigue
# - FatigueRecovery = 10 + (END/10) = 14/turn

# resources/actors/mage.tres (different archetype!)
BaseStrength = 25      # Weak physically
BaseDexterity = 35     # Nimble
BaseEndurance = 30     # Frail
BaseIntelligence = 50  # Highly intelligent
# Result: 60 HP, 60 fatigue (glass cannon!)

# resources/actors/rogue.tres (yet another archetype!)
BaseStrength = 30      # Moderate
BaseDexterity = 50     # Very agile
BaseEndurance = 35     # Moderate
BaseIntelligence = 35  # Clever
# Result: 70 HP, 70 fatigue, high dodge chance
```

**Why Data-Driven Stats Matter** (ADR-006 compliance):
```
DESIGNER WORKFLOW (zero code changes!):
1. Duplicate warrior.tres → Create "berserker.tres"
2. Edit in Godot Inspector:
   - BaseStrength = 50 (+5 more damage!)
   - BaseEndurance = 35 (-5 HP, glass cannon)
   - BaseDexterity = 25 (-5 dodge, slow but deadly)
3. Save → Test immediately (<5 seconds hot-reload!)
4. Iterate balance without programmer

NO CODE CHANGES NEEDED:
✅ Create 10 enemy types (goblin, orc, troll, dragon, etc.)
✅ Create 5 player classes (warrior, mage, rogue, ranger, cleric)
✅ Balance testing (tweak STR/DEX/END/INT in Inspector)
✅ Hot-reload works (instant feedback loop)

DESIGNER EMPOWERMENT: Balance game WITHOUT programmer dependency!
```

**Template Properties** (ActorTemplate.cs Infrastructure extension):
```csharp
// Infrastructure/Templates/ActorTemplate.cs
[GlobalClass]
public partial class ActorTemplate : Resource
{
    // EXISTING (VS_021):
    [Export] public string Id { get; set; } = "";
    [Export] public string NameKey { get; set; } = "";
    [Export] public float MaxHealth { get; set; } = 100f;

    // NEW (VS_032 Phase 1): Base attributes (designer-configurable)
    [Export(PropertyHint.Range, "10,50,1")] public int BaseStrength { get; set; } = 30;
    [Export(PropertyHint.Range, "10,50,1")] public int BaseDexterity { get; set; } = 30;
    [Export(PropertyHint.Range, "10,50,1")] public int BaseEndurance { get; set; } = 30;
    [Export(PropertyHint.Range, "10,50,1")] public int BaseIntelligence { get; set; } = 30;

    // PropertyHint.Range creates sliders in Godot Inspector!
    // Designer sees sliders (10-50 range) instead of raw numbers
}
```

**ActorFactory Integration** (Application layer):
```csharp
// Application/Factories/ActorFactory.cs
public static Result<Actor> CreateFromTemplate(string templateId, ITemplateService templates)
{
    var template = templates.GetTemplate(templateId);

    // Create actor
    var actor = new Actor(ActorId.NewId(), template.NameKey);

    // Add Health component (existing VS_021)
    var health = HealthComponent.Create(template.MaxHealth);
    actor.AddComponent<IHealthComponent>(health.Value);

    // NEW: Add Attributes component (VS_032 Phase 1)
    var attributes = AttributesComponent.Create(
        template.BaseStrength,
        template.BaseDexterity,
        template.BaseEndurance,
        template.BaseIntelligence
    );
    actor.AddComponent<IAttributesComponent>(attributes.Value);

    return Result.Success(actor);
}
```

**Validation Script** (ensure data integrity):
```csharp
// Tools/ValidateActorTemplates.cs (pre-push hook)
public class ActorTemplateValidator
{
    public Result Validate(ActorTemplate template)
    {
        // Check stat ranges (10-50 valid)
        if (template.BaseStrength < 10 || template.BaseStrength > 50)
            return Result.Failure($"{template.Id}: STR out of range (10-50)");

        // Check stat sum (reasonable total)
        int total = template.BaseStrength + template.BaseDexterity +
                    template.BaseEndurance + template.BaseIntelligence;
        if (total < 80 || total > 200)
            return Result.Failure($"{template.Id}: Stat total {total} unrealistic");

        // All checks pass
        return Result.Success();
    }
}
```

**Done When**:
1. Attributes value object created (4 attributes, validation, factory)
2. IAttributesComponent interface + implementation (base + effective calculation)
3. Actor component integration (ActorFactory creates with attributes)
4. **ActorTemplate properties (BaseStrength, BaseDexterity, BaseEndurance, BaseIntelligence) with sliders**
5. **Validation script (ensure stat ranges, prevent broken templates)**
6. **Example templates (warrior.tres, mage.tres, rogue.tres) for testing**
7. Unit tests: Attribute creation, component integration, effective stat calculation (10-12 tests)
8. **Integration test: Load warrior.tres → Actor has STR 45, END 40 (data-driven proof!)**

---

### Phase 2: Equipment Slots

**Size**: M (~4-5h)

**Slot System** (Darklands + Battle Brothers pattern):
```csharp
public enum EquipmentSlot
{
    MainHand,    // Primary weapon (sword, axe, bow)
    OffHand,     // Shield, second weapon, torch
    Head,        // Helmet, hood, hat
    Torso,       // Armor, robe, shirt
    Legs,        // Greaves, pants
    // Future: Ring1, Ring2, Amulet, Cloak (magical items)
}

public class Equipment
{
    private readonly Dictionary<EquipmentSlot, ItemId?> _slots;

    public IReadOnlyDictionary<EquipmentSlot, ItemId?> Slots => _slots;

    // Get item in slot (returns None if empty)
    public Result<ItemId> GetItemInSlot(EquipmentSlot slot);

    // Equip item (validates slot compatibility)
    public Result EquipItem(ItemId itemId, Item item, EquipmentSlot slot);

    // Unequip item (returns to inventory)
    public Result<ItemId> UnequipItem(EquipmentSlot slot);

    // Check if slot occupied
    public bool IsSlotOccupied(EquipmentSlot slot);
}
```

**Item Slot Compatibility** (Item.cs extension):
```csharp
// Add to Item domain entity
public enum ItemSlotType
{
    OneHandedWeapon,   // Can go in MainHand OR OffHand
    TwoHandedWeapon,   // Requires MainHand + OffHand (both slots!)
    Shield,            // OffHand only
    Helmet,            // Head only
    Armor,             // Torso only
    Pants,             // Legs only
    // Future: Ring, Amulet, etc.
}

// Item.cs
public ItemSlotType SlotType { get; private init; }
public bool IsTwoHanded => SlotType == ItemSlotType.TwoHandedWeapon;
```

**Component Integration**:
```csharp
public interface IEquipmentComponent : IComponent
{
    Equipment Equipment { get; }
    Result EquipItem(ItemId itemId, Item item);
    Result<ItemId> UnequipItem(EquipmentSlot slot);
    Result<Item> GetEquippedItem(EquipmentSlot slot, IItemRepository items);
}
```

**Commands** (MediatR pattern):
```csharp
// EquipItemCommand.cs
public record EquipItemCommand(
    ActorId ActorId,
    ItemId ItemId,
    EquipmentSlot TargetSlot
) : IRequest<Result>;

// EquipItemCommandHandler.cs
public async Task<Result> Handle(EquipItemCommand cmd)
{
    // 1. Get actor + equipment component
    var actor = await _actors.GetActorAsync(cmd.ActorId);
    var equipment = actor.GetComponent<IEquipmentComponent>();

    // 2. Get item from inventory
    var inventory = actor.GetComponent<IInventoryComponent>();
    var item = await _items.GetItemByIdAsync(cmd.ItemId);

    // 3. Validate: Item in inventory?
    if (!inventory.Inventory.Contains(cmd.ItemId))
        return Result.Failure("Item not in inventory");

    // 4. Validate: Slot compatible with item type?
    if (!IsSlotCompatible(cmd.TargetSlot, item.SlotType))
        return Result.Failure($"Cannot equip {item.SlotType} in {cmd.TargetSlot}");

    // 5. Two-handed weapon? Unequip offhand
    if (item.IsTwoHanded && equipment.Equipment.IsSlotOccupied(EquipmentSlot.OffHand))
    {
        var unequipResult = equipment.UnequipItem(EquipmentSlot.OffHand);
        if (unequipResult.IsFailure) return unequipResult;
    }

    // 6. Remove from inventory, equip to slot
    inventory.Inventory.RemoveItem(cmd.ItemId);
    return equipment.EquipItem(cmd.ItemId, item);
}
```

**Done When**:
1. Equipment domain class (slots, equip/unequip, validation)
2. Item.SlotType property (one-handed, two-handed, armor types)
3. IEquipmentComponent interface + implementation
4. EquipItemCommand + UnequipItemCommand (MediatR handlers)
5. Two-handed weapon validation (occupies both MainHand + OffHand)
6. Unit tests: Slot compatibility, two-handed weapons, equip/unequip flow (15-18 tests)
7. Integration test: Equip sword → unequip → equip two-handed axe (offhand cleared)

---

### Phase 3: Stat Modifiers from Equipment

**Size**: M (~3-4h)

**Equipment Stat Bonuses** (Item.cs extension):
```csharp
// Add to Item domain entity
public class StatModifiers
{
    // Attribute bonuses
    public int StrengthBonus { get; init; }
    public int DexterityBonus { get; init; }
    public int EnduranceBonus { get; init; }
    public int IntelligenceBonus { get; init; }

    // Combat bonuses
    public int DamageBonus { get; init; }        // +5 damage (magic sword)
    public int DefenseBonus { get; init; }       // +10 defense (plate armor)
    public float CritChanceBonus { get; init; }  // +15% crit (rogue dagger)

    // Weight (affects action time costs - Phase 4)
    public float Weight { get; init; }  // kg (plate = 25kg, leather = 8kg)
}

// Item.cs
public StatModifiers Modifiers { get; private init; }
```

**Effective Attributes Calculation** (AttributesComponent.cs):
```csharp
public Attributes EffectiveAttributes
{
    get
    {
        var effective = new Attributes(
            BaseAttributes.Strength,
            BaseAttributes.Dexterity,
            BaseAttributes.Endurance,
            BaseAttributes.Intelligence
        );

        // Apply equipment bonuses
        var equipment = _actor.GetComponent<IEquipmentComponent>();
        foreach (var slot in equipment.Equipment.Slots.Values)
        {
            if (slot.HasValue)
            {
                var item = _items.GetItemByIdAsync(slot.Value).Result;
                effective = effective.Apply(item.Modifiers);
            }
        }

        return effective;
    }
}
```

**Combat Integration** (ExecuteAttackCommandHandler.cs):
```csharp
// OLD: Fixed damage (VS_020)
float baseDamage = 10f;

// NEW: Equipment-based damage (VS_032 Phase 3)
var weapon = attacker.GetComponent<IEquipmentComponent>()
    .GetEquippedItem(EquipmentSlot.MainHand, _items).Value;

float baseDamage = weapon.Modifiers.DamageBonus;  // Sword = 8, Dagger = 5, Axe = 12

// Apply strength bonus
var attributes = attacker.GetComponent<IAttributesComponent>();
float strengthMultiplier = 1f + (attributes.EffectiveAttributes.Strength / 100f);
float finalDamage = baseDamage * strengthMultiplier;  // 40 STR = 1.4× damage
```

**Data-Driven Item Modifiers** (TileSet metadata):
```gdscript
# Kenney tileset custom_data layers (existing system from VS_021)
custom_data_0 = "iron_sword"      # item_name
custom_data_1 = "weapon"          # item_type
custom_data_2 = 1                 # max_stack_size (weapons not stackable)

# NEW custom_data layers for stats
custom_data_3 = 8                 # damage_bonus
custom_data_4 = 0                 # defense_bonus
custom_data_5 = 2.5               # weight_kg
custom_data_6 = 0                 # strength_bonus
custom_data_7 = 0                 # dexterity_bonus
```

**Done When**:
1. StatModifiers value object (attribute bonuses, combat bonuses, weight)
2. Item.Modifiers property (read from TileSet custom_data)
3. EffectiveAttributes calculation (base + equipment sum)
4. Combat integration (weapon damage, strength multiplier, armor defense)
5. TileSet custom_data schema (7 layers: name, type, stack, damage, defense, weight, attributes)
6. Unit tests: Stat modifier application, equipment bonuses stack, combat uses effective stats (12-15 tests)
7. Integration test: Equip plate armor → defense increases → takes less damage

---

### Phase 4: Weight System & Action Time Costs

**Size**: S (~2-3h)

**Weight Formula** (time-unit cost increase):
```csharp
public class WeightSystem
{
    // Calculate total equipped weight
    public float CalculateTotalWeight(Actor actor, IItemRepository items)
    {
        float totalWeight = 0f;
        var equipment = actor.GetComponent<IEquipmentComponent>();

        foreach (var slot in equipment.Equipment.Slots.Values)
        {
            if (slot.HasValue)
            {
                var item = items.GetItemByIdAsync(slot.Value).Result;
                totalWeight += item.Modifiers.Weight;
            }
        }

        return totalWeight;
    }

    // Calculate action time multiplier (Darklands formula)
    public float CalculateActionTimeMultiplier(float totalWeight, int strength)
    {
        // Carrying capacity = Strength × 3 (e.g., 40 STR = 120 kg capacity)
        float capacity = strength * 3f;
        float encumbrance = totalWeight / capacity;

        // Light load (0-50%): No penalty
        if (encumbrance <= 0.5f)
            return 1.0f;

        // Medium load (50-80%): +20% time
        if (encumbrance <= 0.8f)
            return 1.2f;

        // Heavy load (80-100%): +50% time
        if (encumbrance <= 1.0f)
            return 1.5f;

        // Overloaded (>100%): +100% time (penalty!)
        return 2.0f;
    }
}
```

**Combat Integration** (VS_007 turn queue):
```csharp
// ExecuteAttackCommandHandler.cs (VS_020)
int baseActionTime = 100;  // Base attack = 100 time units

// NEW: Apply weight multiplier (VS_032 Phase 4)
var attributes = attacker.GetComponent<IAttributesComponent>();
float totalWeight = _weightSystem.CalculateTotalWeight(attacker, _items);
float weightMultiplier = _weightSystem.CalculateActionTimeMultiplier(
    totalWeight,
    attributes.EffectiveAttributes.Strength
);

int finalActionTime = (int)(baseActionTime * weightMultiplier);
// Light armor (10kg, 40 STR): 100 time (no penalty)
// Heavy armor (30kg, 40 STR): 120 time (+20% slower)
// Overloaded (140kg, 40 STR): 200 time (+100% slower - PENALTY!)
```

**Gameplay Impact**:
```
LIGHT SKIRMISHER BUILD:
- Leather armor (8kg) + dagger (1kg) = 9kg total
- 40 STR (120kg capacity) → 7.5% encumbrance
- Weight multiplier: 1.0× (no penalty!)
- Attack time: 100 units
- Defense: LOW (leather = +5 defense)
- Strategy: Fast attacks, dodge-focused

HEAVY TANK BUILD:
- Plate armor (25kg) + shield (5kg) + longsword (3kg) = 33kg total
- 40 STR (120kg capacity) → 27.5% encumbrance
- Weight multiplier: 1.0× (still light load!)
- Attack time: 100 units (only slightly slower if STR is low)
- Defense: HIGH (plate = +20 defense)
- Strategy: Tank hits, protect allies

OVERLOADED PENALTY:
- Full plate (25kg) + tower shield (8kg) + two-handed sword (5kg) = 38kg
- 30 STR (90kg capacity) → 42% encumbrance
- Weight multiplier: 1.0× (but approaching medium load!)
- Low strength = HIGH penalty (encourages STR investment for heavy builds)
```

**Done When**:
1. WeightSystem service (calculate total weight, action time multiplier)
2. Carrying capacity formula (Strength × 3 kg)
3. Encumbrance tiers (light/medium/heavy/overloaded)
4. Combat integration (attack time × weight multiplier)
5. Unit tests: Weight calculation, encumbrance tiers, action time penalties (8-10 tests)
6. Integration test: Equip heavy armor → strength affects action speed

---

### Integration Points

**Combat System** (VS_020):
- Weapon damage from equipment (MainHand item)
- Armor defense from equipment (Torso + Head + Legs items)
- Strength affects damage multiplier
- Dexterity affects dodge chance (future: VS_011 Enemy AI)

**Inventory System** (VS_018):
- Equip command moves item from inventory → equipment slot
- Unequip command moves item from slot → inventory
- Two-handed weapon logic (occupies 2 slots)

**Turn Queue** (VS_007):
- Action time costs affected by weight (heavy armor = slower actions)
- Proficiency reduces action time (future: weapon skill progression)

**Future: Realistic Armor Layers** (Deferred):
- Multiple armor layers (padding + mail + plate)
- Damage type interactions (blunt through mail, piercing penetrates, slashing vs plate)
- Individual layer durability (gear degradation)

---

## Fatigue System (PLANNED)

**Status**: Proposed (VS_032 Phase 5 OR separate VS after Phase 4) | **Size**: M (6-8h) | **Priority**: Important (build variety depth)

**What**: Battle Brothers-inspired fatigue system - every action costs fatigue, armor has fatigue penalty, creates action economy trade-offs.

**Why**:
- **Build variety** - Heavy tank (few powerful actions) vs Light skirmisher (many fast actions)
- **Armor trade-off** - Protection vs Action Economy (exactly what we need for tactical depth!)
- **Universal resource** - Single mechanic affects all actions (attacks, movement, skills, dodge)
- **Complements time-unit system** - Fatigue = action cost, time units = turn order (orthogonal resources!)

**Battle Brothers Pattern**:
```
MaxFatigue = Endurance × 2 (40 END = 80 fatigue points)
Recovery = 10 + (Endurance / 10) per turn (40 END = 14 recovery/turn)

ACTION COSTS:
- Move (1 tile): 4 fatigue
- Light attack (dagger): 15 fatigue
- Heavy attack (axe): 25 fatigue
- Skill (whirlwind): 35 fatigue
- Rest action: Recover 50% max fatigue immediately

ARMOR PENALTIES:
- Naked: -0 fatigue max
- Leather armor: -10 fatigue max
- Chainmail: -20 fatigue max
- Plate armor: -35 fatigue max
```

**Emergent Builds**:
```
HEAVY TANK (40 END, Plate -35):
- MaxFatigue: 80 - 35 = 45 points
- Can attack: 45 / 25 = 1-2 heavy attacks before exhausted
- Recovery: 14 per turn (need 2 turns to recover full)
- Strategy: High-impact actions, rest frequently

LIGHT SKIRMISHER (40 END, Leather -10):
- MaxFatigue: 80 - 10 = 70 points
- Can attack: 70 / 15 = 4-5 light attacks before exhausted
- Recovery: 14 per turn (recover faster)
- Strategy: Many fast attacks, kite enemies, dodge
```

**Integration with Time-Unit System**:
```
ORTHOGONAL RESOURCES (both matter!):
- Time Units: WHO acts next (turn order)
- Fatigue: HOW MANY actions you can take

EXAMPLE TURN:
1. Player attacks (costs: 100 time units, 20 fatigue)
   - Time: Player@100 (acts later next turn)
   - Fatigue: 60/80 remaining (can attack 3 more times)
2. Player attacks again (costs: 100 time units, 20 fatigue)
   - Time: Player@200 (much later in queue)
   - Fatigue: 40/80 remaining (can attack 2 more times)
3. Player exhausted (0 fatigue) → MUST rest or penalties
```

**Low Fatigue Penalties** (Battle Brothers pattern):
```
< 25% Fatigue (Exhausted):
- Attack time cost: +50% (100 units → 150 units SLOW!)
- Movement speed: -50% (move costs double fatigue)
- Dodge chance: -25% (50% dodge → 25% dodge)

< 10% Fatigue (Critically Exhausted):
- Attack time cost: +100% (100 units → 200 units VERY SLOW!)
- Cannot use skills (35 fatigue cost = unavailable)
- Forced to rest or collapse
```

**Scope** (high-level):
- FatigueComponent (current/max fatigue, recovery rate)
- Action costs configuration (attack/move/skill costs)
- Armor fatigue penalties (equipment stat modifier)
- Low fatigue penalty system (time cost multiplier)
- Rest action (recover 50% max fatigue, costs time units)
- UI: Fatigue bar (green → yellow → red visual feedback)

**Dependencies**:
- **Prerequisite**: VS_032 Phases 1-4 ✅ (Attributes, Equipment, Stat Modifiers, Weight System)
- **Integration**: Turn Queue (fatigue affects action time costs)
- **Integration**: Equipment (armor has fatigue penalty property)

**Blocks**: Nothing (combat works without fatigue, just no action economy depth)

**Product Owner Note**: Consider making this VS_032 Phase 5 (same feature) OR separate VS after Phase 4 validated. High priority - directly enhances build variety.

---

## Healing & Regeneration System (PLANNED)

**Status**: Proposed (Integrated with Fatigue System) | **Size**: S (3-4h) | **Priority**: Important (resource management depth)

**What**: Battle Brothers-inspired regeneration model - Fatigue regenerates in combat (tactical), HP does NOT regenerate in combat (attrition), with slow HP regen during exploration (quality of life).

**Why**:
- **Orthogonal resources** - Fatigue (short-term action economy) vs HP (long-term attrition)
- **Attrition matters** - Injuries accumulate across fights (retreat decisions, resource management)
- **Resource economy** - Potions/bandages/healing valuable (not obsolete)
- **Darklands philosophy** - Harsh, realistic, meaningful consequences

**Design Pattern** (Battle Brothers validated):
```
FATIGUE (short-term resource):
- Purpose: Action economy management
- Regenerates: 10-16 per turn IN COMBAT
- Decision: Attack now OR rest for sustained combat?
- Creates: Tactical rhythm (burst → rest → burst)

HP (long-term resource):
- Purpose: Attrition and expedition pressure
- Regenerates: ONLY via healing methods (NOT in combat!)
- Decision: Continue exploring OR retreat to heal?
- Creates: Resource management (when to use potions?)
```

---

### In-Combat Regeneration (Tactical Layer)

**Fatigue Recovery**:
```
PASSIVE RECOVERY (every turn):
- Formula: 10 + (Endurance / 10) per turn
- Example: 40 END = 14 fatigue/turn, 60 END = 16 fatigue/turn
- Automatic: No action required

REST ACTION (active recovery):
- Effect: Recover 50% MaxFatigue immediately
- Cost: Uses full turn (time units spent)
- Trade-off: Skip attack to sustain combat longer
```

**NO HP Recovery in Combat**:
```
WHY NO REGEN:
✅ Injuries matter (damage accumulates)
✅ Retreat decisions (withdraw before death)
✅ Healing resources valuable (potions, bandages)
✅ Dungeon pressure (can't endlessly fight)

HEALING SOURCES IN COMBAT:
- Health Potion: Instant +50 HP (costs item slot + time units to drink)
- Stamina Potion: Instant +40 fatigue (rare, expensive)
- Bandage: Stop bleeding status (not full heal)
```

---

### Out-of-Combat Regeneration (Strategic Layer)

**Fatigue Recovery** (between fights):
```
INSTANT FULL RECOVERY:
- Fatigue: 100% recovered between fights (automatic)
- Purpose: Each fight starts fresh (no fatigue debt)
- Why: Fatigue is tactical resource, not attrition resource
```

**HP Slow Regeneration** (exploration):
```
NATURAL HEALING (quality of life):
- Rate: 1 HP per 5 minutes of walking
- Example: 80/100 HP → walk 10 minutes → 82/100 HP (minor)
- Purpose: Small recovery during safe exploration
- NOT MAIN HEALING: Still need rest/potions after tough fights

WHY SLOW REGEN:
✅ Quality of life (don't force rest after every tiny injury)
✅ Still requires healing (20 HP injury = 100 minutes walking!)
✅ Exploration reward (safe areas provide minor recovery)
❌ Doesn't trivialize attrition (too slow to matter mid-dungeon)
```

---

### Healing Methods (Strategic Resource Management)

**Campfire Rest** (primary healing):
```
MECHANICS:
- Duration: 8 hours (time cost!)
- HP Recovery: 50% MaxHealth
- Fatigue Recovery: 100% (already covered)
- Resource Cost: 1 food, 1 water
- Risk: 10% random encounter chance (NOT perfectly safe!)

STRATEGIC TRADE-OFFS:
- When to rest? Low HP + safe area + supplies
- Risk: Time pressure (quests expire, food consumed)
- Risk: Encounter interrupts rest (lose healing, enter combat)
```

**Town Healer** (expensive but reliable):
```
MECHANICS:
- Cost: 10 gold per 20 HP healed
- Recovery: Can heal to 100% HP (unlimited if you pay)
- Availability: Towns only (must travel)
- Instant: No time cost (unlike camping)

STRATEGIC TRADE-OFFS:
- When to use? Low HP + near town + have gold
- Trade-off: Spend gold OR risk death?
- Economy: Gold = healing (creates scarcity)
```

**Consumables** (tactical healing):
```
HEALTH POTION:
- Effect: Instant +50 HP
- Cost: 3 gold to buy, limited quantity
- Usage: In-combat OR exploration
- Trade-off: Use now OR save for emergency?

BANDAGE:
- Effect: +15 HP over 3 turns (delayed healing)
- Effect: Stop bleeding status
- Crafting: Requires cloth material
- Usage: After fight, not instant heal

HERB POULTICE:
- Effect: +25 HP over 5 turns + stop bleeding
- Crafting: Requires herbs (foraged or bought)
- Usage: Better than bandage, slower than potion
```

---

### Attrition Examples

**Scenario 1: Dungeon Crawl Pressure**:
```
Fight 1: Player 100/100 HP → takes 30 damage → 70/100 HP
Fight 2: Player 70/100 HP → takes 25 damage → 45/100 HP
Fight 3: Player 45/100 HP → takes 20 damage → 25/100 HP

DECISION POINT:
- Continue? Risk death (one more fight = lethal)
- Retreat? Lose dungeon progress, must rest/heal
- Use potion? Expensive, limited quantity (save for boss?)

ATTRITION WORKING: Each fight chips away, forces retreat decision
```

**Scenario 2: Exploration Recovery**:
```
After Fight: Player 80/100 HP (minor damage)
Walk to town: 10 minutes = +2 HP (slow regen)
Arrival: Player 82/100 HP

RESULT: Minor recovery, but NOT full heal (still need rest/healer)
QUALITY OF LIFE: Don't force rest after every scratch
```

**Scenario 3: Resource Management**:
```
Player inventory:
- 2× Health Potion (50 HP each)
- 3× Bandage (15 HP delayed)
- 50 gold (can buy 100 HP healing at town)
- 2× Food (for camping)

Current HP: 40/100 (critical!)

OPTIONS:
1. Use potion NOW (+50 HP → 90/100, but 1 potion left)
2. Retreat to town (spend 50 gold, keep potions for boss)
3. Camp here (risky: 10% encounter, use 1 food)
4. Continue exploring (death spiral risk!)

MEANINGFUL CHOICE: No "correct" answer, trade-offs matter
```

---

### Integration with Fatigue System

**Orthogonal Resources** (both matter, different purposes):
```
FATIGUE (Tactical - Action Economy):
- IN combat: Regenerates per turn (10-16/turn)
- OUT combat: Full recovery (instant)
- Purpose: How many actions before rest?
- Builds: Heavy tank (few actions) vs Light skirmisher (many actions)

HP (Strategic - Attrition):
- IN combat: NO regeneration (injuries persist!)
- OUT combat: Slow regen (1 HP / 5 min) + healing methods
- Purpose: When to retreat? When to use resources?
- Pressure: Each fight chips away, dungeon crawl tension
```

**Both Resources Create Depth**:
```
EXAMPLE TURN (both matter):
Player State: 60/80 Fatigue, 50/100 HP

DECISION 1: Attack OR Rest?
- Attack: Costs 20 fatigue (40/80 remaining), deal damage
- Rest: Recover fatigue (+40 fatigue), but enemy acts

DECISION 2: Use Health Potion?
- Yes: +50 HP (100/100), but lose potion (limited!)
- No: Stay 50 HP, risk death, save for emergency

TACTICAL + STRATEGIC: Both layers matter simultaneously
```

---

### Scope (High-Level)

**Fatigue Regeneration** (Phase 5 of VS_032 OR separate VS):
- Passive recovery (10 + END/10 per turn)
- Rest action (recover 50% max fatigue)
- UI: Fatigue bar auto-updates

**HP Healing Methods** (separate VS after Fatigue):
- Slow exploration regen (1 HP / 5 min timer)
- Campfire rest (50% HP recovery, 8h, food cost, 10% encounter risk)
- Town healer (10 gold / 20 HP, instant, unlimited)
- Consumables (potions, bandages, herbs)
- UI: HP bar, healing indicators, consumable inventory

**Dependencies**:
- **Prerequisite**: VS_032 Phases 1-4 ✅ (Attributes, Equipment)
- **Prerequisite**: Fatigue System ✅ (Phase 5 of VS_032)
- **Prerequisite**: Consumable items (potions, food, bandages)
- **Future**: Status effects (bleeding = requires bandage to stop)

**Blocks**: Nothing (combat works without healing, just no attrition depth)

**Product Owner Note**: Implement with Fatigue System (VS_032 Phase 5) - they're closely related. Healing methods can be separate VS after equipment validated.

---

## Stat Training System (PLANNED)

**Status**: Proposed (After Proficiency Validated) | **Size**: M (6-8h) | **Priority**: Important (progression depth, deferred)

**What**: Darklands (1992) inspired use-based stat training - attributes increase slowly through gameplay actions (+20% cap), complements proficiency and equipment progression.

**Why**:
- **Authentic Darklands** - Original game had stat growth via use ("running around Germany")
- **Reward for play** - Physical improvement through action (attack = +STR, dodge = +DEX)
- **Modest growth** - +20% cap prevents power creep (40 STR → max 48)
- **Complements systems** - Three progression layers (equipment > proficiency > stats)
- **Horizontal focus** - Stats are tertiary (equipment + proficiency still primary)

**Research Validation**:
```
DARKLANDS (1992):
"Progression is the result of your actions, successes and failures,
increasing and decreasing your attributes in small increments."

DWARF FORTRESS:
- 500 actions = +1 stat
- Cap: 2× starting value (too much for us!)

OUR MODEL: Modest Darklands pattern (not DF power creep)
```

---

### Training Mechanics

**Stat Increase Formula** (Darklands-inspired):
```
PROGRESS COUNTER per stat:
- Action increments counter (attack = +1 STR progress)
- Threshold reached = +1 stat point
- Threshold scales (harder to train higher stats)

BASE THRESHOLD: 50 actions = +1 stat (moderate rate)
SCALING: Threshold × (current stat / base stat)

EXAMPLE (STR 40 base):
- STR 40 → 41: Requires 50 actions
- STR 42 → 43: Requires 53 actions (+5% harder)
- STR 46 → 47: Requires 58 actions (+15% harder)
- STR 47 → 48: Requires 60 actions (+20% harder, at cap)

GROWTH CAP: Base stat + 20% (40 STR → max 48 STR)
```

**Training Actions** (by stat):
```
STRENGTH (physical power):
+ Melee attack landed: +1 progress
+ Carry heavy equipment: +1 per 10 minutes
+ Force door/chest open: +5 progress
+ Climb obstacle: +3 progress

DEXTERITY (agility and coordination):
+ Ranged attack landed: +1 progress
+ Dodge successful: +2 progress (rewarding!)
+ Lockpicking attempt: +1 progress
+ Sneak past enemy: +3 progress

ENDURANCE (physical toughness):
+ Damage taken: +1 per 10 damage
+ Sprint/run: +1 per minute
+ Heavy armor worn: +1 per 10 minutes
+ Fatigue exhaustion: +2 per exhaustion cycle

INTELLIGENCE (mental capacity):
+ Alchemy crafted: +5 progress
+ Trap detected: +3 progress
+ Book read: +10 progress
+ Puzzle solved: +5 progress
```

---

### Growth Rate Examples

**2-Hour Run** (typical session):
```
GAMEPLAY:
- 50 melee attacks (50 STR progress)
- 30 dodge attempts (60 DEX progress)
- 200 damage taken (20 END progress)
- 5 potions crafted (25 INT progress)

STAT GAINS:
- STR 40 → 41 (+50 progress = threshold!)
- DEX 35 → 36 (+60 progress = threshold!)
- END 50 → 50 (only 20/50 progress, not enough)
- INT 30 → 30 (only 25/50 progress, not enough)

RESULT: +2 stat points over 2 hours = modest, noticeable
```

**4-Hour Run** (full session):
```
GAMEPLAY:
- 100 melee attacks
- 60 dodge attempts
- 400 damage taken
- 10 potions crafted

STAT GAINS:
- STR 40 → 42 (+5% melee damage)
- DEX 35 → 37 (+6% dodge chance)
- END 50 → 52 (+4 HP)
- INT 30 → 32 (+6% alchemy effectiveness)

RESULT: +6 stat points = meaningful progression (but not broken)
```

**10-Hour Playthrough** (complete run to retirement):
```
STAT PROGRESSION:
- STR 40 → 46 (+15% melee damage, near cap at 48)
- DEX 35 → 41 (+17% dodge chance, near cap at 42)
- END 50 → 58 (+16 HP, near cap at 60)
- INT 30 → 36 (+20% alchemy, at cap!)

RESULT: +18 stat points total = significant but capped
PREVENTS POWER CREEP: Can't exceed 20% growth (caps enforced)
```

---

### Emergent Build Specialization

**Heavy Tank Build** (wears plate, takes damage, melee focus):
```
TRAINING PATTERN:
- Wears plate armor (+END training per 10 min)
- Takes lots of damage (+END training per 10 damage)
- Swings heavy weapons (+STR training per attack)
- Rarely dodges (low DEX training)

STAT GROWTH:
- END 50 → 60 (+20 HP, tankier!)
- STR 40 → 48 (+20% melee damage, harder hitting!)
- DEX 35 → 36 (minimal growth, not dodge-focused)
- INT 30 → 31 (minimal growth, not alchemy-focused)

RESULT: Play style reinforces archetype naturally
```

**Light Skirmisher Build** (dodges, ranged combat, alchemy):
```
TRAINING PATTERN:
- Dodges frequently (+DEX training per dodge)
- Uses bow (+DEX training per ranged attack)
- Crafts potions (+INT training per craft)
- Light armor (less END training)

STAT GROWTH:
- DEX 35 → 42 (+20% dodge, more evasive!)
- INT 30 → 36 (+20% alchemy, better potions!)
- STR 30 → 32 (some growth from occasional melee)
- END 40 → 44 (some growth from minor damage)

RESULT: Specialization emerges from gameplay, not allocation
```

---

### Integration with Aging System

**Physical Stats Degrade with Age** (Darklands pattern):
```
AGE DEGRADATION:
- 15-29: No degradation (prime physical condition)
- 30-39: -1 STR/END/DEX per 5 years (slow decline)
- 40-49: -2 STR/END/DEX per 5 years (noticeable decline)
- 50+: -3 STR/END/DEX per 5 years (rapid decline)

MENTAL STATS:
- INT: No degradation until 60+ (wisdom endures)

TRAINING OFFSET:
- Can train stats to offset aging temporarily
- Example: STR 40 → train to 48, then age 35 → STR drops to 46
- Result: Training buys time (stay competitive 5-10 years longer)
- Eventually: Aging wins (forced retirement at 60-70)

STRATEGIC DECISION:
- Young character (20): Low base stats but can grow + no degradation
- Old character (40): High base stats but degradation starts immediately
- Trade-off: Growth potential vs starting power
```

---

### Why +20% Cap is Right

**Power Creep Prevention**:
```
20% GROWTH:
- 40 STR → 48 STR (+20% melee damage)
- 35 DEX → 42 DEX (+20% dodge chance)
- 50 END → 60 END (+20 HP)

NOT GAME-BREAKING:
✅ Noticeable improvement (player feels progression)
✅ Not doubling (40 STR ≠ 80 STR, no power creep)
✅ Equipment still matters (plate armor > 8 STR points)
✅ Proficiency still primary (skill 100 = -30% time cost)

COMPARE TO DF (2× growth):
❌ 40 STR → 80 STR = 100% damage increase (broken!)
❌ Equipment becomes irrelevant (stats dominate)
❌ Vertical progression (violates Darklands philosophy)
```

**Horizontal Progression Maintained**:
```
PROGRESSION HIERARCHY:
1. Equipment: Immediate power (equip plate = +20 defense NOW)
2. Proficiency: Skill mastery (sword 100 = -30% attack time)
3. Stats: Physical growth (STR 40 → 48 = +20% damage SLOWLY)

EQUIPMENT STILL KING:
- Steel sword (+10 damage) > 8 STR points (+20% of 10 = +2 damage)
- Plate armor (+20 defense) > 10 END points (+20 HP)

STATS COMPLEMENT, NOT REPLACE: Tertiary progression layer
```

---

### Scope (High-Level)

**Training Tracking** (separate VS after Proficiency):
- Progress counters (per stat, per actor)
- Action detection (attack landed, dodge successful, damage taken)
- Threshold calculation (scaling based on current stat)
- Growth cap enforcement (base + 20% max)
- UI: Stat progress bars (optional, minimal feedback)

**Action Triggers**:
- Combat integration (attacks, dodges, damage → training)
- Exploration integration (carry weight, sprinting → training)
- Crafting integration (alchemy, lockpicking → training)
- Passive training (armor worn → END training over time)

**Dependencies**:
- **Prerequisite**: VS_032 ✅ (Attributes, Equipment)
- **Prerequisite**: Proficiency System ✅ (validate skill progression first)
- **Recommended**: Aging System (stat degradation offsets growth)
- **Future**: Character creation aging (starting stats trade-off)

**Blocks**: Nothing (combat/proficiency work without stat training)

**Product Owner Note**: **DEFER until after Proficiency validated** - Let players experience fixed stats + proficiency first. If progression feels shallow, add stat training. If deep enough, skip it! Playtest-driven decision.

---

### Why Defer This Feature

**Validate Core Systems First**:
```
PHASE 1: VS_032 Equipment + Stats (fixed stats)
- Prove: Equipment defines power (gear variety matters)
- Test: 4 attributes sufficient? Or need more depth?

PHASE 2: Proficiency System (skill progression)
- Prove: Use-based skill growth rewarding (attack faster over time)
- Test: Proficiency alone sufficient? Or need stat growth too?

PHASE 3: Stat Training (IF NEEDED)
- Prove: Adding stat growth enhances depth (not redundant)
- Test: Does 20% cap feel rewarding? Or too slow?

INCREMENTAL VALIDATION: Build → Test → Decide (not all-at-once)
```

**Risk Mitigation**:
```
IF WE BUILD TOO EARLY:
❌ Stat training redundant (proficiency already satisfying)
❌ Wasted dev time (6-8h on unused system)
❌ Complexity bloat (three progression layers overwhelming)

IF WE WAIT:
✅ Playtest reveals need (players ask "why don't stats grow?")
✅ Informed tuning (know if 20% cap right or adjust)
✅ Optional depth (add if core feels shallow, skip if deep)

PRODUCT OWNER DISCIPLINE: Build minimum, validate, expand (not guess)
```

---

## Morale System (PLANNED)

**Status**: Proposed (After Combat AI + Multiple Encounters) | **Size**: S (3-4h) | **Priority**: Ideas (deferred until mid/late game)

**What**: Combat morale system - characters can flee/surrender when morale breaks, affected by damage taken, allies dying, enemy strength.

**Why**:
- **Emergent behavior** - Enemies flee when losing (realistic outcomes)
- **Player risk management** - Low health = morale risk (retreat before breaking)
- **Boss fights** - High morale enemies fight to death, bandits flee
- **Intelligence integration** - High INT = better morale (mental toughness)

**Morale Mechanics**:
```
BASE MORALE: 50 (neutral)
MAX MORALE: 100 (fearless, never flees)
MIN MORALE: 0 (broken, flees immediately)

MORALE MODIFIERS:
+ Allies nearby (+5 per ally within 3 tiles)
+ Winning battle (+10 if enemies < 50% HP)
- Ally died (-15 per death witnessed)
- Low health (-20 if < 25% HP)
- Outnumbered (-10 if enemies 2×+ your side)
+ High Intelligence (+INT/2 bonus, e.g., 60 INT = +30 morale)

MORALE BREAK (< 25 morale):
- 50% chance to flee per turn (run toward map edge)
- Cannot attack (only move action available)
- Drops equipped items (loot opportunity!)
```

**Example Scenario**:
```
GOBLIN AMBUSH (3 goblins vs player):
1. Combat starts: Goblin morale = 50 (neutral)
2. Player kills Goblin A: Goblin B/C morale = 35 (-15 ally died)
3. Goblin B takes damage (15/30 HP = 50%): Morale = 25 (breaking point!)
4. Goblin B flees (50% chance rolled): Runs toward map edge, drops weapon
5. Goblin C sees B flee: Morale = 20 (-5 ally fled) → Also flees!
6. Player victory without killing all enemies (emergent outcome!)
```

**Scope** (high-level):
- MoraleComponent (current morale, modifiers, break threshold)
- Morale calculation (allies nearby, health, ally deaths, intelligence)
- Flee behavior (AI decision tree: flee toward map edge)
- Drop items on flee (equipped weapons/armor to ground)
- Morale UI (optional: morale bar for player, enemy morale hints)

**Dependencies**:
- **Prerequisite**: VS_032 ✅ (Intelligence affects morale bonus)
- **Prerequisite**: Enemy AI ✅ (flee decision tree)
- **Prerequisite**: Ground Loot ✅ (drop items when fleeing)
- **Recommended**: Multiple enemy encounters (morale most impactful with 3+ enemies)

**Blocks**: Nothing (combat works without morale, just less dynamic)

**Product Owner Note**: Defer until mid-game (after equipment, AI, proficiency validated). Nice-to-have for emergent behavior, not critical for core combat loop.

---

## Renown & Experience Display (PLANNED)

**Status**: Proposed (After Proficiency + Multiple Encounters) | **Size**: XS (1-2h) | **Priority**: Ideas (pure UI polish)

**What**: Informational display of character "veterancy" WITHOUT character levels - shows aggregate experience via proficiency totals, reputation, and equipment tier.

**Why**:
- **NO LEVELS** - Respects Darklands philosophy (no global power scaling)
- **Player feedback** - Shows progression (novice → veteran → master)
- **Informational only** - No combat modifiers, just visual feedback

**Renown Display**:
```
CHARACTER SHEET (informational only):

╔══════════════════════════════════════════╗
║ RENOWN: Veteran Sellsword               ║
║ (Informational - no combat modifiers)   ║
╠══════════════════════════════════════════╣
║ Total Proficiency: 245 / 500            ║
║   Swords: 65 (Expert)                   ║
║   Daggers: 80 (Master)                  ║
║   Axes: 50 (Competent)                  ║
║   Bows: 50 (Competent)                  ║
║                                          ║
║ Reputation: 42 (Known)                  ║
║   Quests Completed: 12                  ║
║   Bosses Defeated: 3                    ║
║   Contracts Fulfilled: 8                ║
║                                          ║
║ Equipment Tier: Veteran Gear            ║
║   Weapon: Steel Longsword (Tier 2)      ║
║   Armor: Reinforced Chainmail (Tier 2)  ║
╚══════════════════════════════════════════╝
```

**Renown Titles** (aggregate proficiency):
```
0-50:    Novice (green recruit)
51-150:  Competent (experienced fighter)
151-250: Veteran (seasoned warrior)
251-350: Expert (master combatant)
351-500: Legendary (living legend)
```

**Reputation System** (separate from combat power):
```
REPUTATION SOURCES:
+ Quest completion (+3 per quest)
+ Boss kills (+5 per unique boss)
+ Contract fulfillment (+2 per contract)
- Quest failure (-5 per failed quest)
- NPC death (-10 per important NPC killed)

REPUTATION EFFECTS (social only, NOT combat):
- Merchant prices (high rep = 10% discount)
- Quest availability (some quests require 30+ rep)
- NPC attitudes (high rep = friendly greetings)
- Contract pay (high rep = better contracts offered)
```

**Equipment Tier Display**:
```
TIER 1: Peasant Gear (rusty, crude, worn)
TIER 2: Veteran Gear (steel, reinforced, maintained)
TIER 3: Master Gear (enchanted, legendary, unique)

INFORMATIONAL ONLY:
- Shows progression via equipment quality
- NOT a power scaling system (Tier 2 sword ≠ 2× Tier 1 damage)
- Unique items can be Tier 1 (rusty dagger with magic property)
```

**Scope** (high-level):
- Renown calculation (aggregate proficiency total)
- Reputation tracking (quest/boss/contract counters)
- Equipment tier display (informational labels)
- Character sheet UI (shows all three: proficiency, reputation, equipment)

**Dependencies**:
- **Prerequisite**: Proficiency System ✅ (need weapon skills to aggregate)
- **Recommended**: Quest/Contract systems (reputation sources)

**Blocks**: Nothing (pure UI polish, no gameplay mechanics)

**Product Owner Note**: Very low priority - implement ONLY after core progression (proficiency, equipment, quests) validated. Nice polish for player feedback, not essential for fun.

---

## Proficiency System (PLANNED)

**Status**: Proposed (After VS_032 Complete) | **Size**: L (12-16h) | **Priority**: Important (progression depth)

**What**: Weapon proficiency tracking - use a weapon type, improve skill, reduce action time costs (Darklands + Mount & Blade pattern)

**Why**:
- **No-level progression** - Darklands philosophy (skills improve, not character level)
- **Time-unit synergy** - Proficiency makes actions FASTER (directly visible in turn queue!)
- **Specialization incentives** - Master daggers vs be generalist (build variety)
- **Learning by doing** - Play more, improve through repetition (Mount & Blade pattern)

**Scope**:
```
Phase 1: Proficiency tracking (weapon type + skill 0-100)
Phase 2: Action time reduction (50 skill = -15% attack time)
Phase 3: Skill progression formula (XP per attack, diminishing returns)
Phase 4: Data-driven weapon types (sword, axe, dagger, bow proficiency categories)
```

**Dependencies**: VS_032 ✅ (equipment system provides weapon usage data)

**Blocks**: Nothing (pure progression system, combat works without it)

**Detailed Design**: TBD (Tech Lead breakdown after VS_032)

---

## Character Aging & Time Pressure (PLANNED)

**Status**: Proposed (Far Future) | **Size**: M (6-8h) | **Priority**: Ideas (deferred until core combat loop validated)

**What**: Character ages over time with stat degradation - creates natural 2-4 hour run duration before retirement/death (Darklands pattern)

**Why**:
- **Time pressure** - Encourages decisive action (no infinite grinding)
- **Natural ending** - Character retires at 60-70 (permadeath alternative)
- **Realism** - Darklands philosophy (aging affects performance)

**Scope**:
- Age tracking (in-game years, not real-time)
- Stat degradation curve (peak at 25-35, decline after 40, severe after 60)
- Retirement/death condition (stats below threshold = forced retirement)

**Dependencies**: VS_032 ✅ (attributes system required for degradation)

**Blocks**: Nothing (pure progression mechanic, combat works without it)

**Detailed Design**: TBD (deferred until Phase 1-2 complete, proven fun)

---

## Combat Integration

**Attribute Effects on Combat**:
```
STRENGTH:
- Melee damage multiplier (40 STR = 1.4× damage)
- Carrying capacity (40 STR = 120 kg capacity)
- Heavy weapon requirements (two-handed axe needs 35+ STR)

DEXTERITY:
- Dodge chance (50 DEX = 50% dodge vs melee)
- Ranged accuracy (future: bow/crossbow hit chance)
- Initiative bonus (future: first strike in ambush)

ENDURANCE:
- Max health (60 END = 120 HP)
- Stamina regeneration (future: sprint/dodge costs stamina)
- Injury resistance (future: reduced bleed/poison duration)

INTELLIGENCE:
- Alchemy effectiveness (future: potion crafting quality)
- Magic power (future: spell damage/duration)
- Perception (future: trap detection, loot quality)
```

**Equipment Effects on Combat**:
```
WEAPONS (MainHand):
- Base damage (dagger = 5, sword = 8, axe = 12)
- Attack time (dagger = 80 units, sword = 100, axe = 120)
- Crit chance (rogue dagger = +15% crit)
- Range (melee = 1 cell, spear = 2 cells, bow = 10 cells)

ARMOR (Torso + Head + Legs):
- Defense bonus (leather = +5, chainmail = +10, plate = +20)
- Weight (affects action time via encumbrance)
- Damage resistance (future: slashing/piercing/blunt percentages)

SHIELDS (OffHand):
- Defense bonus (buckler = +3, kite shield = +6, tower shield = +10)
- Block chance (future: 30% chance to fully block attack)
- Weight (tower shield = 8kg, slows actions if low STR)
```

---

## Inventory Integration

**Equip Flow** (Inventory → Equipment):
```
1. User clicks "Equip" on sword in inventory
2. EquipItemCommand(actorId, itemId, EquipmentSlot.MainHand)
3. Handler validates: Item in inventory? Slot compatible?
4. If MainHand occupied: Unequip old weapon → inventory
5. Remove sword from inventory, add to MainHand slot
6. Recalculate effective attributes (weapon stats applied)
7. UI updates: Inventory slot empty, equipment slot shows sword
```

**Unequip Flow** (Equipment → Inventory):
```
1. User clicks "Unequip" on equipped helmet
2. UnequipItemCommand(actorId, EquipmentSlot.Head)
3. Handler validates: Slot occupied? Inventory has space?
4. Remove helmet from Head slot, add to inventory (auto-placement)
5. Recalculate effective attributes (helmet stats removed)
6. UI updates: Equipment slot empty, inventory shows helmet
```

**Two-Handed Weapon Special Case**:
```
EQUIP TWO-HANDED SWORD:
1. Sword requires MainHand + OffHand (both slots)
2. If OffHand occupied: Unequip shield → inventory
3. Mark MainHand slot = sword (primary)
4. Mark OffHand slot = sword (secondary reference, same ItemId!)
5. Unequip sword: Clears BOTH slots, returns ONE item to inventory

WHY: Two-handed weapon is ONE item, but occupies TWO slots
```

---

## Next Steps

**Immediate Priority** (VS_032 Breakdown):
1. ⏳ **Tech Lead** breaks down VS_032 into 4 phases (3+4+3+2 = 12h estimate)
2. ⏳ **Dev Engineer** implements Phase 1 (Attributes) - Prove stat system works
3. ⏳ **Dev Engineer** implements Phase 2 (Equipment Slots) - Prove equip flow works
4. ⏳ **Dev Engineer** implements Phase 3 (Stat Modifiers) - Prove combat integration works
5. ⏳ **Dev Engineer** implements Phase 4 (Weight System) - Prove build variety works

**After VS_032 Complete**:
- **Product Owner** decides: Proficiency next? Or Enemy AI (VS_011)?
- **Test Specialist** validates build variety (light skirmisher vs heavy tank playstyles)
- **Tech Lead** reviews: Is realistic armor layers needed? Or sufficient depth already?

**Future Work**:
- Proficiency System (after equipment validated)
- Character Aging (far future, deferred until core loop proven fun)
- Realistic Armor Layers (if equipment system feels shallow after playtesting)

**Product Owner Decisions Needed**:
- Approve VS_032 for Tech Lead breakdown?
- Priority after VS_032: Proficiency progression OR Enemy AI?
- Realistic armor layers: Defer until Phase 2? Or never (complexity not worth it)?

---

**Last Updated**: 2025-10-09 23:11
**Status**: VS_032 proposed (awaiting Tech Lead breakdown)
**Owner**: Product Owner (roadmap maintenance), Tech Lead (VS_032 breakdown), Dev Engineer (implementation)

---

*This roadmap provides comprehensive technical details for Darklands stats and progression systems. See [Roadmap.md](Roadmap.md) for high-level project overview.*
