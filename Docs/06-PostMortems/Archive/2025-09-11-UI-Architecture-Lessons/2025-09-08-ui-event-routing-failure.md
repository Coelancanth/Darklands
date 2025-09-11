# Post-Mortem: Complete UI Event Routing Failure After Static Callback Removal

**Date**: September 8, 2025  
**Incident Duration**: ~45 minutes  
**Severity**: Critical (Complete UI update failure)  
**Reporter**: Dev Engineer  
**Status**: Resolved  

## Executive Summary

A critical architectural refactoring to remove static callback anti-patterns inadvertently broke all UI updates in the combat system. After successfully eliminating static callbacks and replacing them with clean MediatR domain events, the UI completely stopped responding to combat events (damage, death). Players could attack enemies but would see no visual feedback - health bars wouldn't update and dead actors wouldn't disappear from the screen.

The root cause was a subtle MediatR dependency injection lifecycle issue where multiple notification handler instances were being created, causing event routing to fail. The issue was resolved by implementing a static event router that bypasses DI instance management for event handler registration.

## Timeline

### 2025-09-08 15:45 - Initial Refactoring Complete
- ✅ Successfully removed static callback anti-pattern from `ExecuteAttackCommandHandler`
- ✅ Implemented clean domain events (`ActorDiedEvent`, `ActorDamagedEvent`)  
- ✅ Created `GameManagerEventBridge` with proper `INotificationHandler` interfaces
- ✅ All 155 tests passing with zero warnings
- ✅ Code compiled without errors

### 2025-09-08 16:00 - Issue Discovery
- ❌ User reported UI not updating during combat
- ❌ Health bars remained static despite damage being dealt
- ❌ Dead actors remained visible on screen
- ❌ No visible errors in application logs

### 2025-09-08 16:05 - Investigation Begins  
- Added debug logging to `GameManagerEventBridge`
- Confirmed events were being published by `ExecuteAttackCommandHandler`
- Discovered bridge service was being created during startup
- GameManager handlers were successfully registered with bridge

### 2025-09-08 16:15 - Root Cause Identified
- **Critical Discovery**: Multiple bridge instances were being created
- **Instance #1**: Created during startup, GameManager handlers registered here
- **Instance #2, #3, #4...**: Created during combat for each event
- **Problem**: Events routed to new instances, but handlers only on Instance #1

### 2025-09-08 16:25 - Solution Implemented
- Created `GameManagerEventRouter` with static handler registration
- Replaced DI-managed handler registration with static approach
- Removed problematic `GameManagerEventBridge` service
- Updated `GameStrapper` to register new router

### 2025-09-08 16:30 - Resolution Confirmed
- ✅ UI updates working correctly
- ✅ Health bars update in real-time
- ✅ Dead actors removed from screen
- ✅ All tests still passing (155/155)
- ✅ No error messages in logs

## Root Cause Analysis

### Primary Root Cause: MediatR DI Instance Lifecycle Issue

**Technical Details:**
```csharp
// ❌ PROBLEM: DI registration appeared correct
services.AddSingleton<GameManagerEventBridge>();

// ❌ PROBLEM: Handler registration with Instance #1 
var eventBridge = _serviceProvider.GetRequiredService<GameManagerEventBridge>();
eventBridge.RegisterHandlers(onActorDied, onActorDamaged);

// ❌ PROBLEM: MediatR created NEW instances for each event
// Instance #2, #3, #4... had no registered handlers
```

**Evidence from Logs:**
```
[16:32:05] 🌉 Bridge service INSTANCE #1 created - ready to route events
[16:32:05] 🔗 INSTANCE #1 registering GameManager handlers - enabled
[16:32:07] 🌉 Bridge service INSTANCE #2 created - ready to route events  
[16:32:07] 💔 INSTANCE #2 RECEIVED ActorDamagedEvent
[16:32:07] ❌ INSTANCE #2 No damage handler registered!
```

### Contributing Factors

1. **MediatR Notification Handler Instantiation**
   - MediatR appears to create new instances of notification handlers per event
   - This behavior bypasses DI singleton lifecycle management
   - Documentation unclear on notification handler instance management

