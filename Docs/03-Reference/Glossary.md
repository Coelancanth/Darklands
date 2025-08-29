# Darklands Glossary

*The authoritative vocabulary for code, documentation, and team discussion.*

**Rule**: If you're unsure what to call something, check here first. All code must use these exact terms.

## Strategic Layer (Overworld)

**Overworld**  
The macro-level game map where strategic decisions occur.
- **Structure**: Node-based or continuous travel map
- **Content**: Locations (towns, dungeons, encounters)
- **Activities**: Travel, quest management, resource planning
- **Not called**: World Map (too generic), Campaign Map
- **Code**: `OverworldState`, `LocationNode`, `TravelCommand`

**Location**  
A visitable place on the overworld.
- **Types**: Town, Dungeon, Encounter, Battlefield
- **Properties**: Name, Type, Danger Level, Available Quests
- **Transitions**: Entry point from overworld to tactical
- **Code**: `ILocation`, `LocationType`, `EnterLocationCommand`

**Quest**  
Time-limited objective providing rewards and reputation.
- **Also called**: Contract (for mercenary-style quests)
- **Properties**: Objective, Reward, Time Limit, Reputation Impact
- **States**: Available, Active, Complete, Failed, Expired
- **Code**: `Quest`, `QuestObjective`, `AcceptQuestCommand`

**Character**  
The player's persistent solo avatar across runs.
- **NOT**: Actor (which is for combat entities)
- **Properties**: Stats, Skills, Traits, Reputation, Age
- **Persistence**: Survives between combats, dies permanently
- **Code**: `PlayerCharacter`, `CharacterSheet`, `CharacterProgression`

**Reputation**  
Standing with factions affecting available quests and prices.
- **Range**: -100 (hostile) to +100 (revered)
- **Sources**: Quest completion, combat actions, choices
- **Effects**: Quest availability, shop prices, NPC reactions
- **Code**: `ReputationSystem`, `FactionStanding`

## Core Combat System

**Scheduler**  
The priority queue that determines turn order in combat.
- **Structure**: SortedSet of actors ordered by NextTurn
- **Purpose**: Deterministic turn sequencing and coordination
- **Not called**: Queue, Timeline, TurnOrder, TurnManager
- **Code**: `CombatScheduler`, `ISchedulable`, `ScheduleActorCommand`, `ProcessNextTurnCommand`

**Turn**  
A single actor's opportunity to perform actions in combat.
- **Starts**: When actor reaches front of scheduler
- **Duration**: Determined by action's time cost
- **Ends**: After action executes and actor re-scheduled
- **Code**: `ProcessTurnCommand`, `NextTurn` property

**Actor**  
Any combat entity that takes turns in tactical battles.
- **Properties**: Id (Guid), NextTurn (TimeUnit), State, Position
- **Types**: PlayerActor (Character in combat), Enemy, NPC, Environmental
- **Relationship**: Character becomes PlayerActor in combat
- **Not called**: Entity, Unit, Combatant
- **Code**: `IActor`, `ScheduleActorCommand`, `ActorState`

## Time System

**TimeUnit**  
The fundamental measure of action duration in milliseconds.
- **Range**: 0 to 10,000ms (10 seconds max)
- **Precision**: Integer milliseconds (no fractions)
- **Purpose**: Deterministic time calculations
- **Code**: `TimeUnit`, `TimeUnitCalculator`

**Time Cost**  
How many TimeUnits an action requires to complete.
- **Calculation**: Base cost × modifiers (agility, encumbrance)
- **Examples**: Quick Attack (500ms), Heavy Strike (2000ms)
- **Not called**: Duration, Delay, Speed
- **Code**: `CombatAction.TimeCost`, `CalculateTimeCost()`

**Next Turn**  
The absolute time when an actor will act again.
- **Calculation**: CurrentTime + TimeCost
- **Storage**: TimeUnit value in timeline
- **Not called**: NextAction, TurnTime, Schedule
- **Code**: `ISchedulable.NextTurn`

## Grid & Positioning

**Grid**  
The tactical combat battlefield divided into discrete tiles.
- **Structure**: 2D array of tiles (typically square or hex)
- **Purpose**: Discrete positioning for tactical combat
- **Not called**: Map, Board, Battlefield (those are higher level)
- **Code**: `CombatGrid`, `GridSize`, `TileType`

**Tile**  
A single space on the combat grid.
- **Properties**: Coordinates (x,y), Terrain, Occupant, Passability
- **Size**: One actor per tile (no stacking)
- **Not called**: Square, Cell, Space
- **Code**: `Tile`, `TileCoordinates`, `TileState`

