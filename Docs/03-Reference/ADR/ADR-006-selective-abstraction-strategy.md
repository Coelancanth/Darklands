# ADR-006: Selective Abstraction Strategy

**Status**: Accepted  
**Date**: 2025-09-08  
**Author**: Tech Lead  
**Deciders**: Tech Lead, Dev Engineer  

## Context

Darklands uses Clean Architecture with Godot, creating a fundamental tension:
- **Clean Architecture** demands framework independence through abstractions
- **Godot** provides excellent built-in features we want to use
- **Over-abstraction** creates unnecessary complexity and slows development
- **Under-abstraction** creates untestable, coupled code

For a Battle Brothers-scale tactical game, we need a pragmatic approach that balances architectural purity with development velocity. Not every Godot feature needs an abstraction layer.

### The Abstraction Spectrum

```csharp
// OVER-ABSTRACTION (Bad): Wrapping everything
public interface ILabelService  // ❌ Unnecessary
{
    void SetText(string nodeId, string text);
}

// UNDER-ABSTRACTION (Bad): Domain knows about Godot
public class Actor  // ❌ Coupled
{
    private AudioStreamPlayer2D _audio;  // Godot in domain!
    public void Attack() { _audio.Play(); }
}

// SELECTIVE ABSTRACTION (Good): Abstract only what matters
public interface IAudioService  // ✅ Needed for testing
{
    void PlaySound(SoundId sound, CoreVector2? position = null);  // Core value type, not Godot
}
public partial class ActorView : Node2D  // ✅ View can use Godot directly
{
    GetNode<Label>("HealthLabel").Text = "100";  // Direct is fine here
}
```

## Decision

We will implement **Selective Abstraction** based on clear criteria:

### Abstraction Decision Matrix

| System | Abstract? | Why | Implementation |
|--------|-----------|-----|----------------|
| **Audio** | ✅ YES | Testing, platform differences | IAudioService |
| **Input** | ✅ YES | Remapping, replay, platforms | IInputService |
| **Save/Load** | ✅ YES | Complex, needs testing | ISaveService |
| **Random** | ✅ YES | Determinism critical | IDeterministicRandom |
| **Time/Clock** | ✅ YES | Determinism, replay, testing | IGameClock |
| **Localization** | ✅ YES | Testing, modding | ILocalizationService |
| **Resources** | ✅ YES | Data-driven design | IResourceLoader |
| **Settings** | ✅ YES | Platform differences | ISettingsService |
| **World Hydration** | ✅ YES | Build scene graph from state safely | IWorldHydrator |
| **Mod Extensions** | ✅ YES | Extensible data attachment | IModExtensionRegistry |
| **UI Controls** | ❌ NO | Already in View layer | Direct Godot |
| **Animations** | ❌ NO | Presentation only | Direct Godot |
| **Particles** | ❌ NO | Visual only | Direct Godot |
| **Scene Loading** | ❌ NO | Godot-specific | Direct Godot |
| **Tweens** | ❌ NO | Presentation only | Direct Godot |

### Reviewer Addendum (2025-09-08)

> Reviewer: This ADR is pragmatic and aligns with the handbook. To lock it down:
> - Ensure Core interfaces avoid Godot types; use Core value objects/DTOs (e.g., `CoreVector2`) and map at the Infra/Presentation boundary.
> - Add architecture tests: forbid `Godot.*` in `src/Darklands.Core.*` and prohibit Core/Application from referencing Presentation assemblies.
> - Include “Time/Clock” abstraction in the decision matrix for determinism and replay.
> - Provide adapter examples converting Core DTOs to Godot types.

### The Four-Layer Abstraction Rules

