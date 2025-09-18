# ADR-022: Logical-Visual Position Separation Pattern

**Status**: Accepted (Clarified)
**Date**: 2025-09-17 (Clarified 2025-09-18)
**Decision Makers**: Tech Lead, Dev Engineer, User
**Tags**: `architecture` `state-management` `fog-of-war` `two-position-model` `event-driven` `deterministic`

## Context

In tactical turn-based games, we frequently encounter scenarios where game logic needs to advance incrementally (for fog of war, vision calculations) while visual representation follows. The initial problem arose with fog of war revealing the destination immediately while the actor was still moving.

This creates a fundamental tension:
- **Game logic** needs deterministic, cell-by-cell progression for FOV and combat calculations
- **Player experience** needs clear visual feedback of position changes
- **Clean Architecture** demands these concerns remain separated

## Decision

We will separate entity positions into exactly TWO distinct concerns:

1. **Logical Position** - The single authoritative position that advances cell-by-cell on a timer (used for ALL game mechanics: FOV, collision, combat, saves)
2. **Visual Position** - The sprite display location that teleports to match logical position (pure cosmetic feedback)

**Core Principle**: The Logical Position IS the authoritative position for everything. There is no "destination position" in game state - only the current logical position and the path being traversed.

**Event Flow**:
- Logical position advances → Publishes event → Visual position responds
- Direction is ALWAYS: Application Layer → Presentation Layer (never reverse)

### Implementation Pattern

```csharp
// 1. Domain Layer - Movement progression (THE authoritative position)
public class MovementProgression
{
    public Position CurrentPosition { get; private set; }  // THE authoritative position
    public IReadOnlyList<Position> RemainingPath { get; private set; }
    private int _elapsedMs;
    private const int MillisecondsPerCell = 200;

    public MovementProgression(Position startPos, IReadOnlyList<Position> path)
    {
        CurrentPosition = startPos;
        RemainingPath = path;
        _elapsedMs = 0;
    }

    public Option<Position> AdvanceTime(int milliseconds)
    {
        if (!RemainingPath.Any()) return None;

        _elapsedMs += milliseconds;
        if (_elapsedMs >= MillisecondsPerCell)
        {
            // Move to next cell in path
            CurrentPosition = RemainingPath[0];
            RemainingPath = RemainingPath.Skip(1).ToList();
            _elapsedMs = 0;
            return Some(CurrentPosition);
        }
        return None;
    }
}

// 2. Application Layer - Movement service managing progression
public interface IMovementProgressionService
{
    void StartMovement(ActorId actor, IReadOnlyList<Position> path);
    void CancelMovement(ActorId actor);  // For ESC or new destination
    void AdvanceGameTime(int milliseconds);
    Position GetCurrentPosition(ActorId actor);  // THE authoritative position
}

public class MovementProgressionService : IMovementProgressionService
{
    private readonly Dictionary<ActorId, MovementProgression> _activeProgressions = new();
    private readonly IEventBus _eventBus;
    private readonly IFogOfWarService _fogOfWar;

    public void StartMovement(ActorId actor, IReadOnlyList<Position> path)
    {
        // Get actor's current position (not destination!)
        var currentPos = GetCurrentPosition(actor);

        // Cancel any existing movement
        CancelMovement(actor);

        // Start new progression from current position
        _activeProgressions[actor] = new MovementProgression(currentPos, path);
    }

    public void CancelMovement(ActorId actor)
    {
        if (_activeProgressions.ContainsKey(actor))
        {
            _activeProgressions.Remove(actor);
            // Actor stays at their current position - no state change needed
        }
    }

    public void AdvanceGameTime(int milliseconds)
    {
        foreach (var (actorId, progression) in _activeProgressions.ToList())
        {
            progression.AdvanceTime(milliseconds)
                .IfSome(newPos =>
                {
                    // Update THE authoritative position
                    UpdateActorPosition(actorId, newPos);

                    // Update FOV from new position
                    _fogOfWar.UpdateVision(actorId, newPos);

                    // Notify presentation layer
                    _eventBus.Publish(new ActorPositionChangedEvent(actorId, newPos));

                    // Remove completed movements
                    if (!progression.RemainingPath.Any())
                    {
                        _activeProgressions.Remove(actorId);
                    }
                });
        }
    }

    public Position GetCurrentPosition(ActorId actor)
    {
        // Return THE authoritative position from domain
        return _actorRepository.Get(actor).Position;
    }
}

// 3. Presentation Layer - Visual teleports to match logical
public class ActorView : Node2D
{
    public void OnLogicalPositionChanged(Position newLogicalPos)
    {
        // Visual sprite teleports instantly to logical position
        Position = GridToWorld(newLogicalPos);

        // Optional: Add brief visual feedback for the teleport
        Modulate = Colors.White * 1.2f;
        CreateTween().TweenProperty(this, "modulate", Colors.White, 0.1f);
    }
}
```

## Consequences

### Positive

- **Clean Separation**: Logical and visual concerns completely decoupled
- **Deterministic**: Logical position advances on fixed timer, reproducible
- **Save-Friendly**: Only save current position and optional movement state
- **Testable**: Can test all game logic without any visual system
- **Intuitive**: Two positions match tactical board game mental model
- **Performance**: FOV updates at controlled intervals, not every frame
- **Interrupt-Friendly**: ESC or new command simply cancels current movement
- **No Ambiguity**: Actor is ALWAYS at their logical position for all game rules

### Negative

- **Discrete Movement**: Visual updates are cell-by-cell (intentional design choice)
- **Timer Management**: Need to track elapsed time for progressions
- **No Smooth Animation**: Teleport style may feel less polished initially (but prevents clipping)

## Alternatives Considered

