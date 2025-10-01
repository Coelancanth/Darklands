# Darklands - Game Vision

**Genre**: Solo Tactical Roguelike with Strategic Layer
**Core Loop**: Navigate harsh world (macro) → Survive tactical encounters (micro)
**Unique Selling Point**: Spiritual successor to Darklands (1992) - realistic medieval combat simulation with no levels, skill-based progression, and emergent narrative in a persistent roguelike world

**Inspirations**:
- **Darklands (1992)**: No-level skill progression, realistic armor, gritty medieval setting, reputation system
- **Battle Brothers**: Macro-layer world navigation, contract system, economic survival
- **Stoneshard**: Grid-based time-unit combat, detailed injury mechanics
- **Angband**: Build-focused itemization, meaningful uniques, permadeath
- **NeoScavenger**: Tetris inventory, drag-and-drop interaction mechanics, survival crafting
- **RimWorld**: Emergent narrative, AI-driven storytelling
- **Mount & Blade**: Use-based proficiency system

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
- Character permadeath (roguelike tradition)
- Persistent world that evolves without you
- Time pressure (quests expire, seasons change, character ages)
- Every decision has opportunity cost

### 4. Skill-Based Progression (No Levels)
**Inspired by Darklands (1992) and Mount & Blade**

- **No experience levels** - your character doesn't have "Level 15"
- **Proficiency system** - use a weapon type, improve with that weapon type
  - Higher proficiency → faster actions (reduced time costs in combat)
  - Specialization naturally emerges from usage patterns
  - "Sword master" vs. "Generalist" are viable strategies
- **Learning by doing** - play more, get stronger through skill improvement
- **No stat inflation** - progression is horizontal (more options) not vertical (bigger numbers)

### 5. Realistic Combat Simulation
**Inspired by Darklands (1992) and Kingdom Come: Deliverance**

- **Layered armor system**:
  - Multiple layers: padding (gambeson), mail (chainmail), plate (full armor)
  - Each layer has individual durability, weight, and protection values
  - Hit location matters (head/torso/limbs have different armor coverage)
  - Damage types interact realistically (blunt trauma through armor, piercing penetrates mail, slashing ineffective vs. plate)
- **Weight = time cost**: Heavy armor protects more but slows all actions
- **Gear degradation**: Armor breaks, weapons dull, repairs cost money (economic pressure)

### 6. Spatial Interaction & Itemization
**Inspired by NeoScavenger and Angband**

**Item Philosophy**:
- **No level scaling** - a sword is always a sword, not "Level 20 Sword +5"
- **Uniques with build-enabling properties** - each legendary item opens new strategies
- **Situational value** - "best" item depends on playstyle, enemy type, situation
- **Constrained randomization** - minor stat variance exists, but a dagger is always fast/low-damage

**Tetris Inventory System (NeoScavenger-style)**:
- **Spatial grid management** - items have physical shapes (sword is 1x3, potion is 1x1, armor is 2x3)
- **Weight affects gameplay** - carrying capacity impacts movement speed and action time costs
- **Looting as puzzle** - tactical decisions about what to keep vs. leave behind
- **Equipment durability integration** - carry backup weapons? Or more consumables? Real trade-offs.

**Drag-and-Drop Interaction Panel (NeoScavenger innovation)**:
- **Central interaction grid** - combine items + skills + location context to discover actions
- **Discovery-based mechanics** - experiment to find what combinations work
  - No quest markers saying "use lockpick here"
  - You see a locked chest, you have lockpicks, you try dragging them together
- **Skill-gated interactions** - you can SEE items but can't use them effectively without proper skills
  - Unskilled medic can bandage wounds (stop bleeding)
  - Skilled medic can set bones, stitch deep cuts, diagnose diseases
- **Context-aware actions** - location and environment matter
  - Same items do different things in different places
  - Campfire location unlocks cooking, workbench unlocks repairs

**Example Interactions**:
```
[Medic Skill] + [Bandages] + [Wounded Character] → "Treat Wounds" (stop bleeding)
[Lockpicking Skill] + [Lockpicks] + [Locked Chest] → "Pick Lock" (or break picks on failure)
[Blacksmith Skill] + [Whetstone] + [Sword] → "Sharpen Weapon" (+damage, consumes whetstone durability)
[Campfire] + [Raw Meat] + [Cooking Skill] → "Cook Food" (better healing than raw, avoids disease)
[Alchemy Skill] + [Herbs] + [Mortar & Pestle] → "Brew Potion" (discover recipes through experimentation)
[Mechanic Skill] + [Scrap Metal] + [Tools] → "Repair Equipment" (restore durability to armor/weapons)
```

**Why This Matters**:
- **Emergent complexity** - simple drag-and-drop rules create deep interactions
- **Rewards experimentation** - "what happens if I combine THIS with THAT?"
- **Skills feel meaningful** - unlocking new interactions, not just stat bonuses
- **Visual/spatial thinking** - no hidden menus, everything is tangible and manipulable
- **Synergy with proficiency system** - use medical supplies → improve Medic skill → unlock better treatments

