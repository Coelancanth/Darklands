# Post-Mortem: TD_061 Presenter-Handler Architectural Violation

**Date**: 2025-09-19
**Author**: Tech Lead
**Severity**: High
**Time Lost**: 12+ hours
**Time Should Have Taken**: 4 hours

## Executive Summary

TD_061 (Progressive FOV Updates) took 12+ hours instead of 4 due to a fundamental architectural violation: Presenters were implementing `INotificationHandler<T>`, causing MediatR to create multiple instances and breaking the singleton pattern. The root cause was mixing UI coordination (stateful, singleton) with message handling (stateless, transient) in the same class.

## Timeline

### 2025-09-17 20:35
- **Event**: TD_061 proposed by Dev Engineer
- **Decision**: Approved "hybrid" approach with instant domain updates + progressive visual
- **Mistake**: Didn't recognize this violates domain truthfulness principle

### 2025-09-17 - 2025-09-18
- **Event**: Phase 1-3 implementation (Domain, Application, Infrastructure)
- **Decision**: Created complex timer infrastructure (GameTimeService, MovementTimer, MovementProgressionService)
- **Mistake**: Over-engineered solution for simple problem

### 2025-09-19 01:00
- **Event**: Phase 4 implementation (Presentation)
- **Decision**: Made GridPresenter implement `INotificationHandler<RevealPositionAdvancedNotification>`
- **Mistake**: Violated Clean Architecture - presenters shouldn't handle domain events

### 2025-09-19 01:50
- **Event**: Runtime testing reveals BR_022
- **Symptom**: MediatR creating new GridPresenter instances, View is null
- **Reaction**: Assumed it was a DI/MediatR bug

### 2025-09-19 03:16
- **Event**: Tech Lead architectural review
- **Discovery**: Not a bug - architectural violation
- **Resolution**: Created TD_065 for correct approach

## Root Cause Analysis

### Primary Cause: Architectural Confusion
```csharp
// WRONG - What we built
public class GridPresenter : INotificationHandler<DomainEvent>
{
    private IGridView _view;  // Needs state!
    // Presenter has TWO incompatible responsibilities
}
```

### Why This Happened:
1. **Conceptual Error**: Thought presenters should react to domain events directly
2. **Layer Violation**: Mixed Application layer concerns (handlers) with Presentation layer (presenters)
3. **MediatR Misunderstanding**: Didn't realize MediatR registers handlers as transient by default

### Technical Details:
```csharp
// Registration conflict:
services.AddSingleton<GridPresenter>();  // For MVP pattern
services.AddMediatR(...);  // Re-registers as: AddTransient<INotificationHandler<T>, GridPresenter>()

// Result:
GetService<GridPresenter>() // Returns singleton
GetService<INotificationHandler<T>>() // Creates NEW instance!
```

## Impact

### Quantitative:
- **Time Wasted**: 12+ hours vs 4 hour estimate
- **Code Written**: ~500 lines of unnecessary timer infrastructure
- **Code Deleted**: Will delete ~400 lines when fixing
- **Tests Written**: 40+ tests for wrong approach

### Qualitative:
- **Architectural Debt**: Complex timer services that shouldn't exist
- **Conceptual Confusion**: "Hybrid" domain model that lies about state
- **Team Impact**: Dev Engineer frustrated by "mysterious" DI issues
- **Knowledge Gap**: Revealed misunderstanding of MediatR patterns

## Lessons Learned

### 1. Domain Models Must Tell Truth
- **Wrong**: Domain instantly teleports, services fake progression
- **Right**: Domain moves step-by-step, events reflect reality

### 2. Presenters Are Not Handlers
- **Wrong**: `Presenter : INotificationHandler<T>`
- **Right**: Handlers in Application layer, Presenters use UIEventBus

### 3. MediatR Registration Order Matters
```csharp
// If you need singleton handlers (rare):
services.AddSingleton<INotificationHandler<T>, MyHandler>();
services.AddMediatR(...);  // Add MediatR LAST
```