**Position**  
An actor's location on the combat grid.
- **Format**: (x, y) coordinates or Tile reference
- **Movement**: Costs TimeUnits based on distance and terrain
- **Validation**: Must be within grid bounds and passable
- **Code**: `Position`, `MoveToPositionCommand`

**Movement**  
Changing position on the grid during combat.
- **Cost**: TimeUnits based on distance (e.g., 100 per tile)
- **Modifiers**: Terrain, encumbrance, injuries affect cost
- **Validation**: Path must be clear and within movement range
- **Code**: `MovementCalculator`, `PathfindingService`

## Combat Actions

**Combat Action**  
A discrete action that can be performed during combat.
- **Properties**: Name, TimeCost, Effects
- **Examples**: Attack, Defend, Move, UseItem
- **Validation**: All actions must have valid time costs
- **Code**: `CombatAction`, `ICombatAction`

**Action Effect**  
The outcome or consequence of a combat action.
- **Types**: Damage, Healing, StatusEffect, Movement
- **Application**: After action time cost paid
- **Not called**: Result, Outcome, Consequence
- **Code**: `IActionEffect`, `ApplyEffect()`

## Equipment System

**Equipment**  
Items providing combat stats with speed/damage tradeoffs.
- **Types**: Weapon, Armor, Accessory
- **Key Tradeoff**: Damage vs Speed (weapons), Protection vs Weight (armor)
- **Properties**: Durability, Weight, Requirements
- **Code**: `IEquipment`, `EquipmentSlot`, `EquipCommand`

**Weapon**  
Equipment that determines attack damage and speed.
- **Properties**: Damage, Speed (affects TimeCost), Range, Weight
- **Examples**: Dagger (low damage, fast), Sword (balanced), Hammer (high damage, slow)
- **Durability**: Degrades with use, requires repair
- **Code**: `Weapon`, `WeaponType`, `AttackWithWeaponCommand`

**Armor**  
Equipment providing protection at the cost of speed.
- **Properties**: Protection, Weight (affects Encumbrance), Coverage
- **Effect**: Reduces damage taken, increases all action TimeCosts
- **Durability**: Degrades when hit, requires repair
- **Code**: `Armor`, `ArmorType`, `DamageReduction`

## Status & Injuries

**Injury**  
Persistent damage affecting character performance.
- **Duration**: Lasts beyond combat until healed
- **Effects**: Stat penalties, increased TimeCosts, ability restrictions
- **Types**: Wounds, Fractures, Bleeding, Exhaustion
- **Code**: `Injury`, `InjuryType`, `ApplyInjuryCommand`

**Status Effect**  
Temporary modifier affecting combat performance.
- **Duration**: Time-limited (rounds or TimeUnits)
- **Types**: Buffs (positive), Debuffs (negative), Conditions
- **Examples**: Stunned, Poisoned, Blessed, Slowed
- **Code**: `StatusEffect`, `StatusDuration`, `ApplyStatusCommand`

## Combat States

**Initiative**  
The initial turn order determination at combat start.
- **Calculation**: Based on agility and random factor
- **Purpose**: Determine who acts first
- **Not called**: Speed, Priority, TurnOrder
- **Code**: `CalculateInitiative()`, `InitiativeModifier`

**Agility**  
Actor attribute affecting action speed.
- **Range**: 1-100 (higher = faster actions)
- **Effect**: Reduces time costs by percentage
- **Not called**: Speed, Dexterity, Quickness
- **Code**: `Agility`, `AgilityModifier`

**Encumbrance**  
Weight penalty affecting action speed.
- **Calculation**: TotalWeight / MaxCarryWeight
- **Effect**: Increases time costs by percentage
- **Range**: 0.0 (unencumbered) to 2.0 (immobilized)
- **Code**: `Encumbrance`, `EncumbranceModifier`

## Game Loop Concepts

**Encounter**  
Transition point from strategic overworld to tactical combat.
- **Trigger**: Entering hostile location, random travel event
- **Setup**: Generates combat grid, places actors, determines initiative
- **Outcome**: Victory, defeat, retreat affect overworld state
- **Code**: `EncounterTrigger`, `BeginEncounterCommand`, `EncounterResult`

**Permadeath**  
Permanent character death ending the current run.
- **Trigger**: Character health reaches zero
- **Effect**: Save deleted, must start new character
- **Alternative**: "Harsh saves" mode (limited save points)
- **Code**: `CharacterDeath`, `PermadeathHandler`, `SaveSystem`