### 7. Emergent Narrative
**Inspired by RimWorld and Darklands (1992)**

- **AI storyteller** - procedural events based on character state and world conditions
- **Reputation/faction system** - your choices affect NPC reactions and quest availability
  - Help the church → gain saint reputation → unlock religious quests
  - Rob merchants → become outlaw → guards attack on sight
- **No scripted story** - every run creates unique memorable tales
- **Origins with mechanical depth**:
  - Starting background (noble, soldier, peasant, scholar, etc.)
  - Affects starting proficiencies, equipment, and faction standings
  - Unlocks origin-specific quest lines (noble has political intrigue, soldier has mercenary contracts)
  - Defines early viable strategies but doesn't lock long-term progression

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

**The smallest version that demonstrates core systems working together:**

### **Phase 1: Combat Core** (First playable)
1. **Character**: Single character with base attributes (Strength, Agility, Endurance, Perception)
2. **Combat**: Grid-based with time-unit action queue visualization
3. **Proficiency**: Basic weapon proficiency tracking (use sword → improve sword skill)
4. **Equipment**:
   - Weapons with speed/damage tradeoff (dagger fast, sword balanced, axe slow)
   - Single armor layer (simplified - just protection/weight values)
5. **Enemies**: 3 types with different behaviors (fast/weak, balanced, slow/strong)
6. **Victory condition**: Survive 5 combat encounters

**Goal**: Prove time-unit combat is fun and proficiency progression feels rewarding.

### **Phase 2: Itemization & Depth** (Replayability hook)
1. **Layered armor**: Add padding + mail layers (plate comes later)
2. **Loot system**: 10 unique weapons with build-enabling properties
3. **Tetris inventory**: Spatial grid with weight/capacity management
4. **Damage types**: Slashing, piercing, blunt (interact with armor types)

**Goal**: Prove build variety creates different playstyles and replay value.

### **Phase 3: Strategic Layer** (World integration)
1. **Overworld**: Simple node map (3 towns, 2 dungeons)
2. **Quests**: Basic contracts (clear dungeon, deliver item)
3. **Economy**: Money for repairs, equipment purchases
4. **Reputation**: Simple faction system (2-3 factions with basic standing)

**Goal**: Demonstrate macro/micro loop integration.

### **Phase 4: Emergent Narrative** (Polish & depth)
1. **Origins**: 3-5 starting backgrounds with mechanical differences
2. **Event system**: Random encounters with choices and consequences
3. **Full reputation**: Faction relationships affect quest availability and NPC behavior
4. **Character aging**: Time pressure mechanic

**Goal**: Create "memorable run stories" like RimWorld.

## 🚫 What We're NOT Building (Yet)

- Party/companion system (true SOLO experience)
- Magic system (unless core to combat)
- Complex crafting (basic repair/upgrade only)
- Base building or hideouts
- Multiplayer
- Full 3D graphics (stay 2D for modding ease)

## 📈 Success Metrics

**Combat & Core Loop**:
- Time-unit system creates "one more turn" addiction (like DCSS)
- Combat depth rivals traditional roguelikes (positioning + timing + gear choices matter)
- Death feels fair ("I should have waited for better gear" not "RNG killed me")

**Progression & Replayability**:
- Proficiency system feels rewarding (visible improvement from hour 1 → hour 100)
- Build variety enables different playstyles (dagger assassin vs. armored tank)
- No two runs feel the same (loot variety + origin choices + emergent events)

**Emergent Stories**:
- Players share "remember when I..." stories on forums
- Failed runs create memorable narratives (like RimWorld/Dwarf Fortress)
- Origin choices create different early-game experiences

**Community & Longevity**:
- Mods appear within first month of release
- Speedrunning community emerges (optimize proficiency gains)
- Wiki community documents build archetypes and loot strategies

---

## 🎯 Design Principles

**From Darklands (1992)**:
1. **Realism enhances tactics** - Realistic armor/weapons create depth, not tedium
2. **No artificial gates** - Player skill matters more than character level
3. **Medieval authenticity** - Grounded in history (no fantasy elements unless justified)

**From Roguelike Tradition**:
1. **Permadeath forces meaningful choices** - Every decision has weight
2. **Procedural content creates replayability** - No scripted optimal path
3. **Emergent complexity from simple rules** - Deep interactions, not feature bloat

**Our Philosophy**:
1. **Systems over content** - 10 weapons with unique properties > 100 stat sticks
2. **Horizontal progression** - More options (proficiencies, gear) not bigger numbers
3. **Respect player time** - Runs should be 2-4 hours, not 40-hour slogs
4. **Mod-friendly architecture** - Players extend the game's lifetime

---

*"A spiritual successor to Darklands (1992) - where skill progression, tactical combat, and emergent storytelling create memorable tales of a lone warrior's journey through a harsh medieval world"*