# ADR-017 Enhancements from Modular Monolith with DDD Analysis

## Executive Summary
After deep analysis of the `modular-monolith-with-ddd` reference implementation, I've identified 12 critical architectural patterns that would significantly strengthen our ADR-017 DDD Bounded Contexts design.

## 🎯 Critical Enhancements for ADR-017

### 1. IntegrationEvents as Separate Assembly Per Module
**Current ADR-017**: Integration events in SharedKernel
**Enhancement**: Each module publishes its own `IntegrationEvents` assembly

```
Tactical/
├── Darklands.Tactical.Domain.csproj
├── Darklands.Tactical.Application.csproj  
├── Darklands.Tactical.Infrastructure.csproj
└── Darklands.Tactical.IntegrationEvents.csproj  # NEW - Public contract

Diagnostics/
├── Darklands.Diagnostics.Domain.csproj
├── Darklands.Diagnostics.Application.csproj
├── Darklands.Diagnostics.Infrastructure.csproj  
└── Darklands.Diagnostics.IntegrationEvents.csproj  # NEW - Public contract
```

**Key Insight**: Modules ONLY reference each other's IntegrationEvents assemblies:
```xml
<!-- Diagnostics.Application.csproj -->
<ItemGroup>
  <!-- Can subscribe to Tactical events WITHOUT coupling to Tactical internals -->
  <ProjectReference Include="..\..\Tactical\IntegrationEvents\Darklands.Tactical.IntegrationEvents.csproj" />
</ItemGroup>
```

### 2. Transactional Outbox/Inbox Pattern
**Current ADR-017**: Direct event publishing
**Enhancement**: Persist events for reliable processing

```csharp
// SharedKernel/Infrastructure/Inbox/InboxMessage.cs
public class InboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; }       // Event type name
    public string Data { get; set; }       // Serialized event
    public DateTime? ProcessedDate { get; set; }  // NULL = unprocessed
}

// SharedKernel/Infrastructure/InternalCommands/InternalCommand.cs
public class InternalCommand  
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Data { get; set; }
    public DateTime? ProcessedDate { get; set; }
}
```

**Benefits**:
- Events survive crashes/restarts
- Guaranteed at-least-once delivery
- Ability to replay failed events
- Audit trail of all cross-context communication

### 3. Aggregate-Based Domain Organization
**Current ADR-017**: Feature-based folders
**Enhancement**: Aggregate-root folders with complete encapsulation

```
Tactical/Domain/
├── Actors/                    # Actor Aggregate
│   ├── Actor.cs              # Aggregate root
│   ├── ActorId.cs            # Strongly-typed ID
│   ├── IActorRepository.cs   # Repository interface
│   ├── Events/               # Domain events
│   │   ├── ActorDamagedEvent.cs
│   │   └── ActorMovedEvent.cs
│   └── Rules/                # Business rules
│       ├── ActorMustBeAliveRule.cs
│       └── ActorCannotMoveToOccupiedTileRule.cs
├── Combat/                    # Combat Aggregate
│   ├── CombatSession.cs
│   ├── CombatSessionId.cs
│   ├── ICombatRepository.cs
│   └── ...
```

### 4. Strongly-Typed IDs with Base Class
**Current ADR-017**: Simple EntityId in SharedKernel
**Enhancement**: TypedIdValueBase for all IDs

```csharp
// SharedKernel/Domain/TypedIdValueBase.cs
public abstract class TypedIdValueBase : IEquatable<TypedIdValueBase>
{
    public Guid Value { get; }
    
    protected TypedIdValueBase(Guid value)
    {
        if (value == Guid.Empty) 
            throw new InvalidOperationException("Id value cannot be empty");
        Value = value;
    }
    
    public override bool Equals(object obj) => // implementation
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(TypedIdValueBase a, TypedIdValueBase b) => // implementation
    public override string ToString() => Value.ToString("N")[..8];
}

// Tactical/Domain/Actors/ActorId.cs
public sealed class ActorId : TypedIdValueBase
{
    public ActorId(Guid value) : base(value) { }
    public static ActorId Create() => new(Guid.NewGuid());
}
```

### 5. Business Rule Pattern with Interface
**Current ADR-017**: Not specified
**Enhancement**: IBusinessRule pattern for all domain validations

```csharp
// SharedKernel/Domain/IBusinessRule.cs
public interface IBusinessRule
{
    bool IsBroken();
    string Message { get; }
}

// SharedKernel/Domain/Entity.cs
public abstract class Entity
{
    protected void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleValidationException(rule);
        }
    }
}

// Usage in Aggregate
public void Move(Position newPosition)
{
    CheckRule(new ActorMustBeAliveRule(_health));
    CheckRule(new ActorCannotMoveToOccupiedTileRule(newPosition, _gridService));
    
    var oldPosition = _position;
    _position = newPosition;
    
    AddDomainEvent(new ActorMovedEvent(Id, oldPosition, newPosition));
}
```

### 6. Module-Level Architecture Tests
**Current ADR-017**: Assembly-level tests only
**Enhancement**: Module isolation tests with smart exclusions

```csharp
[Test]
public void TacticalModule_DoesNotHaveDependency_On_OtherModules()
{
    var otherModules = new[] { "Darklands.Diagnostics", "Darklands.Platform" };
    
    var tacticalAssemblies = new[]
    {
        typeof(Darklands.Tactical.Domain.TacticalMarker).Assembly,
        typeof(Darklands.Tactical.Application.TacticalMarker).Assembly,
        typeof(Darklands.Tactical.Infrastructure.TacticalMarker).Assembly
    };
    
    var result = Types.InAssemblies(tacticalAssemblies)
        .That()
        // CRITICAL: Exclude integration event handlers from isolation check
        .DoNotImplementInterface(typeof(INotificationHandler<>))
        .And().DoNotHaveNameEndingWith("IntegrationEventHandler")
        .And().DoNotHaveName("EventsBusStartup")
        .Should()
        .NotHaveDependencyOnAny(otherModules)
        .GetResult();
        
    result.IsSuccessful.Should().BeTrue();
}
```

