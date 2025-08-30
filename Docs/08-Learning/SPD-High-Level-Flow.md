# Shattered Pixel Dungeon - High-Level Control & Data Flow

**Created**: 2025-08-30
**Purpose**: Understand SPD's architecture at a high level for learning purposes

## ⚠️ CRITICAL: Licensing Considerations

### GPL-3.0 License Implications
Shattered Pixel Dungeon is licensed under **GPL-3.0** (GNU General Public License v3). This means:

**❌ CANNOT USE ASSETS DIRECTLY IN DARKLANDS**
- GPL-3.0 is a "viral" copyleft license
- If we use ANY GPL code or assets, our ENTIRE project becomes GPL
- This would require us to open-source Darklands completely
- **We must NOT copy sprites, sounds, or code directly**

**✅ WHAT WE CAN DO**:
1. **Learn from the architecture** (ideas cannot be copyrighted)
2. **Study their solutions** to similar problems
3. **Create our own assets** inspired by (but not copying) their style
4. **Reference their patterns** while writing our own code

### Asset Attribution
SPD's assets are located in:
- `core/src/main/assets/sprites/` - Character sprites
- `core/src/main/assets/environment/` - Tiles and environments
- `core/src/main/assets/interfaces/` - UI elements

**For VS_008**: We need to create our own placeholder sprites or use CC0/MIT licensed assets.

## 🔄 High-Level Control Flow

### 1. Game Initialization Flow
```
Main Entry Point (Platform Specific)
    ↓
ShatteredPixelDungeon (Main Game Class)
    ↓
Scene Management (Title → Game → etc.)
    ↓
Dungeon.newLevel() - Generate/Load Level
    ↓
Actor.init() - Initialize all actors
    ↓
GameScene - Main gameplay scene
    ↓
Actor.process() - Main game loop
```

### 2. Turn Processing Flow (Actor System)
```
Actor.process() [Main Thread]
    ↓
Find actor with earliest time
    ↓
Set current time = actor.time
    ↓
actor.act() returns boolean
    ├─ true: Continue processing
    └─ false: Wait for input
        ↓
    Hero.act() waits for player input
    Other actors process AI
```

### 3. Player Action Flow
```
User Input (Touch/Click)
    ↓
GameScene interprets input
    ↓
Create HeroAction
    ↓
Hero.ready = true (signals turn ready)
    ↓
Hero.act() processes action
    ↓
Spend time based on action
    ↓
Return to Actor.process()
```

### 4. Combat Flow
```
Attack Initiated
    ↓
Calculate hit (accuracy vs defense)
    ├─ Miss: Show "dodged" effect
    └─ Hit: Continue
        ↓
    Roll damage
        ↓
    Apply damage modifiers
        ↓
    Subtract armor
        ↓
    Apply damage to HP
        ↓
    Check for death
        ↓
    Trigger on-hit effects
```

## 📊 Data Flow Architecture

### 1. Core Data Structures
```
Dungeon (Static Game State)
├── level (Current Level)
├── hero (Player Character)
├── depth (Current floor)
└── chapters (Progress tracking)

Level (Map Data)
├── map[] (1D tile array)
├── mobs (List of enemies)
├── heaps (Item piles)
└── blobs (Environmental effects)

Actor (Time Management)
├── time (When to act next)
├── id (Unique identifier)
└── actPriority (Tie breaker)

Char extends Actor (Characters)
├── pos (Position on map)
├── HP/HT (Health)
├── buffs (Status effects)
└── sprite (Visual representation)
```

### 2. State Management Pattern
```
Game State
    ↓
Stored in static Dungeon class
    ↓
Accessed globally by all systems
    ↓
Persisted via Bundle system (save/load)
```

### 3. Event/Notification Flow
```
Action occurs (e.g., damage)
    ↓
Direct method calls (no event bus)
    ↓
Update game state
    ↓
Update visual (sprite.showStatus())
    ↓
Log messages (GLog.add())
```

## 👤 User Story: Complete Turn

### Player's Perspective:
1. **See**: Grid with hero and enemies
2. **Tap**: Target tile to move/attack
3. **Watch**: Hero moves/attacks
4. **Wait**: Enemies take their turns
5. **Repeat**: Control returns to player

### System Flow:
```
1. GameScene waits for input
2. Player taps tile
3. System creates HeroAction.Move(targetPos)
4. Hero.ready = true
5. Actor.process() resumes
6. Hero.act() executes:
   - If enemy at target: Attack
   - Else: Move toward target
7. Hero.spend(1.0f) - uses time
8. Process other actors until hero.time is earliest again
9. Return control to player
```

## 🏗️ Key Architectural Patterns

### 1. Time-Based Turn System
- All entities are Actors with time values
- Process actor with earliest time
- Actions "spend" time to determine next turn
- Allows variable speed (spend less = act more often)

### 2. State Machine AI
- Each mob has state: SLEEPING, HUNTING, WANDERING, FLEEING
- States determine behavior in act() method
- Transitions based on conditions (see enemy, low health, etc.)

### 3. Single Responsibility Classes
- `Actor`: Time management only
- `Char`: Combat and movement
- `Mob`: AI behavior
- `Hero`: Player input handling
- `Buff`: Status effect logic

### 4. Static Game State
- `Dungeon` class holds all game state statically
- No dependency injection
- Easy global access but tight coupling
- Simple for single-player game

## 🎮 VS_008 Implications

For our Grid Scene implementation (VS_008), we can learn:

### What to Adopt:
1. **Grid as 1D array** with coordinate conversion (simpler than 2D)
2. **Click-to-move** interaction pattern
3. **Turn visualization** through sprite animations
4. **Simple state updates** without complex events

### What to Avoid:
1. **Static global state** - Use dependency injection instead
2. **Direct sprite manipulation** - Use MVP pattern
3. **Tight coupling** - Keep layers separated
4. **GPL assets** - Create our own or use permissive licenses

### Placeholder Assets for VS_008:
Instead of SPD sprites, we should:
1. Use colored squares/circles for initial prototype
2. Find CC0 sprites from OpenGameArt.org
3. Create simple programmer art
4. Commission original sprites later

## 📝 Summary

SPD's architecture is:
- **Simple and direct** - No over-engineering
- **Turn-based via time units** - Elegant speed handling
- **State-focused** - Clear game state management
- **Monolithic** - Everything in one codebase

For Darklands, we should:
- **Learn the patterns** but not copy implementation
- **Keep our Clean Architecture** with proper separation
- **Create original assets** to avoid GPL issues
- **Use the time-unit concept** if it fits our vision

---

**Remember**: We're learning from SPD, not cloning it. Their solutions inform our decisions, but Darklands must be its own unique game with its own identity and legal independence.