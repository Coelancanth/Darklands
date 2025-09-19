# ADR-022: Two-Position Model (Domain Truth Pattern)

**Status**: Revised
**Date**: 2025-09-17 (Revised 2025-09-19)
**Decision Makers**: Tech Lead, Dev Engineer
**Tags**: `architecture` `state-management` `animation` `fog-of-war` `domain-truth`

## Revision Note (2025-09-19)

**This ADR has been significantly revised.** The original "Three-Position Model" with instant domain updates was architecturally flawed. After implementing TD_061 and discovering fundamental issues (12+ hours of debugging), we realized:

1. **The domain was lying** - Actors don't teleport, they move step-by-step
2. **Three positions were unnecessary** - We only need Logical (truth) and Visual (display)
3. **Complex timer services were a symptom** - The real problem was the lying domain model

See Post-Mortem: `/Docs/06-PostMortems/Inbox/2025-09-19-td061-presenter-handler-violation.md`

## Context

In tactical turn-based games, we need to model both the logical progression of game state and its visual representation. The initial problem arose with fog of war revealing the destination immediately while the actor was still visually moving, which revealed a fundamental misunderstanding: we were making the domain lie about reality.

**The key insight**: When an actor moves from (5,5) to (8,8), they don't teleport - they move through (6,6), (7,7), etc. The domain should model this truth.

This creates a clear separation:
- **Domain logic** models what actually happens (step-by-step progression)
- **Visual representation** displays this to players (can be instant or animated)
- **Clean Architecture** keeps these concerns properly separated

## Decision

We will implement a **Two-Position Model** that properly separates logical truth from visual representation:

1. **Logical Position** - The actual, authoritative position in the domain (progresses step-by-step)
2. **Visual Position** - The displayed position in the UI (follows logical position)

**Critical Principle**: The domain must model truth. If an actor moves from (5,5) to (8,8), they actually move through each intermediate position. The domain should reflect this reality, not lie about it with instant teleportation.

### Implementation Pattern

```csharp
// 1. Domain Layer - Models the truth (step-by-step movement)
public class Actor
{
    public Position Position { get; private set; } // Logical Position - THE truth
    public Path? ActivePath { get; private set; }

    public void StartMovement(Path path)
    {
        ActivePath = path;
        RaiseDomainEvent(new MovementStartedEvent(Id, path));
    }

    public void AdvanceMovement()
    {
        if (ActivePath == null) return;

        Position = ActivePath.GetNext(); // Move one step
        RaiseDomainEvent(new ActorMovedEvent(Id, Position));

        if (ActivePath.IsComplete)
        {
            ActivePath = null;
            RaiseDomainEvent(new MovementCompletedEvent(Id));
        }
    }
}

// 2. Application Layer - Orchestrates domain events
public class ActorMovedHandler : INotificationHandler<ActorMovedEvent>
{
    public Task Handle(ActorMovedEvent evt)
    {
        // Calculate FOV for new position
        var fov = _fovCalculator.Calculate(evt.Position);
        _visionService.Update(evt.ActorId, fov);

        // Notify UI layer
        _uiEventBus.Publish(new UpdatePositionUIEvent(evt.ActorId, evt.Position, fov));
        return Task.CompletedTask;
    }
}

// 3. Presentation Layer - Visual representation
public class ActorView : Node2D
{
    public void OnPositionUpdate(UpdatePositionUIEvent evt)
    {
        // Visual can teleport instantly or animate smoothly
        Position = GridToWorld(evt.Position); // Instant visual update
        // OR: AnimateToPosition(evt.Position); // Smooth animation
    }
}
```

## Consequences

### Positive

- **Domain Truth**: Domain model reflects reality - actors really move step-by-step
- **Clean Separation**: Domain logic vs visual representation clearly separated
- **Event-Driven**: Natural event flow as position actually changes
- **Testable**: Domain movement testable without any UI
- **Save-Friendly**: Current position + optional path = complete state
- **Interrupt-Friendly**: Can cancel at any position (actor stays where they are)
- **FOV Correctness**: FOV updates naturally as position changes

### Negative

- **Perceived Complexity**: Seems more complex than instant teleport (but actually simpler)
- **Game Loop Required**: Need mechanism to advance movement over time
- **Initial Confusion**: Developers may expect instant movement

## Alternatives Considered (and Why They Failed)

### Alternative 1: Three-Position Model (Original Incorrect Approach)
Have instant domain updates with a separate "Revealed Position" for FOV.
- **Rejected**: Makes domain lie about reality, creates unnecessary complexity
- **Lesson**: This is what TD_061 tried and it led to 12+ hours of architectural issues

### Alternative 2: Animation-Driven Updates
Let animation callbacks trigger FOV updates.
- **Rejected**: Couples game logic to rendering, non-deterministic, violates Clean Architecture

### Alternative 3: Complex Timer Infrastructure
Create GameTimeService, MovementTimer, MovementProgressionService to fake progression.
- **Rejected**: Over-engineered solution for simple problem, domain still lies about state

