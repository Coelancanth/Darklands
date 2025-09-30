# Control Flow Example: Move → Attack → Save

**Last Updated**: 2025-09-30
**Purpose**: Demonstrate complete data flow through Clean Architecture + Godot integration
**Scenario**: Player moves actor on grid, attacks enemy, result saved to markdown file

## Architecture Overview

This example shows how **Commands**, **Handlers**, **Events**, and **Subscribers** work together across layers:

- **Presentation Layer** (Godot): User input, visual updates
- **Application Layer** (Core): Command handlers, business logic
- **Infrastructure Layer**: Event forwarding, persistence
- **Domain Layer** (Core): Pure business rules, state

---

## Complete Control Flow

### Phase 1: Actor Move

#### **1. USER INPUT (Presentation Layer - Godot)**

User clicks on grid tile to move actor.

```csharp
// Presentation/UI/GridInputNode.cs
public partial class GridInputNode : Node2D
{
    private IMediator _mediator;

    private async void _on_tile_clicked(Vector2I tilePosition)
    {
        // Godot → Core: Send command
        var result = await _mediator.Send(
            new MoveActorCommand(
                ActorId: _selectedActorId,
                TargetPosition: tilePosition));

        result.Match(
            onSuccess: () => _logger.LogInformation("Move executed"),
            onFailure: err => ShowError(err));
    }
}
```

---

#### **2. COMMAND HANDLER (Core Application Layer - Pure C#)**

```csharp
// Core/Application/Commands/MoveActorCommandHandler.cs
public class MoveActorCommandHandler : IRequestHandler<MoveActorCommand, Result>
{
    private readonly IGridStateService _gridService;
    private readonly IMediator _mediator;

    public async Task<Result> Handle(MoveActorCommand cmd, CancellationToken ct)
    {
        // 1. Validate move (business logic)
        var currentPos = _gridService.GetActorPosition(cmd.ActorId);
        if (currentPos.IsFailure)
            return Result.Failure("Actor not found");

        var isValid = IsValidMove(currentPos.Value, cmd.TargetPosition);
        if (!isValid)
            return Result.Failure("Invalid move");

        // 2. Update state (Core state change)
        var moveResult = _gridService.MoveActor(cmd.ActorId, cmd.TargetPosition);
        if (moveResult.IsFailure)
            return moveResult;

        // 3. Publish domain event (notify subscribers)
        await _mediator.Publish(new ActorMovedEvent(
            ActorId: cmd.ActorId,
            OldPosition: currentPos.Value,
            NewPosition: cmd.TargetPosition,
            Timestamp: DateTime.UtcNow));

        return Result.Success();
    }
}
```

---

#### **3. EVENT FORWARDING (Infrastructure Layer)**

**MediatR → GodotEventBus Bridge**:

```csharp
// Infrastructure/Events/UIEventForwarder.cs
public class UIEventForwarder<TEvent> : INotificationHandler<TEvent>
    where TEvent : INotification
{
    private readonly IGodotEventBus _eventBus;

    public Task Handle(TEvent notification, CancellationToken ct)
    {
        // Forward to GodotEventBus (for UI updates)
        return _eventBus.PublishAsync(notification);
    }
}
```

**GodotEventBus dispatches to Godot nodes** (thread-safe, main thread):

```csharp
// Infrastructure/Events/GodotEventBus.cs
public Task PublishAsync<TEvent>(TEvent notification)
{
    // ...prune dead references, get snapshot...

    foreach (var sub in snapshot)
    {
        if (sub.Target.TryGetTarget(out var target) && target is Node node)
        {
            // Defer to main thread (thread-safe)
            Callable.From(() => sub.TryInvoke(notification, _logger)).CallDeferred();
        }
    }

    return Task.CompletedTask;
}
```

---

#### **4. UI UPDATE (Presentation Layer - Godot)**

```csharp
// Presentation/Components/ActorSpriteNode.cs
public partial class ActorSpriteNode : EventAwareNode
{
    protected override void SubscribeToEvents()
    {
        EventBus.Subscribe<ActorMovedEvent>(this, OnActorMoved);
    }

    private void OnActorMoved(ActorMovedEvent e)
    {
        if (e.ActorId != _actorId) return;

        // Update visual position (Godot native)
        var tween = CreateTween();
        tween.TweenProperty(this, "position",
            ToGodotCoords(e.NewPosition),
            duration: 0.3f);

        _logger.LogDebug("Actor sprite moved to {Position}", e.NewPosition);
    }
}
```