### 7. Layered BuildingBlocks/SharedKernel
**Current ADR-017**: Flat SharedKernel
**Enhancement**: Layered SharedKernel matching Clean Architecture

```
SharedKernel/
├── Domain/
│   ├── Entity.cs
│   ├── ValueObject.cs
│   ├── IAggregateRoot.cs
│   ├── IBusinessRule.cs
│   ├── IDomainEvent.cs
│   └── TypedIdValueBase.cs
├── Application/
│   ├── ICommand.cs
│   ├── IQuery.cs
│   └── IEventHandler.cs
└── Infrastructure/
    ├── EventBus/
    │   ├── IIntegrationEvent.cs
    │   └── IIntegrationEventBus.cs
    ├── Inbox/
    └── InternalCommands/
```

### 8. Domain Events Collection in Entity Base
**Current ADR-017**: Not specified
**Enhancement**: Standard pattern for domain event collection

```csharp
public abstract class Entity
{
    private List<IDomainEvent> _domainEvents;
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= new List<IDomainEvent>();
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }
}
```

### 9. Integration Event Versioning Strategy
**Current ADR-017**: Basic versioning mentioned
**Enhancement**: Comprehensive versioning with mapping

```csharp
public abstract class IntegrationEvent : INotification
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public int Version { get; }  // Schema version
    
    protected IntegrationEvent(Guid id, DateTime occurredOn, int version = 1)
    {
        Id = id;
        OccurredOn = occurredOn;
        Version = version;
    }
}

// Event mapper for version handling
public interface IIntegrationEventMapper
{
    IIntegrationEvent Map(string eventType, string eventData, int version);
}
```

### 10. Module Public Interface Pattern
**Current ADR-017**: Not specified
**Enhancement**: Each module exposes a public interface

```csharp
// Tactical/Application/Contracts/ITacticalModule.cs
public interface ITacticalModule
{
    Task<Guid> ExecuteCommandAsync(ICommand command);
    Task<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> query);
}

// Implementation hidden in Infrastructure
internal class TacticalModule : ITacticalModule
{
    private readonly IMediator _mediator;
    // Implementation details...
}
```

### 11. Repository Per Aggregate Pattern
**Current ADR-017**: Generic repositories
**Enhancement**: Specific repository per aggregate root

```csharp
// Domain/Actors/IActorRepository.cs
public interface IActorRepository
{
    Task<Actor> GetByIdAsync(ActorId id);
    Task AddAsync(Actor actor);
    Task UpdateAsync(Actor actor);  
    Task RemoveAsync(Actor actor);
    // No query methods - use separate read models
}
```

### 12. Separate Test Projects Structure
**Current ADR-017**: Single test project
**Enhancement**: Test segregation by type

```
tests/
├── Tactical/
│   ├── Darklands.Tactical.Domain.UnitTests/
│   ├── Darklands.Tactical.Application.UnitTests/
│   ├── Darklands.Tactical.IntegrationTests/
│   └── Darklands.Tactical.ArchTests/
├── Diagnostics/
│   └── ... (same structure)
└── SystemTests/
    └── Darklands.SystemTests/  # Cross-module tests
```

## 🚀 Implementation Priority

### Phase 1: Foundation (MUST HAVE)
1. **IntegrationEvents assemblies** - Enable true module decoupling
2. **Module isolation tests** - Enforce boundaries from day one
3. **Strongly-typed IDs** - Type safety across boundaries

### Phase 2: Reliability (SHOULD HAVE)
4. **Inbox/Outbox pattern** - Reliable event processing
5. **Domain events in Entity** - Standard event handling
6. **Business rule pattern** - Consistent validation

### Phase 3: Organization (NICE TO HAVE)  
7. **Aggregate-based folders** - Better domain organization
8. **Repository per aggregate** - Clear boundaries
9. **Layered SharedKernel** - Better separation

## 💡 Key Architectural Insights

### The "Integration Events Assembly" Pattern
This is THE key pattern that makes everything work:
- Modules can subscribe to each other's events
- But can't access internal implementation
- Provides compile-time safety
- Natural anti-corruption layer

### The "Selective Test Exclusion" Pattern
Architecture tests MUST exclude:
- `IntegrationEventHandler` classes
- Classes implementing `INotificationHandler<IntegrationEvent>`
- Event bus startup/configuration classes

This allows event handlers while maintaining module isolation.

### The "ProcessedDate" Pattern
Both InboxMessage and InternalCommand use `DateTime? ProcessedDate`:
- NULL = needs processing
- Non-NULL = already processed
- Simple, reliable, database-friendly

## 📝 Recommended ADR-017 Updates

### 1. Update Assembly Structure Section
Add IntegrationEvents assembly to each context

### 2. Add Reliability Section
Document Inbox/Outbox pattern for event reliability

### 3. Enhance Testing Section
Add module isolation tests with exclusions

### 4. Update Event Bus Section
Show how modules reference only IntegrationEvents assemblies

### 5. Add Aggregate Organization Section
Document aggregate-based folder structure

## 🎯 Conclusion

The modular-monolith-with-ddd project demonstrates battle-tested patterns for TRUE module isolation while maintaining pragmatic communication. The key innovation is the **IntegrationEvents assembly pattern** - this single change would dramatically improve our architecture's maintainability and evolvability.

**Recommendation**: Update ADR-017 to incorporate at minimum the Phase 1 patterns, as they provide the foundation for everything else.