2. **Complex DI Integration with Godot Nodes**
   - GameManager is a Godot `Node2D`, not a DI-registered service
   - Bridge pattern required to connect DI world to Godot world
   - Multiple layers of indirection obscured the instance problem

3. **Insufficient Integration Testing**
   - All unit tests passed because they used mocked dependencies
   - No integration tests covered MediatR event routing to actual UI
   - Issue only surfaced during manual gameplay testing

## Impact Assessment

### User Impact
- **Severity**: Critical - Complete loss of visual feedback during combat
- **Duration**: 45 minutes from discovery to resolution
- **Affected Features**: All combat UI updates (health bars, actor removal)
- **Data Loss**: None (combat logic continued to work correctly)

### System Impact
- **Performance**: No performance degradation
- **Data Integrity**: Maintained (all domain logic unaffected)
- **Testing**: All automated tests continued to pass
- **Architecture**: Improved (eliminated static anti-pattern)

## What Went Right

1. **Excellent Logging Strategy**
   - Debug logging quickly identified multiple instance creation
   - Instance counters provided clear evidence of the problem
   - Comprehensive event flow visibility

2. **Solid Test Coverage**
   - All 155 tests continued passing throughout incident
   - Test-driven approach prevented business logic regressions
   - Quick confidence that domain layer was unaffected

3. **Clean Architecture Benefits**
   - Issue isolated to presentation layer event routing
   - Domain events themselves worked correctly
   - Business logic completely unaffected

4. **Rapid Problem Isolation**
   - Issue quickly narrowed to MediatR event routing
   - Clear separation between event publishing (working) and handling (broken)

## What Went Wrong

1. **Insufficient MediatR Integration Knowledge**
   - Assumed DI singleton lifecycle would apply to notification handlers
   - Did not anticipate MediatR creating multiple handler instances
   - Relied on documentation that was unclear about instance management

2. **Missing Integration Test Coverage**
   - No tests covering complete event flow from domain to UI
   - Unit tests with mocks did not catch DI lifecycle issues
   - Manual testing was only way to discover the problem

3. **Overcomplex Initial Solution**
   - Bridge pattern added unnecessary indirection
   - DI integration created lifecycle dependencies
   - Should have started with simpler static approach

## Action Items

### Immediate (Completed)
- [x] Implement `GameManagerEventRouter` with static handler registration (**TEMPORARY SOLUTION**)
- [x] Remove problematic `GameManagerEventBridge` service
- [x] Clean up debug logging to reduce verbosity
- [x] Verify all UI updates working correctly

### Short Term (Within 1 week) - **ARCHITECTURAL DEBT RESOLUTION**
- [ ] **CRITICAL**: Research proper MediatR notification handler lifecycle management
- [ ] **CRITICAL**: Investigate MediatR configuration options for singleton behavior
- [ ] **CRITICAL**: Explore alternative event routing patterns (Observer, Event Bus)
- [ ] Create integration tests for MediatR event routing to UI
- [ ] Document current static solution as temporary architectural debt

### Long Term (Within 1 month) - **PROPER SOLUTION IMPLEMENTATION**
- [ ] **Replace static handler approach** with proper architectural solution
- [ ] Evaluate switching from MediatR to custom event bus for UI events
- [ ] Consider separating domain events from UI notification events
- [ ] Implement event sourcing pattern for better event observability
- [ ] Create architectural guidelines for DI integration with Godot

## Technical Details

### Final Solution Architecture

```csharp
// ✅ SOLUTION: Static handler registration bypasses DI instance issues
public sealed class GameManagerEventRouter : INotificationHandler<ActorDiedEvent>
{
    private static Action<ActorDiedEvent>? _staticOnActorDied;
    
    // ✅ Static registration ensures consistency across instances
    public static void RegisterHandlers(Action<ActorDiedEvent> handler) 
    {
        _staticOnActorDied = handler;
    }
    
    // ✅ Any instance can access the static handler
    public Task Handle(ActorDiedEvent notification, CancellationToken ct)
    {
        _staticOnActorDied?.Invoke(notification);
        return Task.CompletedTask;
    }
}
```

### Key Lessons Learned