### Alternative 4: Instant Domain Teleportation (What We Initially Built)
Actor instantly moves to destination, services fake intermediate positions.
- **Rejected**: Domain doesn't model truth, requires complex orchestration

## Godot Integration

### CRITICAL: Architectural Boundary Alignment

Per ADR-006 and ADR-010, the Two-Position Model must respect these boundaries:

1. **Logical Position** → Domain Layer (pure C#, progresses step-by-step)
2. **Event Handling** → Application Layer (handlers, not presenters!)
3. **Visual Position** → Presentation Layer (Godot display)

**WARNING**: Presenters must NOT implement INotificationHandler. They subscribe to UI events via UIEventBus (ADR-010).

### Simple Game Loop Integration

The Two-Position Model uses a simple game loop to advance movement:

```csharp
// APPLICATION LAYER: Simple game loop
public class GameLoop : IHostedService
{
    private Timer _timer;
    private readonly IActorRepository _actors;
    private readonly IMediator _mediator;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Tick every 200ms for movement progression
        _timer = new Timer(OnTick, null, 0, 200);
        return Task.CompletedTask;
    }

    private void OnTick(object? state)
    {
        // Advance all moving actors
        var movingActors = _actors.GetMovingActors();
        foreach (var actor in movingActors)
        {
            actor.AdvanceMovement();
        }

        // Dispatch domain events
        _mediator.DispatchPendingEvents();
    }
}

// No complex timer infrastructure needed!
// Domain events flow naturally as actors move
```

### Event Flow (Clean and Simple)

```csharp
// 1. Command starts movement
public class MoveActorCommandHandler
{
    public async Task<Fin<Unit>> Handle(MoveActorCommand command)
    {
        var path = _pathfinder.FindPath(actor.Position, command.Target);
        actor.StartMovement(path);
        return FinSucc(unit);
    }
}

// 2. Domain advances step-by-step
// Actor.AdvanceMovement() called by game loop
// Raises ActorMovedEvent for each step

// 3. Application layer handles domain event
public class ActorMovedHandler : INotificationHandler<ActorMovedEvent>
{
    public Task Handle(ActorMovedEvent evt)
    {
        var fov = _fovCalculator.Calculate(evt.Position);
        _uiEventBus.Publish(new UpdateFOVUIEvent(evt.ActorId, fov));
        return Task.CompletedTask;
    }
}

// 4. Visual Position → Godot Scene Graph (per ADR-016)
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

## Amendment 2: CallMethod Track Integration (2025-09-19)

Based on TD_065 implementation planning, the Two-Position Model explicitly supports Godot's CallMethod tracks for enhanced visual feedback without violating architectural boundaries.

### What Are CallMethod Tracks?

CallMethod tracks in Godot's AnimationPlayer allow animations to trigger methods at specific frames. They're perfect for adding polish and game feel while maintaining clean architecture.

### Architectural Alignment

CallMethod tracks fit perfectly into the Two-Position Model because they:
1. **Are Reactive**: Triggered BY animations, don't control game state
2. **Enhance Presentation**: Add polish without coupling to domain logic
3. **Respect Boundaries**: Stay within the Presentation layer
4. **Fire and Forget**: Don't wait for return values or block execution

### Integration Pattern

```csharp
// Presentation Layer - ActorView.cs
public partial class ActorView : Node2D
{
    private AnimationPlayer _animationPlayer;
    private bool _isAnimating;

    public override void _Ready()
    {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");

        // Animation setup with CallMethod tracks
        // Frame 0: Start of movement animation
        // Frame 10: CallMethod → OnFootPlant()
        // Frame 20: CallMethod → OnStepComplete()
    }

    // Called by CallMethod track at frame 10
    public void OnFootPlant()
    {
        // Visual/Audio feedback only - no domain updates!
        PlayFootstepSound();
        SpawnDustParticles();
        FlashTileHighlight();
    }

    // Called by CallMethod track at frame 20
    public void OnStepComplete()
    {
        // Mark animation complete for presentation layer
        _isAnimating = false;

        // Optional: Trigger arrival effects
        ShowArrivalFeedback();
    }

    private void PlayFootstepSound()
    {
        // Per ADR-006: Direct Godot audio usage in presentation
        var audioPlayer = GetNode<AudioStreamPlayer2D>("FootstepAudio");
        audioPlayer.Stream = GD.Load<AudioStream>("res://audio/footstep.ogg");
        audioPlayer.Play();
    }
}
```

### Animation Setup Example

```
AnimationPlayer Structure:
└─ Animation: "move_step" (200ms duration)
    ├─ Transform Track: position
    │   ├─ 0ms: Start position
    │   └─ 200ms: End position (one tile over)
    ├─ CallMethod Track: ActorView
    │   ├─ 50ms: OnFootLift()      # Prepare step
    │   ├─ 100ms: OnMidStep()       # Peak of movement
    │   ├─ 150ms: OnFootPlant()     # Contact with ground
    │   └─ 200ms: OnStepComplete()  # Ready for next
    └─ Property Track: modulate
        ├─ 150ms: Flash white (1.3x)
        └─ 200ms: Return to normal
```

### Approved Use Cases

✅ **Visual Effects**
```csharp
public void OnSpellImpact()
{
    SpawnParticles("res://effects/spell_impact.tscn");
    Camera.AddTrauma(0.2f);  // Screen shake
}
```

✅ **Audio Feedback**
```csharp
public void OnSwordSwing()
{
    AudioManager.PlaySfx("sword_whoosh");
}
```

✅ **Animation State Management**
```csharp
public void OnAnimationMidpoint()
{
    _currentFrame = AnimationFrame.Middle;
    _canInterrupt = true;  // Allow animation canceling
}
```

✅ **Visual Polish**
```csharp
public void OnFootstep()
{
    // Spawn dust at current position
    var dust = DustScene.Instantiate<CPUParticles2D>();
    dust.GlobalPosition = GlobalPosition;
    dust.Emitting = true;
    GetTree().CurrentScene.AddChild(dust);
}
```

### Prohibited Use Cases

❌ **Domain State Updates**
```csharp
// NEVER DO THIS!
public void OnAttackHit()
{
    _actor.Health -= 10;  // Domain logic in view!
    _mediator.Send(new DamageCommand());  // Commands from view!
}
```

❌ **Service Calls**
```csharp
// NEVER DO THIS!
public void OnMovementComplete()
{
    _gridService.UpdatePosition();  // Service calls from animation!
    _fogOfWarService.Recalculate();  // Coupling to application layer!
}
```

❌ **Game Logic Control**
```csharp
// NEVER DO THIS!
public void OnAnimationEnd()
{
    GameManager.EndTurn();  // Animation controlling game flow!
    NextPlayer.StartTurn();  // View driving game logic!
}
```

### Benefits of CallMethod Track Integration

1. **Precise Timing**: Effects trigger at exact animation frames
2. **Decoupled Polish**: Add game feel without architectural violations
3. **Designer-Friendly**: Animators can adjust timing in Godot editor
4. **Performance**: More efficient than polling animation state
5. **Maintainable**: Effects clearly tied to specific animation moments

### Implementation Guidelines

1. **Naming Convention**: Prefix CallMethod track methods with "On"
   - `OnFootPlant()`, `OnSwordContact()`, `OnSpellRelease()`

2. **Keep Methods Small**: Each should do one specific thing
   ```csharp
   public void OnStepComplete()
   {
       _isAnimating = false;  // Single responsibility
   }
   ```

3. **No Return Values**: CallMethod tracks ignore return values
   ```csharp
   public void OnEffect()  // void return only
   {
       // Effects here
   }
   ```

4. **Thread Safety**: CallMethod tracks run on main thread
   ```csharp
   public void OnParticleSpawn()
   {
       // Safe to modify scene tree
       AddChild(particleInstance);
   }
   ```

### Testing Considerations

```csharp
[Test]
public void CallMethodTracks_DoNotUpdateDomain()
{
    // Verify view methods don't modify domain state
    var view = new ActorView();
    var initialState = GetDomainState();

    view.OnFootPlant();
    view.OnStepComplete();

    var finalState = GetDomainState();
    Assert.AreEqual(initialState, finalState);
}
```

### Migration from TD_061

TD_061's complex timer infrastructure tried to coordinate animation with game state. With CallMethod tracks:

**Before (TD_061 - Complex)**:
```csharp
// Timer service trying to sync animation with FOV
_timerService.OnTick += () => {
    UpdatePosition();
    if (AnimationAtFrame(10)) PlaySound();
    if (AnimationComplete()) UpdateFOV();
};
```

**After (TD_065 with CallMethod - Simple)**:
```csharp
// Animation plays, triggers methods at specific frames
public void OnFootPlant() => PlaySound();
public void OnStepComplete() => _isAnimating = false;
// FOV updates from domain events, not animation!
```

### Key Principle

CallMethod tracks are for **enhancement**, not **control**:
- Domain advances state (truth)
- Events notify of changes
- Animations visualize changes
- CallMethod tracks enhance visualization
- They NEVER feed back to domain

This maintains the core principle of the Two-Position Model: domain tells truth, presentation follows.

## Key Lessons from This Revision

1. **Domain models must tell truth** - If actors move step-by-step in reality, model it that way
2. **Complexity is a smell** - Complex timer services were a symptom of wrong architecture
3. **Events should flow naturally** - When domain position changes, events fire naturally
4. **Presenters are not handlers** - They subscribe to UI events, not domain notifications
5. **Two positions are enough** - Logical (truth) and Visual (display)

## Notes

This pattern is standard in professional game development:
- **Battle Brothers/XCOM**: Authoritative game state with visual feedback
- **Multiplayer Games**: Server authoritative + client interpolation
- **The key principle**: Domain models reality, visuals follow

The original Three-Position Model was our mistake. The corrected Two-Position Model aligns with industry standards.

## References

- Post-Mortem: `/Docs/06-PostMortems/Inbox/2025-09-19-td061-presenter-handler-violation.md`
- Correct Implementation: TD_065 in Backlog.md
- Original (failed) attempt: TD_061 (12+ hours of issues)
- Pattern standard: Every professional tactical game uses this approach