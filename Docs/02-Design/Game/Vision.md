# Darklands - Game Vision

**Genre**: Solo Tactical Roguelike with Strategic Layer  
**Core Loop**: Navigate harsh world (macro) → Survive tactical encounters (micro)  
**Unique Selling Point**: Deep solo character progression with time-unit tactical combat and persistent world simulation

## 🎯 Core Pillars

### 1. Dual-Layer Gameplay
- **Macro Layer** (Battle Brothers world + Darklands depth)
  - Overworld travel between towns/dungeons/encounters
  - Solo character development (skills, traits, reputation)
  - Contract/quest system for progression
  - Economic survival (food, equipment, repairs, lodging)
  
- **Micro Layer** (Stoneshard + Traditional Roguelike)
  - Time-unit based combat (each action costs different time)
  - Grid-based tactical positioning
  - Environmental interactions and destructible terrain
  - Detailed injury/status system affecting performance

### 2. Extensibility First
- Data-driven design (JSON/YAML for all game content)
- Mod-friendly architecture from day one
- Clear separation: Engine → Game Rules → Content
- Workshop/mod support planned early

### 3. Meaningful Consequences
- Character permadeath (or harsh saves like Stoneshard)
- Persistent world that evolves without you
- Time pressure (quests expire, seasons change, age)
- Every decision has opportunity cost

## 🎮 Core Gameplay Loop

```
World Map → Accept Quest → Travel → Enter Location
    ↓                                      ↓
Character Management              Tactical Combat
(Skills, Gear, Health)           (Time-unit based)
    ↓                                      ↓
Survival Pressure ← Success/Fail → Reputation/Rewards
```

## ⚔️ Time-Unit Combat System

### Core Concept
- Every action has a **time cost** (measured in ticks/units)
- Faster characters/actions get more "turns" 
- Example costs:
  - Move 1 tile: 100 units
  - Quick dagger stab: 50 units  
  - Heavy sword swing: 150 units
  - Drink potion: 75 units
  - Reload crossbow: 200 units

### Why This Matters
- Weapon choice becomes tactical (speed vs damage)
- Armor weight affects all action speeds
- Positioning matters (backing away costs time)
- Status effects can slow/speed actions

## 🏗️ Technical Architecture Notes

### Two Distinct Systems
1. **Strategic System** - Location-based, event-driven
2. **Tactical System** - Time-unit based with action queue

### Key Technical Challenges
- Time-unit system with clear action queue visualization
- Seamless transition between overworld and tactical
- Save system that preserves exact combat state
- Mod system for items, enemies, locations, skills
- AI that understands time costs for tactics

## 📊 Minimum Viable Game (MVG)

**The smallest version that demonstrates both layers working together:**

1. **Character**: Single character with 3-4 base stats
2. **Overworld**: Simple node map (3 towns, 2 dungeons)
3. **Quests**: 2 types (clear dungeon, deliver item)
4. **Combat**: Grid-based with time-unit system
5. **Equipment**: Weapon (speed/damage tradeoff) + Armor (protection/weight)
6. **Enemies**: 3 types with different time costs (fast rat, normal bandit, slow ogre)

## 🚫 What We're NOT Building (Yet)

- Party/companion system (true SOLO experience)
- Magic system (unless core to combat)
- Complex crafting (basic repair/upgrade only)
- Base building or hideouts
- Multiplayer
- Full 3D graphics (stay 2D for modding ease)

## 📈 Success Metrics

- Combat depth rivals Desktop Dungeons or DCSS
- Every run feels different due to build variety
- Death feels fair ("I should have waited" not "RNG killed me")
- Time-unit system creates "one more turn" addiction
- Mods appear within first month

---

*"A lone warrior's journey through a harsh world where every second counts"*