### Alternative 1: Visual Position Coupling
Update FOV based on sprite's visual position as it animates.
- **Rejected**: Non-deterministic, frame-rate dependent, untestable

### Alternative 2: Instant Destination Update
Update actor position to destination immediately when movement starts.
- **Rejected**: Actor could be attacked at destination before arriving, breaks tactical rules

### Alternative 3: Instant FOV Updates
Update FOV for entire path when movement command issued.
- **Rejected**: Reveals entire path before actor moves there, breaks fog of war

### Alternative 4: Complex Three-Position Model
Track separate "authoritative", "revealed", and "visual" positions.
- **Rejected**: Over-engineered. Actor IS where they are, not where they're going

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
// 1. Movement Command → Start Progression (NOT instant move!)
public class MoveActorCommandHandler
{
    public async Task<Fin<Unit>> Handle(MoveActorCommand command)
    {
        // Validate path
        var path = _pathfinding.FindPath(actor.Position, command.Destination);
        if (path.IsEmpty) return Fail("No valid path");

        // Start movement progression (actor stays at current position!)
        _movementService.StartMovement(actor.Id, path);

        // Notify presentation layer that movement started
        _eventBus.Publish(new MovementStartedEvent(actor.Id, path));

        return Unit.Default;
    }
}

// 2. Position advances cell-by-cell → FOV Updates
public class MovementProgressionService
{
    public void AdvanceGameTime(int gameMs)
    {
        // As shown earlier - advances logical position incrementally
        // Each position change triggers FOV update
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

### Movement Cancellation (ESC Pattern)

```csharp
// Handle ESC or clicking new destination during movement
public class GridPresenter : EventAwarePresenter
{
    private void OnEscPressed()
    {
        // Simply cancel current movement
        _movementService.CancelMovement(_currentActorId);
        // Actor stays at their current logical position
    }

    private void OnTileClicked(Position clickedPos)
    {
        if (_movementService.HasActiveMovement(_currentActorId))
        {
            // Cancel and start new movement from current position
            var currentPos = _movementService.GetCurrentPosition(_currentActorId);
            var newPath = _pathfinding.FindPath(currentPos, clickedPos);
            _movementService.StartMovement(_currentActorId, newPath);
        }
        else
        {
            // Normal movement command
            _mediator.Send(new MoveActorCommand(_currentActorId, clickedPos));
        }
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

### Challenge 4: Save During Movement
**Problem**: Need to save mid-movement state
**Solution**: Save current position and remaining path
```csharp
public class SaveData
{
    public Position CurrentPosition { get; set; }  // THE authoritative position
    public MovementSaveState? ActiveMovement { get; set; }  // Optional
}

public class MovementSaveState
{
    public List<Position> RemainingPath { get; set; }
    public int ElapsedMs { get; set; }
    // Visual position NOT saved - just teleports to current on load
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
- **ADR-010**: UI Event Bus - Events coordinate between the two positions
- **ADR-016**: Embrace Scene Graph - Visual elements use parent-child relationships
- **ADR-018**: Godot DI Lifecycle - Services properly scoped, GameTimeDriver as autoload
- **ADR-021**: MVP Separation - Presenters bridge game logic to Godot views

## Visual Movement Strategy

Based on TD_062 analysis, we use discrete (teleport) movement for actors to prevent sprite clipping:

### Our Chosen Approach: Discrete Movement
- Visual position teleports instantly to match logical position
- No interpolation = no diagonal clipping possible
- Clear cell-by-cell progression visible to player
- Matches classic tactical games (original X-COM, early Fire Emblem)

### Implementation

```csharp
public partial class ActorView : Node2D
{
    public void OnPositionChanged(ActorPositionChangedEvent evt)
    {
        // Teleport to new position
        Position = GridToPixel(evt.NewPosition);

        // Visual feedback separate from movement
        PlayMovementFeedback();
    }

    private void PlayMovementFeedback()
    {
        // Brief flash to show movement occurred
        Modulate = Colors.White * 1.2f;
        CreateTween().TweenProperty(this, "modulate", Colors.White, 0.1f);
    }
}
```

### Why Discrete Movement?

1. **No Clipping**: Eliminates diagonal sprite clipping (TD_062)
2. **Clear State**: Actor is always at a discrete cell
3. **Simpler Code**: No interpolation math or edge cases
4. **Classic Feel**: Matches tactical game expectations

## Notes

This pattern is widely used in game development:
- **Unity/Unreal**: "Gameplay Position" vs "Visual Position"
- **Multiplayer Games**: "Authoritative Position" vs "Predicted Position" vs "Interpolated Position"
- **RTS Games**: Fog of war controllers separate from unit positions
- **Roguelikes**: Discrete movement (NetHack, DCSS, Cogmind) vs interpolated (ToME4)

The pattern emerged from TD_061 (Progressive FOV Updates) but applies broadly across the codebase.

### Revision History

**2025-09-18 (Rev 1)**: Initial attempt incorrectly suggested three positions due to misleading code example showing `actor.MoveTo(destination)`. This was architectural astronautics.

**2025-09-18 (Rev 2)**: Clarified that there are exactly TWO positions: the logical position (which progresses cell-by-cell and IS the authoritative position) and the visual position (which teleports to match). Removed confusing "destination as authoritative" concept - actors are where they ARE, not where they're GOING.

**2025-09-18 (Rev 3)**: Added ESC cancellation pattern, clarified save/load behavior, and emphasized discrete (teleport) movement to prevent sprite clipping.

## References

- Original discussion: TD_061 in Backlog.md
- Discrete movement decision: TD_062 in Backlog.md
- Implementation example: `IFogOfWarRevealService` (to be implemented)
- Pattern inspiration: [Client-Side Prediction](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)