**Season**  
Time progression affecting world state and quests.
- **Progression**: Spring → Summer → Autumn → Winter
- **Effects**: Quest availability, travel difficulty, prices
- **Character Aging**: Years pass, affecting stats
- **Code**: `Season`, `TimePassageSystem`, `SeasonalEffects`

**Save State**  
Preserved game progress between sessions.
- **Modes**: Permadeath (ironman), Harsh (limited), Normal
- **Content**: Character, overworld state, quest progress
- **Combat Saves**: Exact turn state including TimeUnit positions
- **Code**: `SaveGame`, `SaveSerializer`, `LoadGameCommand`

## Architecture Terms

### CQRS Pattern

**Command**  
A request to perform an action (CQRS pattern).
- **Suffix**: Always ends with "Command"
- **Returns**: `Fin<T>` for error handling
- **Examples**: `ScheduleActorCommand`, `ProcessTurnCommand`
- **Code**: `IRequest<Fin<T>>` from MediatR

**Handler**  
Processes a command or query (CQRS pattern).
- **Suffix**: Always ends with "Handler"
- **Purpose**: Contains business logic
- **Pattern**: One handler per command/query
- **Code**: `IRequestHandler<TRequest, TResponse>`

**Query**  
A request to retrieve data without side effects.
- **Suffix**: Always ends with "Query"
- **Returns**: `Fin<T>` with requested data
- **Examples**: `GetTimelineQuery`, `GetActorStateQuery`
- **Code**: `IRequest<Fin<T>>` from MediatR

### MVP Pattern (ADR-001)

**Model**  
Pure C# domain logic with no dependencies.
- **Location**: `src/Domain/` and `src/Application/`
- **Purpose**: Business rules and data manipulation
- **Dependencies**: None (LanguageExt, MediatR only)
- **Code**: Domain entities, value objects, handlers

**View Interface**  
Contract defining UI capabilities for Presenters.
- **Location**: `src/Features/[Feature]/Views/` (Core project)
- **Purpose**: Dependency inversion between Presenter and View
- **Methods**: Async Task methods for UI operations
- **Code**: `I*View` interfaces with async signatures

**View Implementation**  
Godot-specific UI implementation of View Interface.
- **Location**: `godot_project/features/[feature]/`
- **Purpose**: Display, input handling, thread marshalling
- **Dependencies**: Godot.NET SDK + Core reference
- **Threading**: Uses CallDeferred internally for UI updates
- **Code**: `*View.cs` classes extending Godot nodes

**Presenter**  
Pure C# orchestration of Model and View interaction.
- **Location**: `src/Features/[Feature]/Presenters/` (Core project)
- **Dependencies**: MediatR, LanguageExt only (NO Godot)
- **Pattern**: Calls View interface methods (dependency inversion)
- **Threading**: View implementations handle CallDeferred internally
- **Code**: `*Presenter` classes inheriting `PresenterBase<TView>`

### Project Structure (ADR-001)

**Core Project**  
Pure C# game logic with no Godot dependencies.
- **File**: `src/Darklands.Core.csproj`
- **Purpose**: All game logic, moddable by C# only
- **Dependencies**: MediatR, LanguageExt, DI, Serilog
- **Not called**: Engine, Logic, Business

**Godot Project**  
Main Godot project referencing Core.
- **File**: `Darklands.csproj`
- **Purpose**: UI, rendering, input, audio
- **Dependencies**: Godot.NET SDK + Core reference
- **Not called**: UI, Frontend, Client

**Test Project**  
Unit and integration tests for Core.
- **File**: `tests/Darklands.Core.Tests.csproj`
- **Purpose**: Validate all Core functionality
- **Dependencies**: xUnit, FluentAssertions, Core
- **Code**: `*Tests.cs` classes

### Infrastructure Components

**GameStrapper**  
Dependency injection container configuration.
- **Location**: `src/Infrastructure/DependencyInjection/`
- **Purpose**: Register all services and handlers
- **Pattern**: Single registration point
- **Code**: `GameStrapper.Configure()`

**Behavior**  
MediatR pipeline behavior for cross-cutting concerns.
- **Suffix**: Always ends with "Behavior"
- **Purpose**: Logging, error handling, validation
- **Examples**: `LoggingBehavior`, `ErrorHandlingBehavior`
- **Code**: `IPipelineBehavior<TRequest, TResponse>`

### Feature Organization