1. **MediatR Notification Handlers**: May create new instances per event despite DI singleton registration
2. **Static Patterns**: **Should be avoided** - they create technical debt even when solving immediate problems
3. **Integration Testing**: Critical for complex DI scenarios that unit tests can't catch
4. **Godot + DI Integration**: Requires careful consideration of node lifecycle vs. service lifecycle
5. **Emergency vs. Proper Solutions**: **Quick fixes often become permanent debt** - resist the temptation to ship hacks
6. **Architectural Trade-offs**: **Reliability without clean design is unsustainable** in the long term

## Prevention Strategies

1. **Enhanced Testing**
   - Add integration tests for all MediatR event flows
   - Include manual UI testing in CI/CD pipeline
   - Create smoke tests for critical user interactions

2. **Better Documentation**
   - Document all DI service lifetimes and their implications
   - Create architectural decision records for complex integration patterns
   - Maintain troubleshooting guides for common DI issues

3. **Monitoring**
   - Add health checks for UI event routing
   - Monitor event handler registration status
   - Alert on event routing failures

## Architectural Concerns & Technical Debt

### Current Solution Assessment: **PRAGMATIC HACK, NOT BEST PRACTICE**

**Critical Evaluation**: The static handler registration solution, while functional, introduces significant architectural debt:

#### **Problems with Current Approach**
1. **Global Mutable State**: Static handlers violate functional programming principles
2. **Testing Complexity**: Static state makes parallel test execution impossible
3. **Thread Safety**: Potential race conditions if handlers are re-registered
4. **Hidden Dependencies**: Static coupling obscures the dependency graph
5. **Violation of DI Principles**: Bypasses the entire dependency injection system

#### **Anti-Patterns Introduced**
```csharp
// ❌ ANTI-PATTERN: Global mutable state
private static Action<ActorDiedEvent>? _staticOnActorDied;

// ❌ ANTI-PATTERN: Static registration breaks testability
public static void RegisterHandlers(...)
{
    _staticOnActorDied = handler; // Global mutation
}
```

#### **Why This Is Technical Debt**
- **We traded one anti-pattern (static callbacks) for another (static handlers)**
- **The solution works but violates Clean Architecture principles**
- **Future maintainers will struggle with the global state management**
- **Testing becomes more complex due to shared static state**

### **Preferred Architectural Solutions** (Future Work)

1. **Custom Event Bus with Proper DI**
   ```csharp
   public interface IUIEventBus
   {
       void Subscribe<T>(IEventHandler<T> handler) where T : IDomainEvent;
       Task PublishAsync<T>(T domainEvent) where T : IDomainEvent;
   }
   ```

2. **Observer Pattern with Weak References**
   ```csharp
   public interface IDomainEventPublisher
   {
       void Subscribe(WeakReference<IUIEventHandler> handler);
       Task NotifyAsync<T>(T domainEvent) where T : IDomainEvent;
   }
   ```

3. **MediatR with Proper Container Configuration**
   - Research MediatR lifetime scopes
   - Investigate container-per-request patterns
   - Explore MediatR pipeline behaviors for UI routing

## Conclusion

This incident highlights the complexity of integrating external libraries (MediatR) with game engines (Godot) that have their own lifecycle management. While the static callback anti-pattern was successfully eliminated, **the replacement solution is itself an anti-pattern** that should be considered temporary technical debt.

**The current static handler approach is a pragmatic hack that prioritizes immediate functionality over architectural integrity.** While appropriate as an emergency fix, it must be replaced with a proper architectural solution that maintains both reliability and clean design principles.

The incident resulted in:
- ✅ **Immediate problem resolution** (UI updates work)
- ❌ **Introduction of new technical debt** (static handlers)
- ⚡ **Critical need for architectural refactoring** to eliminate the hack
- 📚 **Deeper understanding** of MediatR/DI integration challenges

**This is a temporary solution that must be replaced with proper architecture to maintain long-term system health.**

---

**Reviewers**: Tech Lead, Test Specialist  
**Post-Mortem Review Date**: TBD  
**Related ADRs**: ADR-008 (Functional Error Handling), ADR-005 (Persona Completion Authority)  
**Related Issues**: TD_012 (Static Callback Removal)