```csharp
// LAYER 1: Domain - ZERO Godot (Non-negotiable)
namespace Darklands.Core.Domain
{
    public sealed class CombatCalculator
    {
        // ✅ Pure C#, no framework knowledge
        public CombatResult Calculate(Actor attacker, Actor target)
        {
            return new CombatResult(/* pure data */);
        }
    }
}

// LAYER 2: Application - ZERO Godot (Non-negotiable)
namespace Darklands.Core.Application
{
    public sealed class AttackCommandHandler
    {
        private readonly IAudioService _audio;  // ✅ Interface only
        private readonly IGameClock _clock;     // ✅ Deterministic time source (see below)
        
        public Task<Fin<Unit>> Handle(AttackCommand command)
        {
            // Uses interfaces, never Godot directly
            _audio.PlaySound(SoundId.Attack);
            var now = _clock.CurrentTurn; // Deterministic, replayable time
        }
    }
}

// LAYER 3: Infrastructure - Bridges to Godot (Selective)
namespace Darklands.Core.Infrastructure
{
    // ✅ ABSTRACT: High-value service
    public sealed class GodotAudioService : IAudioService
    {
        private readonly AudioStreamPlayer2D _player;  // Godot here
        
        public void PlaySound(SoundId sound, CoreVector2? position = null)
        {
            var stream = GD.Load<AudioStream>($"res://sounds/{sound}.ogg");
            _player.Stream = stream;
            if (position.HasValue) _player.Position = new Vector2(position.Value.X, position.Value.Y);
            _player.Play();
        }
    }
    
    // ❌ DON'T ABSTRACT: Low-value wrapper
    // Just use Godot's SceneTree directly in Views!
}

// LAYER 4: Presentation - Full Godot (Direct usage encouraged)
namespace Darklands.Presentation
{
    public partial class ActorView : Node2D
    {
        public override void _Ready()
        {
            // ✅ Direct Godot usage in presentation layer
            var label = GetNode<Label>("HealthLabel");
            label.Text = "100 HP";
            label.Modulate = Colors.Red;
            
            // ✅ Direct tween usage
            var tween = CreateTween();
            tween.TweenProperty(this, "position", new Vector2(100, 100), 0.5f);
            
            // ✅ Direct particle usage
            GetNode<GpuParticles2D>("HitEffect").Emitting = true;
        }
    }
}
```

### Abstraction Criteria Checklist

**Abstract when ANY of these are true:**
1. ✅ **Testing Required**: Domain/Application logic needs unit tests
2. ✅ **Platform Variance**: Different implementations per platform
3. ✅ **Determinism Required**: Must be reproducible (random, time)
4. ✅ **Save/Load Affected**: Part of persistent game state
5. ✅ **Modding Hook**: Modders need to replace/extend
6. ✅ **Complex State**: Non-trivial logic that needs isolation

**DON'T Abstract when ALL of these are true:**
1. ❌ **Presentation Only**: Pure visual/UI concern
2. ❌ **Godot-Specific**: Makes no sense outside Godot
3. ❌ **Simple Pass-Through**: Wrapper adds no value
4. ❌ **Rarely Changes**: Stable, unlikely to need swapping
5. ❌ **No Testing Benefit**: Nothing to test in isolation

### Practical Examples

