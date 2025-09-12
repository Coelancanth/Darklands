# DDD Bounded Contexts: Lessons from Modular Monolith Analysis

**Date**: 2025-09-12  
**Author**: Tech Lead  
**Source**: Analysis of `modular-monolith-with-ddd` reference implementation  
**Impact**: Critical architectural patterns for ADR-017 enhancement

## 🎯 Executive Summary

Through deep analysis of a production-proven modular monolith implementation, I discovered 12 architectural patterns that solve the fundamental challenge of DDD: **How to achieve true module isolation while enabling necessary communication.**

The breakthrough insight: **IntegrationEvents assemblies as public contracts** - modules can subscribe to events without accessing internals.

## 📚 Core Learning: The Module Boundary Problem

### The Challenge
In DDD, bounded contexts must be isolated yet communicate. Traditional approaches fail because:
- Direct references break isolation
- Shared libraries create coupling  
- Event buses lose type safety
- Interfaces still expose internals

### The Solution: IntegrationEvents Assembly Pattern
Each module publishes a separate assembly containing ONLY its integration events:

```
Module Structure:
├── Module.Domain.csproj           ← Internal (hidden)
├── Module.Application.csproj      ← Internal (hidden)  
├── Module.Infrastructure.csproj   ← Internal (hidden)
└── Module.IntegrationEvents.csproj ← PUBLIC (exposed)
```

Other modules reference ONLY the IntegrationEvents assembly, achieving:
- ✅ Complete internal isolation
- ✅ Type-safe event contracts
- ✅ Compile-time boundary enforcement
- ✅ Natural anti-corruption layer

## 🔍 Key Patterns Discovered

### 1. The IntegrationEvents Assembly Pattern

**Learning**: Separation of public contracts from implementation is THE key to modular monoliths.

```xml
<!-- Diagnostics.Application.csproj -->
<ItemGroup>
  <!-- Can subscribe to Tactical events WITHOUT seeing Tactical internals -->
  <ProjectReference Include="..\..\Tactical\IntegrationEvents\Tactical.IntegrationEvents.csproj" />
  <!-- But CANNOT reference Tactical.Domain, Application, or Infrastructure -->
</ItemGroup>
```

**Why It Works**:
- Events are the ONLY public API between modules
- Implementation details remain completely hidden
- Changes to internals don't affect other modules
- Natural versioning boundary

### 2. Smart Architecture Test Exclusions

**Learning**: Architecture tests must be pragmatic, not dogmatic.

```csharp
[Test]
public void Module_Should_Not_Reference_Other_Modules()
{
    var result = Types.InAssemblies(moduleAssemblies)
        .That()
        // CRITICAL: Exclude integration event handlers!
        .DoNotImplementInterface(typeof(INotificationHandler<>))
        .And().DoNotHaveNameEndingWith("IntegrationEventHandler")
        .Should()
        .NotHaveDependencyOnAny(otherModules)
        .GetResult();
}
```

**Key Insight**: Event handlers MUST be allowed to reference other modules' events, but nothing else.

### 3. Transactional Outbox/Inbox Pattern

**Learning**: Reliable event processing requires persistence, not just in-memory buses.

```csharp
public class InboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; }        // Event type name
    public string Data { get; set; }        // Serialized event JSON
    public DateTime? ProcessedDate { get; set; }  // NULL = unprocessed
}
```

**Benefits Discovered**:
- Events survive application crashes
- Natural retry mechanism for failures
- Audit trail of all cross-context communication
- Can replay events for debugging
- Simple database-backed reliability

### 4. Aggregate-Based Domain Organization

**Learning**: Organizing by aggregates (not features) creates natural boundaries.

```
Domain/
├── Actors/                    # Actor Aggregate (complete unit)
│   ├── Actor.cs              # Aggregate root
│   ├── ActorId.cs            # Strongly-typed ID
│   ├── IActorRepository.cs   # Repository interface
│   ├── Events/               # Domain events (internal)
│   │   ├── ActorDamagedEvent.cs
│   │   └── ActorMovedEvent.cs
│   └── Rules/                # Business rules (cohesive)
│       ├── ActorMustBeAliveRule.cs
│       └── ActorCannotMoveToOccupiedTileRule.cs
```