**Feature**  
Self-contained business capability with all layers.
- **Structure**: Commands/, Presenters/, Views/ folders
- **Principle**: Organized by business value, not technical layer
- **Examples**: Combat, Inventory, Character
- **Code**: `src/Features/[Feature]/` hierarchy

**Vertical Slice**  
End-to-end feature implementation spanning all layers.
- **Scope**: Single user story from UI to domain
- **Size**: Maximum 2 days implementation
- **Independence**: Deployable and testable alone
- **Code**: Includes all 4 phases for one capability

**Feature Boundary**  
Clear separation between different business capabilities.
- **Communication**: Through MediatR commands/queries only
- **Shared Code**: Domain value objects and infrastructure only
- **Not shared**: Feature-specific handlers and presenters
- **Purpose**: Enable independent development and testing

## Value Objects

**Value Object**  
Immutable object defined by its attributes.
- **Examples**: TimeUnit, CombatAction, Position
- **Creation**: Via factory methods with validation
- **Not**: Entities (which have identity)
- **Code**: `record struct`, private constructors

**Factory Method**  
Static method that creates validated instances.
- **Pattern**: `Create()` or `From*()`
- **Returns**: `Fin<T>` with success or error
- **Purpose**: Prevent invalid object creation
- **Code**: `TimeUnit.Create()`, `CombatAction.Create()`

## Error Handling

**Fin<T>**  
Functional error handling monad from LanguageExt.
- **Success**: `FinSucc(value)`
- **Failure**: `FinFail<T>(error)`
- **Purpose**: Avoid exceptions in domain logic
- **Code**: `Fin<TimeUnit>`, `Fin<Unit>`

## Phased Implementation (ADR-002)

**Phase**  
Mandatory stage of feature implementation.
- **Sequence**: 1→2→3→4 (no skipping allowed)
- **Gate**: Tests must pass before next phase
- **Commit**: Include `[Phase X/4]` in message
- **Purpose**: Prevent integration bugs and technical debt

**Phase 1: Domain**  
Pure C# business logic with zero dependencies.
- **Location**: `src/Domain/[Feature]/`
- **Content**: Entities, value objects, business rules
- **Gate**: 100% unit tests passing, <100ms execution
- **Dependencies**: LanguageExt only
- **Code**: Pure functions, immutable records

**Phase 2: Application**  
CQRS commands and handlers for use cases.
- **Location**: `src/Application/[Feature]/Commands/`
- **Content**: Commands, queries, handlers
- **Gate**: Handler tests passing, <500ms execution
- **Dependencies**: MediatR + Phase 1
- **Code**: `IRequest<Fin<T>>`, `IRequestHandler`

**Phase 3: Infrastructure**  
State management and external services.
- **Location**: `src/Infrastructure/[Feature]/`
- **Content**: Repositories, state services, persistence
- **Gate**: Integration tests passing, <2s execution
- **Dependencies**: DI container + Phases 1-2
- **Code**: Service implementations, data access

**Phase 4: Presentation**  
Godot UI and user interaction.
- **Location**: `godot_project/features/[feature]/`
- **Content**: Views, scenes, input handling
- **Gate**: Manual testing in Godot editor
- **Dependencies**: Godot.NET + Phases 1-3
- **Code**: Godot nodes, MVP presenters

### Phase Gate Protocol

**Gate**  
Quality checkpoint preventing phase progression.
- **Rule**: ALL tests must be GREEN to proceed
- **Tools**: `./scripts/test/quick.ps1` for validation
- **Enforcement**: No exceptions for "simple" features
- **Commit**: Only after gate passes

---

## Usage Guidelines

1. **Consistency**: Always use these exact terms in code and documentation
2. **No Synonyms**: Don't create alternative names for clarity
3. **Case Sensitivity**: Use exact casing as shown in Code examples
4. **New Terms**: Must be added here before use in code
5. **Conflicts**: If terms conflict with Godot/C#, prefix with "Darklands"

## Examples of Correct Usage

✅ **Correct**:
```csharp
public class ScheduleActorCommand : IRequest<Fin<Unit>>
{
    public Guid ActorId { get; init; }
    public TimeUnit NextTurn { get; init; }
}
```

❌ **Incorrect**:
```csharp
public class ScheduleEntityCommand  // Wrong: "Entity" not "Actor"
{
    public Guid CharacterId { get; init; }  // Wrong: "Character" not "Actor"
    public TimeUnit TurnTime { get; init; }  // Wrong: "TurnTime" not "NextTurn"
}
```

---

*Last Updated: 2025-08-29 17:15 - Major expansion: Added Strategic Layer, Equipment, Grid, Status systems to align with Vision.md*