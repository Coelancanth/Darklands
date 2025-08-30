# ADR-002: Phased Implementation Protocol

**Status**: Approved  
**Date**: 2025-08-28  
**Decision Makers**: Tech Lead (adopted from proven ADR-006)

## Context

Previous experience (ADR-006) has proven that building features UI-first or with mixed concerns leads to:
- Integration bugs masking domain logic errors
- Complex mocking required for testing
- Risky refactoring when UI and logic are intertwined
- Technical debt from premature integration decisions
- Debugging requiring full Godot runtime

Darklands needs the same disciplined approach to ensure consistent quality.

## Decision

We adopt the proven **Model-First Implementation Protocol** where ALL features are built in strict phases, starting with pure C# domain models and expanding outward through architectural layers.

### Mandatory Implementation Phases

#### Phase 1: Pure Domain Model (Zero Dependencies)
- Define domain entities and value objects
- Implement business rules as pure functions
- Write comprehensive unit tests
- **GATE**: 100% unit tests passing, >80% coverage
- **Time**: <100ms test execution

#### Phase 2: Application Layer (Commands/Handlers)
- Create CQRS commands and queries
- Implement handlers with Fin<T> error handling
- Write handler unit tests with mocked repositories
- **GATE**: All handler tests passing
- **Time**: <500ms test execution

#### Phase 3: Infrastructure Layer (State/Services)
- Implement state services and repositories
- Add integration tests
- Verify data flow in isolation
- **GATE**: Integration tests passing
- **Time**: <2s test execution

#### Phase 4: Presentation Layer (Godot/UI)
- Create presenter contracts (interfaces)
- Implement MVP pattern
- Wire Godot nodes and signals
- **GATE**: Manual testing in editor works
- **Time**: Variable (manual)

### Phase Transition Rules

1. **HARD GATE**: Cannot proceed to next phase until current phase tests are GREEN
2. **NO SHORTCUTS**: Even "simple" features follow all phases
3. **DOCUMENTATION**: Each phase completion documented in commit
4. **REVIEW**: Tech Lead validates phase completion before proceeding

### Commit Message Convention

```bash
feat(combat): implement time-unit domain model [Phase 1/4]
feat(combat): add attack command handlers [Phase 2/4]  
feat(combat): integrate combat state service [Phase 3/4]
feat(combat): complete combat UI presentation [Phase 4/4]
```

## Implementation Example: Time-Unit Combat

```csharp
// PHASE 1: Pure Domain Model (Start here)
namespace Darklands.Core.Domain.Combat;

public record TimeUnit(int Value)
{
    public static TimeUnit operator +(TimeUnit a, TimeUnit b) 
        => new(a.Value + b.Value);
    
    public static TimeUnit operator -(TimeUnit a, TimeUnit b)
        => new(Math.Max(0, a.Value - b.Value));
}

public record CombatAction(string Name, TimeUnit Cost, int Damage);

public static class TimeUnitCalculator
{
    public static Fin<TimeUnit> CalculateActionTime(
        CombatAction action, 
        int agility, 
        int encumbrance)
    {
        if (agility <= 0)
            return FinFail<TimeUnit>(Error.New("Invalid agility"));
            
        var baseTime = action.Cost.Value;
        var agilityModifier = 100.0 / agility;
        var encumbranceModifier = 1.0 + (encumbrance * 0.1);
        
        var finalTime = (int)(baseTime * agilityModifier * encumbranceModifier);
        return FinSucc(new TimeUnit(finalTime));
    }
}

// PHASE 1 TEST
[Fact]
public void FastDaggerStab_WithHighAgility_CalculatesCorrectTime()
{
    // Arrange
    var daggerStab = new CombatAction("Stab", new TimeUnit(50), 5);
    
    // Act
    var result = TimeUnitCalculator.CalculateActionTime(daggerStab, 20, 0);
    
    // Assert
    result.Match(
        Succ: time => time.Value.Should().Be(250), // 50 * (100/20) * 1.0
        Fail: e => Assert.Fail($"Should succeed: {e}")
    );
}

// PHASE 2: Application Layer (only after Phase 1 complete)
public record ExecuteCombatActionCommand(
    Guid ActorId,
    Guid TargetId, 
    string ActionName) : IRequest<Fin<CombatResult>>;

public class ExecuteCombatActionHandler 
    : IRequestHandler<ExecuteCombatActionCommand, Fin<CombatResult>>
{
    private readonly ICombatantRepository _combatants;
    private readonly IActionRepository _actions;
    
    public async Task<Fin<CombatResult>> Handle(
        ExecuteCombatActionCommand cmd, 
        CancellationToken ct)
    {
        return await (
            from actor in _combatants.Get(cmd.ActorId)
            from target in _combatants.Get(cmd.TargetId)
            from action in _actions.Get(cmd.ActionName)
            from timeUnits in TimeUnitCalculator.CalculateActionTime(
                action, actor.Agility, actor.Encumbrance)
            let damage = CalculateDamage(action, actor, target)
            from _ in _combatants.ApplyDamage(cmd.TargetId, damage)
            select new CombatResult(timeUnits, damage)
        ).ToAsync();
    }
}

// PHASE 3: Infrastructure (only after Phase 2 complete)
public class CombatStateService : ICombatantRepository
{
    private readonly Dictionary<Guid, Combatant> _combatants = new();
    private readonly ILogger _logger;
    
    public Task<Fin<Combatant>> Get(Guid id)
    {
        _logger.Debug("Getting combatant {Id}", id);
        return Task.FromResult(
            _combatants.TryGetValue(id, out var combatant)
                ? FinSucc(combatant)
                : FinFail<Combatant>(Error.New($"Combatant {id} not found"))
        );
    }
    
    // ... implementation
}

// PHASE 4: Presentation (only after Phase 3 complete)
public class CombatPresenter : PresenterBase<ICombatView>
{
    private readonly IMediator _mediator;
    
    public async void OnActionSelected(string actionName, Vector2 target)
    {
        var result = await _mediator.Send(new ExecuteCombatActionCommand(
            _currentActor.Id,
            GetTargetAt(target),
            actionName
        ));
        
        result.Match(
            Succ: r => {
                View.UpdateTimeDisplay(r.TimeUnits.Value);
                View.ShowDamage(target, r.Damage);
            },
            Fail: e => View.ShowError(e.Message)
        );
    }
}
```

## Testing Requirements Per Phase

| Phase | Test Type | Speed Target | Command |
|-------|-----------|--------------|---------|
| 1. Domain | Unit | <100ms | `dotnet test --filter "Category=Domain"` |
| 2. Application | Unit | <500ms | `dotnet test --filter "Category=Application"` |
| 3. Infrastructure | Integration | <2s | `dotnet test --filter "Category=Integration"` |
| 4. Presentation | Manual/E2E | Variable | Manual in Godot editor |

## Consequences

### Positive
- **Early Bug Detection**: Logic errors caught in milliseconds, not minutes
- **Clear Dependencies**: Each layer only knows about layers below
- **Parallel Development**: Multiple devs can work on different phases
- **Refactoring Safety**: Change domain without touching UI
- **Fast Feedback**: Domain tests run in <100ms

### Negative
- **Initial Slower Delivery**: First implementation takes longer
- **More Files**: Separation requires more classes/interfaces
- **Mental Model Shift**: Requires thinking in layers
- **Upfront Design**: Must think through domain before coding

## References
- Proven ADR-006: Model-First Implementation Protocol
- [Move Block Reference](https://github.com/user/darklands-reference/src/Features/Block/Move/)
- Clean Architecture principles