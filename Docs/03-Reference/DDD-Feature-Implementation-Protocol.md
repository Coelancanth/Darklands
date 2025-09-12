# DDD Feature Implementation Protocol

## 🎯 Quick Decision Tree for New Features

### Step 1: Which Bounded Context?
Ask: "What is the PRIMARY concern of this feature?"

```
Combat damage calculation → Tactical
Performance monitoring → Diagnostics  
Sound effects → Platform
Save/Load → Platform (not Tactical!)
```

**Rule**: If unsure, it goes in Tactical (the core game).

### Step 2: Is it a Vertical Slice or Shared?

```
Vertical Slice (80% of features):
├── Has UI component → YES
├── Complete user action → YES
├── Can ship independently → YES
└── Example: "Execute Attack", "Move Actor"

Shared Domain (20% of features):
├── Multiple slices need it → YES
├── Core entity/aggregate → YES
├── Business rules used everywhere → YES
└── Example: Actor aggregate, Position value object
```

### Step 3: Where Does Code Go?

```
Tactical/
├── Features/              # Vertical Slices go here
│   └── Attack/
│       ├── Domain/
│       │   └── Rules/    # Slice-specific rules
│       ├── Application/
│       │   └── ExecuteAttackCommandHandler.cs
│       └── Infrastructure/
│
├── Domain/
│   ├── Actors/           # Shared aggregates (PLURAL folders)
│   │   └── Actor.cs      # Singular class names
│   ├── Grids/            # Avoids Grid.Grid collision
│   │   └── Grid.cs
│   └── ValueObjects/     # Shared value objects
│       └── Position.cs
│
└── Contracts/            # Public API ONLY
    └── Events/
        └── ActorDamagedContractEvent.cs
```

## 📋 Implementation Checklist

### For Product Owner (Defining the Feature)
- [ ] Identify bounded context (Tactical/Diagnostics/Platform)
- [ ] Write VS item with acceptance criteria
- [ ] Confirm it's a complete vertical slice

### For Tech Lead (Breaking it Down)
- [ ] Determine if vertical slice or shared domain
- [ ] Identify which aggregates are involved
- [ ] Decide: domain event or contract event?
- [ ] Create folder structure if new slice
- [ ] Use plural folder names for aggregates (Actors/, not Actor/)

### For Dev Engineer (Implementing)
- [ ] Create in Features/ folder if vertical slice
- [ ] Use existing aggregates from Domain/Aggregates
- [ ] Domain events for internal, Integration events for cross-context
- [ ] All handlers in Application layer

### For Test Specialist (Verifying)
- [ ] Unit tests in slice folder
- [ ] Integration tests for cross-context events
- [ ] Architecture tests still pass

## 🔄 Event Decision Matrix

| Scenario | Event Type | Assembly | Handler Location |
|----------|-----------|----------|------------------|
| Attack damages actor (same context) | `ActorDamagedEvent` (IDomainEvent) | Tactical.Domain | Tactical.Application |
| Attack needs monitoring (cross-context) | `ActorDamagedIntegrationEvent` (IIntegrationEvent) | Tactical.IntegrationEvents | Diagnostics.Application |
| UI needs update | Use app notification via EventBus | N/A | Presenter |

## ⚡ Quick Examples

### Example 1: New "Heal" Feature
1. **Context**: Tactical (game mechanics)
2. **Type**: Vertical Slice (complete feature)
3. **Structure**:
```
Tactical/Features/Heal/
├── Domain/Rules/
│   └── CannotHealDeadActorRule.cs
├── Application/
│   ├── HealActorCommand.cs
│   └── HealActorCommandHandler.cs
└── Infrastructure/
    └── HealingEffectService.cs
```
4. **Namespace**: `Darklands.Tactical.Features.Heal.Application`
4. **Events**: 
   - `ActorHealedEvent` (domain) 
   - `ActorHealedIntegrationEvent` (if monitoring needed)

### Example 2: New "Critical Hit" Mechanic
1. **Context**: Tactical
2. **Type**: Shared Domain (multiple slices use it)
3. **Structure**:
```
Tactical/Domain/
├── Combat/                    # Plural folder for Combat aggregate
│   ├── CriticalHitResult.cs  # Value object in aggregate
│   └── ICriticalHitCalculator.cs # Service interface
```
4. **Namespace**: `Darklands.Tactical.Domain.Combat`
4. **Used by**: Attack slice, Spell slice, etc.

## 🚫 Anti-Patterns to Avoid

1. **Cross-Context Direct References**
   ```csharp
   // ❌ WRONG - Diagnostics referencing Tactical domain
   using Darklands.Tactical.Domain.Actors;
   
   // ✅ RIGHT - Use integration events
   using Darklands.Tactical.IntegrationEvents;
   ```

2. **Generic Integration Events**
   ```csharp
   // ❌ WRONG - Too generic
   public record SomethingHappenedEvent(object Data);
   
   // ✅ RIGHT - Specific and meaningful
   public record ActorDamagedIntegrationEvent(Guid ActorId, int Damage);
   ```

3. **Vertical Slice Using Another Slice's Internals**
   ```csharp
   // ❌ WRONG - Movement slice using Attack's domain
   using Darklands.Tactical.Features.Attack.Domain;
   
   // ✅ RIGHT - Use shared domain
   using Darklands.Tactical.Domain.Aggregates;
   ```

## 📝 Namespace Conventions

### Aggregate Folder Naming (CRITICAL)
- **Always use PLURAL** for aggregate folders
- **Always use SINGULAR** for class names
- **Inspired by**: modular-monolith-with-ddd pattern

```csharp
// ❌ WRONG - Creates collision
namespace Domain.Actor { public class Actor } // Actor.Actor is ugly

// ✅ RIGHT - No collision  
namespace Domain.Actors { public class Actor } // Actors.Actor is clean
```

### Examples
| Aggregate | Folder Name | Class Name | Full Reference |
|-----------|------------|------------|----------------|
| Actor | `Actors/` | `Actor.cs` | `Actors.Actor` |
| Grid | `Grids/` | `Grid.cs` | `Grids.Grid` |
| Combat | `Combat/` | `CombatSession.cs` | `Combat.CombatSession` |

## 🎓 Key Principles

1. **Bounded Contexts are HARD boundaries** - No exceptions
2. **Vertical Slices are SOFT boundaries** - Can share within context
3. **Contract Events are PUBLIC APIs** - Version them carefully
4. **Domain Events are INTERNAL** - Change freely
5. **Plural folders, singular classes** - Avoids namespace collisions
6. **When in doubt, start as Vertical Slice** - Refactor to shared later

## 📊 Decision Summary Card

```
New Feature Arrives
    ↓
Q1: Primary Concern? → [Tactical|Diagnostics|Platform]
    ↓
Q2: Complete Feature? → [YES: Vertical Slice|NO: Shared Domain]
    ↓
Q3: Crosses Contexts? → [YES: Integration Event|NO: Domain Event]
    ↓
Implement in: Context/Features/FeatureName/
```

---

*"Make the right thing easy, make the wrong thing hard."* - Architecture Principle