---

### Phase 2: Attack Enemy

#### **5. USER INPUT - Attack (Presentation Layer)**

```csharp
// Presentation/UI/CombatUI.cs
private async void _on_attack_button_pressed()
{
    try
    {
        var result = await _mediator.Send(
            new ExecuteAttackCommand(
                AttackerId: _playerActorId,
                TargetId: _selectedEnemyId));

        result.Match(
            onSuccess: () => _logger.LogInformation("Attack succeeded"),
            onFailure: err => _logger.LogError("Attack failed: {Error}", err));
    }
    catch (Exception ex)
    {
        // CRITICAL: async void safety (from ADR-002)
        _logger.LogError(ex, "Unhandled exception in attack handler");
    }
}
```

---

#### **6. ATTACK HANDLER (Core Application Layer)**

```csharp
// Core/Application/Commands/ExecuteAttackCommandHandler.cs
public class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Result>
{
    private readonly IComponentRegistry _registry;
    private readonly IMediator _mediator;

    public async Task<Result> Handle(ExecuteAttackCommand cmd, CancellationToken ct)
    {
        // 1. Get attacker and target from registry
        var attackerResult = _registry.GetComponent<CombatComponent>(cmd.AttackerId);
        var targetResult = _registry.GetComponent<HealthComponent>(cmd.TargetId);

        if (attackerResult.IsFailure || targetResult.IsFailure)
            return Result.Failure("Actor not found");

        // 2. Calculate damage (pure business logic)
        var attacker = attackerResult.Value;
        var target = targetResult.Value;
        var damage = CalculateDamage(attacker, target);

        // 3. Apply damage (state mutation)
        var damageResult = target.TakeDamage(damage);
        if (damageResult.IsFailure)
            return damageResult;

        // 4. Publish domain event (notify all interested parties)
        await _mediator.Publish(new AttackExecutedEvent(
            AttackerId: cmd.AttackerId,
            TargetId: cmd.TargetId,
            Damage: damage,
            TargetDied: !target.IsAlive,
            Timestamp: DateTime.UtcNow));

        return Result.Success();
    }

    private float CalculateDamage(CombatComponent attacker, HealthComponent target)
    {
        // Domain logic: damage calculation
        return attacker.AttackPower * (1.0f - target.Defense);
    }
}
```

---

#### **7. MULTIPLE EVENT SUBSCRIBERS (Parallel, Independent)**

**7a. UI Update (Presentation)**:

```csharp
// Presentation/Components/HealthBarNode.cs
public partial class HealthBarNode : EventAwareNode
{
    protected override void SubscribeToEvents()
    {
        EventBus.Subscribe<AttackExecutedEvent>(this, OnAttackExecuted);
    }

    private void OnAttackExecuted(AttackExecutedEvent e)
    {
        if (e.TargetId != _actorId) return;

        // Visual feedback
        _healthBar.Value = GetCurrentHealthPercentage();
        _damageFlashAnimation.Play("damage_flash");
        _damageLabel.Text = $"-{e.Damage:F0}";
    }
}
```

**7b. File Persistence (Infrastructure)**:

```csharp
// Infrastructure/Persistence/CombatLogHandler.cs
public class CombatLogHandler : INotificationHandler<AttackExecutedEvent>
{
    private readonly string _logFilePath;
    private readonly ILogger<CombatLogHandler> _logger;

    public CombatLogHandler(ILogger<CombatLogHandler> logger)
    {
        _logFilePath = "combat_log.md";
        _logger = logger;
    }

    public async Task Handle(AttackExecutedEvent notification, CancellationToken ct)
    {
        try
        {
            var logEntry = FormatLogEntry(notification);

            // Append to markdown file
            await File.AppendAllTextAsync(_logFilePath, logEntry, ct);

            _logger.LogInformation("Combat log saved: {AttackerId} → {TargetId}",
                notification.AttackerId, notification.TargetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write combat log");
            // Don't throw—logging shouldn't break game
        }
    }

    private string FormatLogEntry(AttackExecutedEvent e)
    {
        return $@"
## Attack - {e.Timestamp:yyyy-MM-dd HH:mm:ss}

- **Attacker**: {e.AttackerId}
- **Target**: {e.TargetId}
- **Damage**: {e.Damage:F1}
- **Result**: {(e.TargetDied ? "💀 Target Died" : "✅ Target Survived")}

---
";
    }
}
```

**7c. Sound Effects (Presentation)**:

```csharp
// Presentation/Audio/CombatAudioNode.cs
public partial class CombatAudioNode : EventAwareNode
{
    [Export] private AudioStreamPlayer _attackSound;

    protected override void SubscribeToEvents()
    {
        EventBus.Subscribe<AttackExecutedEvent>(this, OnAttackExecuted);
    }

    private void OnAttackExecuted(AttackExecutedEvent e)
    {
        _attackSound.Play();
    }
}
```

---

## Flow Diagram

```
USER CLICKS TILE
      ↓
[GridInputNode] ─── MoveActorCommand ───→ [MoveActorCommandHandler]
                                                  ↓
                                          1. Validate move
                                          2. Update GridStateService
                                          3. Publish ActorMovedEvent
                                                  ↓
                                          [UIEventForwarder] → [GodotEventBus]
                                                  ↓
                                    ┌─────────────┴─────────────┐
                                    ↓                           ↓
                            [ActorSpriteNode]           [GridHighlightNode]
                            Updates position           Clears highlight


USER CLICKS ATTACK BUTTON
      ↓
[CombatUI] ─── ExecuteAttackCommand ───→ [ExecuteAttackCommandHandler]
                                                  ↓
                                          1. Get components (registry)
                                          2. Calculate damage
                                          3. Apply to HealthComponent
                                          4. Publish AttackExecutedEvent
                                                  ↓
                                          [UIEventForwarder] → [GodotEventBus]
                                                  ↓
                        ┌───────────────────┼───────────────────┐
                        ↓                   ↓                   ↓
              [HealthBarNode]      [CombatLogHandler]   [CombatAudioNode]
              Update health bar    Write to file       Play sound
```

---

## Key Architectural Points

### **1. Godot NEVER sees Core directly**
- Godot nodes send commands via `IMediator`
- Core publishes events via `IMediator`
- `GodotEventBus` bridges the gap (see ADR-002)

### **2. Multiple subscribers, no coupling**
- **UI updates**: Health bar, damage numbers, animations
- **File persistence**: Combat log writer
- **Sound effects**: Audio playback
- All react to same event **independently**

### **3. Thread safety built-in**
- `CallDeferred` ensures UI updates on main thread
- `lock` protects ComponentRegistry
- No race conditions (see ADR-002: Thread Safety)

### **4. Error isolation**
- Handler exception doesn't crash game (try/catch in `TryInvoke`)
- File write failure doesn't stop UI updates
- Each subscriber independent (see ADR-002: Error Handling)

### **5. State sources clear (SSOT)**
- **Grid positions**: `IGridStateService` (SSOT)
- **Actor stats**: `IComponentRegistry` (SSOT)
- **Visuals**: Godot nodes (derived from events)

---

## Result: `combat_log.md`

```markdown
## Attack - 2025-09-30 14:23:45

- **Attacker**: actor_123
- **Target**: enemy_456
- **Damage**: 25.5
- **Result**: ✅ Target Survived

---

## Attack - 2025-09-30 14:23:52

- **Attacker**: actor_123
- **Target**: enemy_456
- **Damage**: 30.2
- **Result**: 💀 Target Died

---
```

---

## Why This Architecture Works

**Extensibility**: Want to add replays? Just subscribe to events and save them.

**Testability**: Core handlers can be tested without Godot:
```csharp
[Fact]
public async Task ExecuteAttack_ValidTarget_PublishesEvent()
{
    // Arrange
    var handler = new ExecuteAttackCommandHandler(_registry, _mediator);
    var command = new ExecuteAttackCommand(attacker, target);

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    _mediator.Verify(m => m.Publish(It.IsAny<AttackExecutedEvent>(), default));
}
```

**Maintainability**: Each layer has clear responsibilities:
- **Presentation**: Input/output only
- **Application**: Orchestration and business logic
- **Domain**: Pure rules and calculations
- **Infrastructure**: Cross-cutting concerns (logging, persistence)

**This architecture allows you to add new subscribers (analytics, replays, achievements) without touching existing code—just subscribe to events!** 🎯

---

## References

- [ADR-001: Clean Architecture Foundation](ADR/ADR-001-clean-architecture-foundation.md)
- [ADR-002: Godot Integration Architecture](ADR/ADR-002-godot-integration-architecture.md)
- [ADR-003: Functional Error Handling](ADR/ADR-003-functional-error-handling.md)
- [HANDBOOK.md](HANDBOOK.md) - Daily reference guide