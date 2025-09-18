# ADR-022: Logical-Visual Position Separation Pattern

**Status**: Accepted (Revised)
**Date**: 2025-09-17 (Revised 2025-09-18)
**Decision Makers**: Tech Lead, Dev Engineer, User
**Tags**: `architecture` `state-management` `animation` `fog-of-war` `two-position-model` `event-driven`

## Context

In tactical turn-based games, we frequently encounter scenarios where game logic needs to advance incrementally (for fog of war, vision calculations) while visual representation smoothly follows behind. The initial problem arose with fog of war revealing the destination immediately while the actor was still animating movement.

This creates a fundamental tension:
- **Game logic** needs instant, deterministic state changes for saves, replays, and testing
- **Player experience** needs progressive visualization for comprehension and immersion
- **Clean Architecture** demands these concerns remain separated

## Decision

We will separate game entity positions into two distinct concerns:

1. **Logical Position** - The authoritative game state that advances cell-by-cell on a timer (used for FOV, collision, game rules)
2. **Visual Position** - The cosmetic sprite location that animates smoothly to follow logical position (purely for player feedback)

**Core Principle**: All game mechanics (FOV, combat, collision) operate on Logical Position. Visual Position is purely cosmetic and NEVER affects game logic.

**Event Flow**:
- Logical position advances → Publishes event → Visual position responds
- Direction is ALWAYS: Application Layer → Presentation Layer (never reverse)

### Implementation Pattern

```csharp
// 1. Domain Layer - Movement progression with logical position
public class MovementProgression
{
    public Position LogicalPosition { get; private set; }
    public IReadOnlyList<Position> Path { get; private set; }
    private int _elapsedMs;
    private int _currentIndex;

    public Option<Position> AdvanceTime(int milliseconds)
    {
        _elapsedMs += milliseconds;
        if (_elapsedMs >= MillisecondsPerCell && _currentIndex < Path.Count)
        {
            LogicalPosition = Path[_currentIndex++];
            _elapsedMs = 0;
            return Some(LogicalPosition);
        }
        return None;
    }
}

// 2. Application Layer - Movement service that updates FOV atomically
public interface IMovementProgressionService
{
    void StartMovement(ActorId actor, Path path);
    void AdvanceGameTime(int milliseconds);
    Position GetLogicalPosition(ActorId actor);
}

public class MovementProgressionService : IMovementProgressionService
{
    public void AdvanceGameTime(int milliseconds)
    {
        foreach (var progression in _activeProgressions.Values)
        {
            progression.AdvanceTime(milliseconds)
                .IfSome(newPos =>
                {
                    // ATOMIC updates when logical position changes
                    UpdateActorLogicalPosition(progression.ActorId, newPos);
                    UpdateFOVFromPosition(newPos);
                    PublishMovementEvent(progression.ActorId, newPos);
                });
        }
    }
}

// 3. Presentation Layer - Visual follows logical
public class ActorView : Node2D
{
    public void OnLogicalPositionChanged(Position newLogicalPos)
    {
        // Visual sprite animates to catch up with logical position
        var worldPos = GridToWorld(newLogicalPos);
        var tween = GetTree().CreateTween();
        tween.TweenProperty(this, "position", worldPos, 0.2f);
    }
}
```

## Consequences

### Positive

- **Clean Separation**: Logical and visual concerns completely decoupled
- **Deterministic**: Logical position advances on fixed timer, reproducible
- **Save-Friendly**: Only need to save logical position and timer state
- **Testable**: Can test FOV updates without any animation system
- **Intuitive**: Two positions (logical/visual) matches board game mental model
- **Performance**: FOV updates at controlled intervals (5x/second), not every frame
- **Interrupt-Friendly**: Can cleanly cancel movement and start new command
- **Atomic Updates**: FOV and position update together, maintaining consistency

### Negative

- **Visual Lag**: Sprite slightly behind logical position (barely noticeable at 200ms)
- **Discrete FOV**: FOV updates in steps rather than smoothly (actually more honest for grid-based games)
- **Timer Management**: Need to track elapsed time for progressions

## Alternatives Considered

### Alternative 1: Visual Position Coupling
Update FOV based on sprite's visual position as it animates.
- **Rejected**: Non-deterministic (frame-rate dependent), untestable, save/load nightmare

### Alternative 2: Three-Position Model
Track Game Position, Revealed Position, and Visual Position separately.
- **Rejected**: Over-engineered for our needs. Two positions achieve same goals with less complexity

### Alternative 3: Instant FOV Updates
Update FOV immediately when movement command issued.
- **Rejected**: Reveals entire path before actor moves, breaks tactical gameplay

### Alternative 4: Waypoint Events in Command Handler
Have command handler sleep and emit events at each waypoint.
- **Rejected**: Blocks handler, can't handle interruptions, mixes timing into business logic