### 4. Complex Solutions Signal Wrong Approach
- When implementation feels complex, we're usually solving wrong problem
- Step back and question the approach, not debug harder

## What Went Well

1. **Testing Caught Issue**: Unit tests passed but runtime revealed problem
2. **Good Logging**: Instance hash logging made issue visible
3. **Documentation**: Dev Engineer created thorough analysis document
4. **Review Process**: Tech Lead review identified architectural violation

## What Went Poorly

1. **Initial Design Review**: Approved overcomplicated approach
2. **Incremental Complexity**: Each phase added more complexity instead of questioning approach
3. **Debugging Focus**: Spent hours debugging symptom instead of questioning architecture
4. **Late Architecture Review**: Should have reviewed after Phase 2 complexity

## Action Items

### Immediate (TD_065):
- [x] Revert to commit before TD_061
- [ ] Implement correct domain step-by-step movement
- [ ] Use Application layer handlers only
- [ ] Connect presenters via UIEventBus

### Short-term:
- [ ] TD_066: Add NetArchTest boundary enforcement
- [ ] TD_067: Document MediatR registration patterns
- [ ] TD_068: Standardize DI registration order
- [ ] TD_069: Update persona protocols with ADR compliance

### Long-term:
- [ ] Architecture review after Phase 2 of any complex feature
- [ ] Create architectural decision flowchart
- [ ] Regular architecture workshops for team

## Prevention Strategies

### 1. Architectural Tests (TD_066)
```csharp
[Test]
public void Presenters_Must_Not_Implement_INotificationHandler()
{
    Types.InNamespace("*.Presenters")
        .Should().NotImplementInterface(typeof(INotificationHandler<>))
        .GetResult().IsSuccessful.Should().BeTrue();
}
```

### 2. Registration Patterns (TD_067)
- Document correct MediatR registration order
- Create helper methods for singleton handlers
- Standardize service registration structure

### 3. Clear Separation Patterns
```
Application Layer:
├── Commands/
├── Handlers/     ← ALL INotificationHandler<T> here
└── Services/

Presentation Layer:
├── Presenters/   ← NO handlers, UIEventBus only
└── Views/
```

### 4. Protocol Updates (TD_069)
- Update all persona docs with ADR compliance checks
- Add "Architecture Smell" detection guidelines
- Mandate Phase 2 review for complex features

## Corrected Architecture

```csharp
// Domain - Truth
public class Actor {
    public void AdvanceMovement() {
        Position = _path.GetNext();  // Actually moves
        RaiseDomainEvent(new ActorMovedEvent(Position));
    }
}

// Application - Handler
public class ActorMovedHandler : INotificationHandler<ActorMovedEvent> {
    public Task Handle(ActorMovedEvent evt) {
        var fov = _calculator.Calculate(evt.Position);
        _uiEventBus.Publish(new UpdateFOVUIEvent(fov));
        return Task.CompletedTask;
    }
}

// Presentation - Presenter
public class GridPresenter {  // NO handler interface!
    public GridPresenter(IUIEventBus bus) {
        bus.Subscribe<UpdateFOVUIEvent>(OnFOVUpdate);
    }
}
```

## Conclusion

This incident revealed a fundamental misunderstanding of Clean Architecture boundaries and MediatR patterns. The 12+ hours spent fighting symptoms could have been 4 hours implementing the correct approach. The key lesson: **When a presenter needs to react to domain events, use an Application layer handler to bridge to UIEventBus, never make the presenter a handler directly.**

## Sign-off

**Tech Lead**: This architectural violation was my responsibility to catch during initial review. The lessons learned here will be codified in tests and protocols to prevent recurrence.

**Severity**: High (architectural violation, not just a bug)
**Prevention Priority**: Critical (could affect many features)
**Knowledge Transfer**: Required reading for all personas

---

*"The best bugs to fix are the ones that reveal architectural misunderstandings."*