**Why This Works Better Than Feature Folders**:
- Aggregate is the transaction boundary
- All related code in one place
- Clear ownership and responsibilities
- Natural unit for testing

### 5. Business Rule Pattern

**Learning**: Explicit business rules make domain logic testable and reusable.

```csharp
public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}

// In aggregate method
public void Move(Position newPosition)
{
    CheckRule(new ActorMustBeAliveRule(_health));
    CheckRule(new PositionMustBeEmptyRule(newPosition, _grid));
    
    // If we get here, all rules passed
    _position = newPosition;
    AddDomainEvent(new ActorMovedEvent(Id, newPosition));
}
```

**Benefits**:
- Rules are explicit and named
- Easy to test in isolation
- Reusable across methods
- Self-documenting code

### 6. Domain Events Collection in Entity Base

**Learning**: Standardizing event collection prevents inconsistency.

```csharp
public abstract class Entity
{
    private List<IDomainEvent> _domainEvents;
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => 
        _domainEvents?.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= new List<IDomainEvent>();
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents() => _domainEvents?.Clear();
}
```

**Key Pattern**: Events are collected during operations, then dispatched after persistence.

### 7. Strongly-Typed IDs with Base Class

**Learning**: Type safety at boundaries prevents entire categories of bugs.

```csharp
public abstract class TypedIdValueBase : IEquatable<TypedIdValueBase>
{
    public Guid Value { get; }
    
    protected TypedIdValueBase(Guid value)
    {
        if (value == Guid.Empty) 
            throw new InvalidOperationException("Id cannot be empty");
        Value = value;
    }
}

// Can't accidentally pass ActorId where CombatId expected!
public sealed class ActorId : TypedIdValueBase { }
public sealed class CombatId : TypedIdValueBase { }
```

### 8. Repository Per Aggregate Pattern

**Learning**: Generic repositories hide important domain operations.

```csharp
// NOT this:
public interface IRepository<T> 
{
    Task<T> GetByIdAsync(Guid id);
    Task SaveAsync(T entity);
}

// But THIS:
public interface IActorRepository
{
    Task<Actor> GetByIdAsync(ActorId id);
    Task<Actor> GetByNameAsync(string name);  // Domain-specific query
    Task AddAsync(Actor actor);
    Task UpdateHealthAsync(ActorId id, int health);  // Specific operation
}
```

## 💡 Critical Insights

### Insight 1: Events as the ONLY Public API
The most profound learning: **modules should communicate ONLY through events**, never through direct service calls or shared interfaces. This creates:
- Natural temporal decoupling
- Clear contract boundaries
- Evolutionary architecture

### Insight 2: The ProcessedDate Pattern
Using `DateTime? ProcessedDate` for event tracking is brilliantly simple:
- NULL = needs processing
- Non-NULL = already processed  
- No complex state machines
- Database-friendly
- Query-friendly (`WHERE ProcessedDate IS NULL`)

### Insight 3: Integration Events Are NOT Domain Events
Critical distinction discovered:
- **Domain Events**: Internal to module, rich objects, can change freely
- **Integration Events**: Public contracts, primitive types only, versioned, stable

### Insight 4: Module Tests Need Exclusions
Pure isolation is impossible - event handlers MUST reference other modules' events. The solution: explicitly exclude event handlers from isolation tests while enforcing everything else.

## 🚀 Practical Applications

### For Our Codebase (Darklands)

1. **Immediate Win**: Add IntegrationEvents assemblies
   - 2-3 hours of work
   - Instant boundary enforcement
   - Foundation for everything else

2. **Next Step**: Implement Outbox/Inbox
   - Reliable event processing
   - Survive crashes and restarts
   - Enable event replay

3. **Refactoring Target**: Aggregate-based organization
   - Move from features to aggregates
   - Create natural boundaries
   - Improve cohesion

### Anti-Patterns to Avoid

❌ **Shared Domain Models** - Each context needs its own models  
❌ **Synchronous Module Calls** - Use events for all cross-module communication  
❌ **Generic Events** - Events should be specific and meaningful  
❌ **Circular Dependencies** - Even through events (use correlation IDs instead)