```csharp
// ✅ GOOD: Audio needs abstraction for testing
public interface IAudioService
{
    void PlaySound(SoundId sound, CoreVector2? position = null);
    void SetMusicTrack(MusicId track);
    void SetBusVolume(AudioBus bus, float volume);
}

// Usage in tests
[Test]
public void Combat_PlaysCorrectSounds()
{
    var audioMock = new Mock<IAudioService>();
    var combat = new CombatService(audioMock.Object);
    
    combat.ExecuteAttack(attacker, target);
    
    audioMock.Verify(a => a.PlaySound(SoundId.SwordHit, It.IsAny<CoreVector2?>()));
}

// ❌ BAD: Label wrapper adds no value
public interface ILabelService  // Don't do this!
{
    void SetLabelText(string path, string text);
    void SetLabelColor(string path, Color color);
}
// Just use GetNode<Label>() directly in Views!

// ✅ GOOD: Input needs abstraction for remapping and replay
public interface IInputService
{
    bool IsActionPressed(InputAction action);
    CoreVector2 GetMousePosition();
    IObservable<InputEvent> InputEvents { get; }
}

// ✅ GOOD: Deterministic clock abstraction for simulation/replay
public interface IGameClock
{
    ulong CurrentTurn { get; }
    void AdvanceTurns(ulong by);
}

// ❌ BAD: Tween wrapper is pointless
public interface ITweenService  // Don't do this!
{
    void TweenProperty(Node node, string property, Variant value, float duration);
}
// Just use CreateTween() directly in Views!

// ✅ GOOD: Settings need abstraction for platform differences
public interface ISettingsService
{
    T Get<T>(SettingKey<T> key);
    void Set<T>(SettingKey<T> key, T value);
    void Save();
}

// ❌ BAD: Particle wrapper serves no purpose
public interface IParticleService  // Don't do this!
{
    void EmitParticles(string particlePath, Vector2 position);
}
// Just use GpuParticles2D directly in Views!
```

### Migration Strategy for Existing Code

```csharp
// BEFORE: Over-abstracted
public interface INodeService
{
    Node GetNode(string path);
    void AddChild(Node parent, Node child);
    void RemoveChild(Node parent, Node child);
}

// AFTER: Remove unnecessary abstraction
// Just use Godot's Node API directly in Views!
public partial class GameView : Control
{
    public override void _Ready()
    {
        var healthBar = GetNode<ProgressBar>("UI/HealthBar");  // Direct!
        AddChild(new Label());  // Direct!
    }
}

// BEFORE: Under-abstracted
public class Actor  // Domain class
{
    private AudioStreamPlayer _audio;  // ❌ Godot in domain!
    public void Attack() { _audio.Play(); }
}

// AFTER: Properly abstracted
public class Actor  // Domain class
{
    // Pure domain, no Godot
}

public class AttackPresenter  // Presentation layer
{
    private readonly IAudioService _audio;  // ✅ Abstracted service
    public void PresentAttack() { _audio.PlaySound(SoundId.Attack); }
}
```

## Implementation Guidelines

### 1. Service Registration in GameStrapper

```csharp
public static class GameStrapper
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // ✅ Register high-value abstractions
        services.AddSingleton<IAudioService, GodotAudioService>();
        services.AddSingleton<IInputService, GodotInputService>();
        services.AddSingleton<ISaveService, SaveService>();
        services.AddSingleton<IDeterministicRandom, DeterministicRandom>();
        services.AddSingleton<ILocalizationService, GodotLocalizationService>();
        services.AddSingleton<ISettingsService, GodotSettingsService>();
        
        // ❌ DON'T register low-value wrappers
        // No ILabelService, ITweenService, IParticleService, etc.
    }
}
```

### 2. View Layer Best Practices

```csharp
public partial class CombatView : Control
{
    private IMediator? _mediator;  // ✅ Injected service
    private IAudioService? _audio;  // ✅ Injected service
    
    public override void _Ready()
    {
        // Get injected services
        _mediator = GameStrapper.GetService<IMediator>();
        _audio = GameStrapper.GetService<IAudioService>();
        
        // Use Godot directly for UI
        var damageLabel = GetNode<Label>("DamagePopup");  // ✅ Direct
        damageLabel.Text = "100";  // ✅ Direct
        
        var tween = CreateTween();  // ✅ Direct
        tween.TweenProperty(damageLabel, "modulate:a", 0.0f, 1.0f);  // ✅ Direct
        
        GetNode<AnimationPlayer>("AnimationPlayer").Play("hit");  // ✅ Direct
    }
    
    private void OnAttackExecuted(AttackResult result)
    {
        _audio?.PlaySound(SoundId.Hit);  // ✅ Through abstraction
        ShowDamageNumber(result.Damage);  // ✅ Direct UI manipulation
    }
}
```

### 3. Testing Strategy