## Godot Integration

### CRITICAL: Architectural Boundary Alignment

Per ADR-006 and ADR-010, the Logical-Visual Separation must respect these boundaries:

1. **Logical Position** → Domain/Application Layers (pure C#, deterministic)
2. **Visual Position** → Presentation Layer (Godot animations, cosmetic only)

**MANDATORY RULES**:
- Logical position NEVER depends on visual position
- Visual updates happen via one-way events (Application → Presentation)
- Game logic NEVER waits for animations to complete
- Presenters NEVER drive game state changes

### Coordinating with Godot's Update Loop

The Temporal Decoupling Pattern must integrate with Godot's frame-based update system while respecting architectural boundaries:

```csharp
// APPLICATION LAYER: Game time service (NOT in Presenter!)
public interface IGameTimeService
{
    void AdvanceTime(int deltaMs);
    event Action<int> TimeAdvanced;
}

public class GameTimeService : IGameTimeService
{
    private readonly IFogOfWarRevealService _revealService;
    private readonly IMediator _mediator;
    private int _accumulatedMs = 0;
    private const int TickMs = 50; // 20 ticks per second

    public void AdvanceTime(int deltaMs)
    {
        _accumulatedMs += deltaMs;

        while (_accumulatedMs >= TickMs)
        {
            // Advance all time-based systems
            _revealService.AdvanceTime(TickMs);
            _mediator.Publish(new GameTickEvent(TickMs));
            _accumulatedMs -= TickMs;

            TimeAdvanced?.Invoke(TickMs);
        }
    }
}

// INFRASTRUCTURE: Bridge from Godot to Application (Autoload)
public partial class GameTimeDriver : Node
{
    private IGameTimeService _gameTimeService;
    private IScopeManager _scopeManager;

    public override void _Ready()
    {
        // GameTimeDriver is registered as autoload in project.godot
        Name = "GameTimeDriver";

        // Get services through proper DI (per ADR-018)
        var serviceLocator = GetNode<ServiceLocator>("/root/ServiceLocator");
        _scopeManager = serviceLocator.ScopeManager;
        _gameTimeService = this.GetService<IGameTimeService>();

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Process(double delta)
    {
        if (!GetTree().Paused)
        {
            // Convert Godot delta to milliseconds and advance game time
            _gameTimeService.AdvanceTime((int)(delta * 1000));
        }
    }
}
```

### Godot Signal Flow

```csharp
// 1. Game Position Change → Godot Signal
public class MoveActorCommandHandler
{
    public async Task<Fin<Unit>> Handle(MoveActorCommand command)
    {
        // Update game position instantly
        actor.MoveTo(command.Destination);

        // Start reveal progression
        _revealService.StartRevealProgression(actor.Id, command.Path);

        // Notify Godot views via signal
        _eventBus.Publish(new ActorMovedEvent(actor.Id, command.Path));
    }
}

// 2. Revealed Position Change → FOV Update
public class FogOfWarRevealService
{
    public void AdvanceTime(int gameMs)
    {
        // ... advance reveal position ...
        if (positionChanged)
        {
            // Trigger FOV recalculation
            _eventBus.Publish(new RevealPositionChangedEvent(actorId, newPos));
        }
    }
}

// 3. Visual Position → Godot Scene Graph (per ADR-016)
public partial class ActorView : Node2D
{
    [Signal]
    public delegate void MovementCompletedEventHandler();

    public override void _Ready()
    {
        // Per ADR-016: Health bar is CHILD node, not separate view
        var healthBar = new ProgressBar();
        healthBar.Position = new Vector2(0, -20);
        AddChild(healthBar);

        // Status effects also children - move automatically with actor
        var statusIcons = new HBoxContainer();
        statusIcons.Position = new Vector2(0, -30);
        AddChild(statusIcons);
    }

    public void OnActorMoved(ActorMovedEvent evt)
    {
        // Animate actor position - children move automatically!
        var tween = GetTree().CreateTween();
        foreach (var position in evt.Path)
        {
            var worldPos = GridToWorld(position);
            tween.TweenProperty(this, "position", worldPos, 0.2f);
        }
        tween.TweenCallback(Callable.From(() => EmitSignal(SignalName.MovementCompleted)));
    }
}
```

### Pause Handling

```csharp
// Pause is handled in the GameTimeDriver (Infrastructure layer)
public partial class GameTimeDriver : Node
{
    public override void _Process(double delta)
    {
        // Check Godot's pause state BEFORE advancing game time
        if (GetTree().Paused) return;

        _gameTimeService.AdvanceTime((int)(delta * 1000));
    }
}

// Presenters DON'T drive game time - they RESPOND to events
public partial class GridPresenter : EventAwarePresenter
{
    protected override void OnNotification(INotification notification)
    {
        switch (notification)
        {
            case RevealPositionChangedEvent evt:
                // Update FOV display based on new reveal position
                UpdateFogOfWarDisplay(evt.ActorId, evt.Position);
                break;
        }
    }
}
```

### Service Registration (per ADR-018)

```csharp
// In ServiceConfiguration.cs
public static IServiceProvider ConfigureServices()
{
    var services = new ServiceCollection();

    // Time and progression services
    services.AddSingleton<IGameTimeService, GameTimeService>();
    services.AddSingleton<IFogOfWarRevealService, FogOfWarRevealService>();

    // Other services...
    services.AddSingleton<IScopeManager>(provider =>
        new GodotScopeManager(provider));

    return services.BuildServiceProvider();
}
```

### Autoload Configuration

```gdscript
# In project.godot
[autoload]
ServiceLocator="*res://ServiceLocator.cs"
GameTimeDriver="*res://Infrastructure/GameTimeDriver.cs"
```

### Scene Tree Considerations

- **GameTimeDriver**: Autoload node under /root, survives scene changes
- **Presenter Lifetime**: Presenters tied to scene lifecycle via DI scope
- **View Updates**: Views use Godot signals, not direct references
- **Service Cleanup**: Application services handle their own state

```csharp
// GameTimeDriver is persistent (under /root)
public partial class GameTimeDriver : Node
{
    public override void _Ready()
    {
        // This node persists across scene changes
        GetTree().AutoAcceptQuit = false;
    }
}

// Presenters are transient (per scene)
public partial class GridPresenter : EventAwarePresenter
{
    public override void _ExitTree()
    {
        // Presenters clean up subscriptions, not game state
        UnsubscribeAll();
        base._ExitTree();
    }
}

// Services manage their own lifecycle
public class FogOfWarRevealService : IFogOfWarRevealService
{
    public void OnSceneChange()
    {
        // Service decides what to keep/clear on scene change
        _activeProgressions.Clear();
    }
}
```

## Implementation Guidelines

### Visual Position and Scene Graph (per ADR-016)

**CRITICAL**: Visual position leverages Godot's scene graph for automatic transformations:

```gdscript
# Scene structure - health/status are CHILDREN, not separate views
ActorView (Node2D)              # Parent - handles visual position
  ├── Sprite2D                  # Visual representation
  ├── HealthBar (ProgressBar)   # Moves automatically with parent
  ├── StatusEffects (HBox)      # Positioned relative to parent
  └── SelectionIndicator        # Inherits parent transform
```

**Benefits of Scene Graph for Visual Position**:
- Children automatically follow parent's visual position
- No manual synchronization needed
- Transform inheritance (scale, rotation)
- Automatic cleanup when parent freed

### When to Apply This Pattern

Use the Temporal Decoupling Pattern when:
- State changes instantly but visualization takes time
- View calculations need intermediate positions
- Testing requires deterministic behavior
- Save/load must work during transitions

### Specific Applications

1. **Movement**: Actor moves instantly, FOV reveals progressively, sprite animates smoothly
2. **Combat**: Damage applied instantly, health bar animates, floating text rises
3. **Resource Changes**: Gold deducted instantly, UI counter rolls to new value
4. **Spell Effects**: Effect applied instantly, visual particles play over time

### Service Naming Convention

Services that manage revealed position should be named by their PURPOSE, not mechanism:
- ✅ `IFogOfWarRevealService`
- ✅ `ICombatAnimationService`
- ❌ `ILogicalMovementService`
- ❌ `IProgressionService`

## Godot-Specific Challenges and Solutions

### Challenge 1: Frame Rate Independence
**Problem**: Godot's `_process` runs at variable frame rates (30-144fps)
**Solution**: Use fixed game ticks accumulated from delta time

### Challenge 2: Thread Safety
**Problem**: Game logic may run on different thread than Godot main thread
**Solution**: Use `CallDeferred` for UI updates from background threads
```csharp
// Safe cross-thread UI update
Callable.From(() => UpdateFogOfWar(newVision)).CallDeferred();
```

### Challenge 3: Node Lifecycle
**Problem**: Progressions outliving their associated nodes
**Solution**: Cancel progressions in `_ExitTree()` and validate node references

### Challenge 4: Save During Animation
**Problem**: Visual position not at game position during save
**Solution**: Only serialize game position and progression state, reconstruct visual on load
```csharp
public class SaveData
{
    public Position GamePosition { get; set; }
    public RevealProgressionState RevealState { get; set; }
    // Visual position is NOT saved - will catch up on load
}
```

### Challenge 5: Multiplayer Considerations
**Problem**: Network lag adds fourth position type (predicted)
**Solution**: Pattern extends to four positions for multiplayer
```
1. Authoritative (server) → 2. Predicted (client) → 3. Revealed → 4. Visual
```

## Related Decisions

- **ADR-004**: Deterministic Simulation - Game position updates must be deterministic
- **ADR-005**: Save-Ready Architecture - Only game and progression state need saving
- **ADR-006**: Selective Abstraction - Visual position handled directly by Godot (NOT abstracted)
- **ADR-010**: UI Event Bus - Events coordinate between the three positions
- **ADR-016**: Embrace Scene Graph - Visual elements use parent-child relationships
- **ADR-018**: Godot DI Lifecycle - Services properly scoped, GameTimeDriver as autoload
- **ADR-021**: MVP Separation - Presenters bridge game logic to Godot views

## Amendment 1: Discrete Visual Movement Mode (2025-09-18)

Based on TD_062 analysis, the Temporal Decoupling Pattern now supports both interpolated and discrete visual movement:

### Visual Position Modes

#### 1. Interpolated Mode (Original)
- Visual position smoothly animates between positions
- Uses Godot tweening for fluid motion
- Best for: Projectiles, spell effects, camera panning
- Risk: Can cause corner-clipping with diagonal paths

#### 2. Discrete Mode (NEW - Default for Actors)
- Visual position updates instantly to match revealed position
- No interpolation = no clipping possible
- Best for: Actor movement, teleportation, grid-based games
- Benefit: Completely eliminates sprite clipping issues

### Implementation Pattern

```csharp
public interface IVisualPositionStrategy
{
    void UpdateVisualPosition(Node2D node, Position target, float duration);
}

public class DiscretePositionStrategy : IVisualPositionStrategy
{
    public void UpdateVisualPosition(Node2D node, Position target, float duration)
    {
        // Instant position update
        node.Position = GridToPixel(target);

        // Optional: Add arrival feedback
        AddVisualFeedback(node, target);
    }

    private void AddVisualFeedback(Node2D node, Position target)
    {
        // Brief flash
        node.Modulate = Colors.White * 1.3f;
        node.CreateTween().TweenProperty(node, "modulate", Colors.White, 0.1f);

        // Future: dust particles, sound effects
    }
}

public class InterpolatedPositionStrategy : IVisualPositionStrategy
{
    public void UpdateVisualPosition(Node2D node, Position target, float duration)
    {
        var tween = node.CreateTween();
        tween.TweenProperty(node, "position", GridToPixel(target), duration);
    }
}
```

### Strategy Selection

```csharp
public class ActorView : Node2D
{
    private readonly IVisualPositionStrategy _moveStrategy;

    public ActorView()
    {
        // Actors use discrete to prevent clipping
        _moveStrategy = new DiscretePositionStrategy();
    }
}

public class ProjectileView : Node2D
{
    private readonly IVisualPositionStrategy _moveStrategy;

    public ProjectileView()
    {
        // Projectiles use interpolation for smooth flight
        _moveStrategy = new InterpolatedPositionStrategy();
    }
}
```

### Benefits of Dual-Mode Support

1. **Flexibility**: Choose per entity type
2. **Bug Prevention**: Discrete mode eliminates clipping
3. **Polish Options**: Can mix modes for different effects
4. **Future-Proof**: Easy to switch strategies later

### Migration Path

1. **Phase 1**: Convert actors to discrete (fixes TD_062)
2. **Phase 2**: Keep projectiles interpolated
3. **Phase 3**: Add step animations when sprites ready
4. **Phase 4**: Per-entity strategy configuration

This amendment makes the Temporal Decoupling Pattern more robust and flexible while maintaining its core architectural benefits.

## Notes

This pattern is widely used in game development:
- **Unity/Unreal**: "Gameplay Position" vs "Visual Position"
- **Multiplayer Games**: "Authoritative Position" vs "Predicted Position" vs "Interpolated Position"
- **RTS Games**: Fog of war controllers separate from unit positions
- **Roguelikes**: Discrete movement (NetHack, DCSS, Cogmind) vs interpolated (ToME4)

The pattern emerged from TD_061 (Progressive FOV Updates) but applies broadly across the codebase.

### Revision History

**2025-09-18 (Rev 1)**: Simplified from Three-Position Model to Two-Position Model based on user insight that FOV should be calculated from logical position with atomic updates. The simpler model achieves all architectural goals with better conceptual clarity.

**2025-09-18 (Rev 2)**: Renamed from "Temporal Decoupling Pattern" to "Logical-Visual Position Separation" for clarity. Updated content to emphasize the one-way event flow and strict separation of concerns.

## References

- Original discussion: TD_061 in Backlog.md
- Discrete movement decision: TD_062 in Backlog.md
- Implementation example: `IFogOfWarRevealService` (to be implemented)
- Pattern inspiration: [Client-Side Prediction](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)