## 📊 Complexity Analysis

| Pattern | Implementation Complexity | Value Delivered | ROI |
|---------|-------------------------|-----------------|-----|
| IntegrationEvents Assemblies | Low (2-3 hours) | Very High | ⭐⭐⭐⭐⭐ |
| Module Isolation Tests | Low (1-2 hours) | High | ⭐⭐⭐⭐⭐ |
| Outbox/Inbox | Medium (1 day) | Very High | ⭐⭐⭐⭐ |
| Business Rules | Low (2-3 hours) | High | ⭐⭐⭐⭐ |
| Aggregate Organization | High (2-3 days) | Medium | ⭐⭐⭐ |

## 🎓 Learning Methodology

### How This Analysis Was Conducted

1. **Structural Analysis**: Explored project structure to understand organization
2. **Dependency Analysis**: Examined .csproj files to understand references
3. **Pattern Recognition**: Identified recurring patterns across modules
4. **Test Analysis**: Studied architecture tests to understand enforcement
5. **Code Deep Dive**: Examined implementation details of key patterns

### What Made This Effective

- **Reference Implementation**: Real production code, not theoretical
- **Multiple Modules**: Could see patterns across different contexts
- **Test Coverage**: Architecture tests revealed enforcement strategies
- **Building Blocks**: Shared kernel showed abstraction patterns

## 📝 Recommendations

### Must Have (Do This Week)
1. Create IntegrationEvents assemblies for each context
2. Add module isolation tests with smart exclusions
3. Implement TypedIdValueBase for all IDs

### Should Have (Do This Month)
4. Implement Outbox/Inbox pattern
5. Add Business Rule pattern
6. Standardize domain events in Entity base

### Nice to Have (Do This Quarter)
7. Refactor to aggregate-based organization
8. Create repository per aggregate
9. Layer the SharedKernel properly

## 🔮 Future Research Topics

Based on this analysis, areas for further investigation:

1. **Event Versioning Strategies** - How to evolve integration events
2. **Saga/Process Managers** - Coordinating across modules
3. **Read Model Projections** - CQRS with event sourcing
4. **Module Deployment** - Can modules be deployed independently?
5. **Testing Strategies** - Integration tests across module boundaries

## 📖 References

- **Source Project**: `modular-monolith-with-ddd` by Kamil Grzybek
- **Key Files Analyzed**:
  - Module .csproj files showing reference patterns
  - LayersTests.cs showing architecture enforcement
  - ModuleTests.cs showing isolation testing
  - IntegrationEvent.cs showing event patterns
  - Entity.cs showing domain event collection
- **Related ADRs**: ADR-017 (DDD Bounded Contexts Architecture)

## ✅ Conclusion

The modular-monolith-with-ddd project demonstrates that **true module isolation IS achievable** in a monolith through careful architectural patterns. The key breakthrough is the **IntegrationEvents assembly pattern** - a simple yet powerful approach that provides compile-time boundary enforcement while maintaining type safety.

For Darklands, implementing even just the top 3 patterns would dramatically improve our architecture's maintainability and evolvability. The ROI is exceptional - a few hours of work for years of architectural benefits.

---

*"The best architectures are discovered, not designed. This analysis discovered patterns proven in production."* - Tech Lead
## 🎯 CRITICAL UPDATE: Namespace Collision Solution

**Date**: 2025-09-12
**Discovery**: modular-monolith-with-ddd uses pluralization strategy for namespace collisions

**The Problem We Had**:
-  and  create verbose, confusing references
- Planned complex reorganization with new bounded contexts (4h work)

**The Simple Solution**:
- Use **plural** folder names: ,  
- Keep **singular** class names: , 
- Result:  and  - clean and clear!

**Impact**:
- TD_032 reduced from 4h to 2h work
- No complex reorganization needed
- Follows industry convention
- Much simpler to implement and understand

**Key Insight**: Sometimes the best solution is the simplest one. The modular-monolith project showed us that pluralization elegantly solves namespace collisions without architectural complexity.

---