```csharp
// Domain/Application tests use mocks
[Test]
public void CombatService_WithValidAttack_PlaysSound()
{
    var audio = new Mock<IAudioService>();
    var combat = new CombatService(audio.Object);
    
    combat.ExecuteAttack(testAttacker, testTarget);
    
    audio.Verify(a => a.PlaySound(It.IsAny<SoundId>(), It.IsAny<CoreVector2?>()));
}

// View tests are minimal or skipped
// (UI testing through Godot is expensive and brittle)
```

// Architecture tests (example using NetArchTest)
[Test]
public void Core_ShouldNotReference_Godot()
{
    var result = Types.InAssembly(typeof(Darklands.Core.Domain.Actor).Assembly)
        .Should().NotHaveDependencyOn("Godot")
        .GetResult();
    result.IsSuccessful.Should().BeTrue();
}

[Test]
public void Core_Application_ShouldNotReference_Presentation()
{
    var core = Types.InAssembly(typeof(Darklands.Core.Domain.Actor).Assembly)
        .Should().NotHaveDependencyOn("Darklands.Presentation").GetResult();
    var app = Types.InAssembly(typeof(Darklands.Core.Application.AttackCommandHandler).Assembly)
        .Should().NotHaveDependencyOn("Darklands.Presentation").GetResult();
    core.IsSuccessful.Should().BeTrue();
    app.IsSuccessful.Should().BeTrue();
}

## Consequences

### Positive

1. **Faster Development**: No unnecessary abstraction layers
2. **Cleaner Code**: Less indirection where it doesn't add value
3. **Better Performance**: Fewer layers = less overhead
4. **Easier Debugging**: Direct calls are easier to trace
5. **Pragmatic Testing**: Test what matters, skip what doesn't
6. **Godot Integration**: Use Godot's features as intended

### Negative

1. **Requires Judgment**: Developers must decide when to abstract
2. **Inconsistency Risk**: Different developers might choose differently
3. **Refactoring Cost**: Wrong abstraction decision requires rework
4. **Documentation Need**: Must document why each choice was made

## Decision Examples

### Audio: ABSTRACT ✅
- **Why**: Need to mock in tests, platform audio differences, sound mods

### Particles: DON'T ABSTRACT ❌
- **Why**: Pure visual, no logic to test, Godot-specific

### Save System: ABSTRACT ✅
- **Why**: Complex logic, needs extensive testing, platform differences

### UI Labels: DON'T ABSTRACT ❌
- **Why**: Already in view layer, no logic, Godot-specific

### Input: ABSTRACT ✅
- **Why**: Remapping, recording, platform differences, testing

### Scene Transitions: DON'T ABSTRACT ❌
- **Why**: Godot-specific, presentation concern, minimal logic

## Anti-Patterns to Avoid

```csharp
// ❌ ANTI-PATTERN: Anemic abstraction
public interface IVector2Service
{
    Vector2 Add(Vector2 a, Vector2 b);
    float Distance(Vector2 a, Vector2 b);
}

// ❌ ANTI-PATTERN: Leaky abstraction
public interface IAudioService
{
    AudioStreamPlayer2D GetPlayer();  // Exposes Godot type!
}

// ❌ ANTI-PATTERN: God interface
public interface IGodotService
{
    void PlaySound(string sound);
    void LoadScene(string scene);
    void ShowParticles(string particles);
    void CreateTween(Node node);
    // ... 100 more methods
}

// ❌ ANTI-PATTERN: Abstraction for the sake of it
public interface IHealthBarService
{
    void SetHealth(int current, int max);
}
// Just update the ProgressBar directly!
```

## References

- [Selective Abstraction](https://martinfowler.com/bliki/PreferenceForAbstraction.html) - Martin Fowler
- [YAGNI Principle](https://martinfowler.com/bliki/Yagni.html) - You Aren't Gonna Need It
- [Pragmatic Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Godot Best Practices](https://docs.godotengine.org/en/stable/tutorials/best_practices/) - Official Godot docs