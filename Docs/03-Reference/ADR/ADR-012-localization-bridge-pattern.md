# ADR-012: Localization Bridge Pattern

**Status**: Proposed  
**Date**: 2025-09-08  
**Author**: Tech Lead  
**Deciders**: Tech Lead, Dev Engineer  

## Context

Darklands requires comprehensive localization support for:
- UI text (menus, dialogs, tooltips)
- Game content (actor names, item descriptions, ability text)
- System messages (errors, notifications, combat feedback)
- Dynamic text with parameters (e.g., "{actor} deals {damage} damage")

Godot provides a robust localization system through TranslationServer with:
- CSV import for translation data
- Runtime language switching
- Automatic fallback chains
- Built-in UI control integration (Label.text auto-translates)

However, this creates an architectural challenge similar to ADR-011: How do we leverage Godot's localization without violating Clean Architecture principles where Domain and Application layers must remain framework-agnostic?

### The Architectural Challenge

```
Domain Layer (Pure C#)          ← Must NOT know about Godot/TranslationServer
    ↑
Application Layer (CQRS)        ← Must NOT know about Godot/TranslationServer  
    ↑
Infrastructure Layer            ← CAN bridge Godot localization to Domain
    ↑
Presentation Layer (Godot)      ← Full TranslationServer integration
```

### Key Requirements

1. **Domain Purity**: Business logic generates message keys, not translated text
2. **Type Safety**: Compile-time checking for translation keys and parameters
3. **Testability**: Domain/Application tests without Godot dependencies
4. **Designer-Friendly**: Non-programmers can edit translations
5. **Hot-Reload**: Translations update without restart during development
6. **Performance**: Minimal overhead for frequent lookups

## Decision

We will implement a **Localization Bridge Pattern** that mirrors ADR-011's approach:

1. **Domain Layer** works with translation keys and structured messages
2. **Infrastructure Layer** bridges to Godot's TranslationServer
3. **Presentation Layer** handles final text rendering with Godot controls
4. **Translation data** lives in CSV files imported by Godot

### Core Pattern

```csharp
// Domain Layer - Pure C#, no localization implementation
namespace Darklands.Core.Domain.Localization
{
    // Strongly-typed translation keys prevent typos
    public sealed record TranslationKey(string Value)
    {
        // Common keys as constants for compile-time safety
        public static readonly TranslationKey AttackSuccess = new("combat.attack.success");
        public static readonly TranslationKey AttackMiss = new("combat.attack.miss");
        public static readonly TranslationKey ActorDefeated = new("combat.actor.defeated");
        public static readonly TranslationKey TurnStart = new("combat.turn.start");
    }

    // Messages with parameters for dynamic text
    public sealed record LocalizedMessage(
        TranslationKey Key,
        IReadOnlyDictionary<string, object>? Parameters = null
    )
    {
        public static LocalizedMessage Create(TranslationKey key) => new(key);
        
        public static LocalizedMessage Create(TranslationKey key, params (string key, object value)[] parameters)
        {
            var dict = parameters.ToDictionary(p => p.key, p => p.value);
            return new(key, dict);
        }
    }

    // Domain events include localized messages, not text
    public sealed record AttackExecutedEvent(
        ActorId AttackerId,
        ActorId TargetId,
        Damage DamageDealt,
        LocalizedMessage Message
    ) : INotification;
}

// Application Layer - Generates messages, doesn't translate
namespace Darklands.Core.Application.Commands
{
    public sealed class ExecuteAttackCommandHandler : ICommandHandler<ExecuteAttackCommand>
    {
        public async Task<Fin<Unit>> Handle(ExecuteAttackCommand command, CancellationToken ct)
        {
            // Domain logic generates message keys, not text
            var message = attack.IsHit
                ? LocalizedMessage.Create(
                    TranslationKey.AttackSuccess,
                    ("attacker", attacker.Name),
                    ("target", target.Name),
                    ("damage", damageDealt.Value))
                : LocalizedMessage.Create(
                    TranslationKey.AttackMiss,
                    ("attacker", attacker.Name),
                    ("target", target.Name));

            await _mediator.Publish(new AttackExecutedEvent(
                attacker.Id,
                target.Id,
                damageDealt,
                message
            ));
        }
    }
}

// Infrastructure Layer - Bridges to Godot's TranslationServer
namespace Darklands.Core.Infrastructure.Localization
{
    public interface ILocalizationService
    {
        string Translate(LocalizedMessage message);
        string GetCurrentLanguage();
        Fin<Unit> SetLanguage(string languageCode);
        IObservable<string> LanguageChanged { get; }
    }

    public sealed class GodotLocalizationService : ILocalizationService
    {
        private readonly ILogger _logger;
        private readonly Subject<string> _languageChanged = new();

        public string Translate(LocalizedMessage message)
        {
            try
            {
                // Get base translation from Godot
                var baseText = TranslationServer.Translate(message.Key.Value);
                
                // Handle parameters if present
                if (message.Parameters?.Any() == true)
                {
                    return FormatWithParameters(baseText, message.Parameters);
                }
                
                return baseText;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Translation failed for key: {Key}", message.Key.Value);
                // Fallback to key itself
                return $"[{message.Key.Value}]";
            }
        }

        private string FormatWithParameters(string template, IReadOnlyDictionary<string, object> parameters)
        {
            var result = template;
            foreach (var (key, value) in parameters)
            {
                result = result.Replace($"{{{key}}}", value?.ToString() ?? "");
            }
            return result;
        }

        public string GetCurrentLanguage()
        {
            return TranslationServer.GetLocale();
        }

        public Fin<Unit> SetLanguage(string languageCode)
        {
            try
            {
                TranslationServer.SetLocale(languageCode);
                _languageChanged.OnNext(languageCode);
                return unit;
            }
            catch (Exception ex)
            {
                return Error.New($"Failed to set language: {ex.Message}");
            }
        }

        public IObservable<string> LanguageChanged => _languageChanged.AsObservable();
    }
}

// Presentation Layer - Final rendering with Godot UI
namespace Darklands.Presentation.Views
{
    public partial class CombatLogView : RichTextLabel
    {
        private ILocalizationService? _localizationService;
        private IDisposable? _eventSubscription;

        public override void _Ready()
        {
            _localizationService = GameStrapper.GetServices()
                .Bind(sp => sp.GetService<ILocalizationService>().ToFin())
                .Match(
                    Succ: service => service,
                    Fail: _ => null);

            // Subscribe to combat events
            _eventSubscription = GameStrapper.GetServices()
                .Bind(sp => sp.GetService<IMediator>().ToFin())
                .Match(
                    Succ: mediator => mediator.Subscribe<AttackExecutedEvent>(OnAttackExecuted),
                    Fail: _ => null);
        }

        private void OnAttackExecuted(AttackExecutedEvent evt)
        {
            // Translate message at presentation layer
            var text = _localizationService?.Translate(evt.Message) ?? evt.Message.Key.Value;
            
            // Use Godot's BBCode for rich formatting
            AppendBbcode($"[color=yellow]{text}[/color]\n");
        }
    }
}
```

### Translation File Structure

```
darklands/
├── godot_project/
│   └── localization/
│       ├── translations.csv          # Main translation file
│       ├── translations.en.translation # Godot-generated
│       ├── translations.es.translation # Godot-generated
│       └── translations.ja.translation # Godot-generated
```

### CSV Format (Godot-compatible)

```csv
keys,en,es,ja
combat.attack.success,{attacker} hits {target} for {damage} damage!,¡{attacker} golpea a {target} por {damage} de daño!,{attacker}が{target}に{damage}ダメージを与えた！
combat.attack.miss,{attacker} misses {target},{attacker} falla contra {target},{attacker}の攻撃は{target}に外れた
combat.actor.defeated,{actor} has been defeated!,¡{actor} ha sido derrotado!,{actor}が倒された！
combat.turn.start,{actor}'s turn begins,Comienza el turno de {actor},{actor}のターン開始
ui.menu.new_game,New Game,Nuevo Juego,新しいゲーム
ui.menu.continue,Continue,Continuar,続ける
ui.menu.settings,Settings,Configuración,設定
```

## Implementation Strategy

### Phase 1: Infrastructure Foundation
1. Create ILocalizationService interface
2. Implement GodotLocalizationService with TranslationServer bridge
3. Add to GameStrapper DI configuration
4. Create initial CSV with core keys

### Phase 2: Domain Integration
1. Define TranslationKey and LocalizedMessage types
2. Update domain events to use LocalizedMessage
3. Modify command handlers to generate messages
4. Add strongly-typed key constants

### Phase 3: Presentation Layer
1. Update views to use ILocalizationService
2. Implement language switching UI
3. Add BBCode formatting for rich text
4. Test hot-reload during development

### Phase 4: Content Migration
1. Extract all hardcoded strings to CSV
2. Create translation keys for all text
3. Provide initial translations
4. Document translation workflow

## Consequences

### Positive

1. **Clean Architecture Maintained**: Domain remains pure C# without framework dependencies
2. **Type Safety**: Compile-time checking prevents invalid translation keys
3. **Designer-Friendly**: CSV editing in Excel/Google Sheets
4. **Godot Integration**: Leverages TranslationServer's proven features
5. **Hot-Reload**: Translations update immediately during development
6. **Fallback Support**: Automatic fallback to default language
7. **Performance**: TranslationServer caching prevents repeated lookups
8. **Testability**: Domain/Application tests mock ILocalizationService
9. **Extensible**: Easy to add new languages
10. **Parameter Safety**: Structured parameter passing prevents injection

### Negative

1. **Indirection**: Extra layer between domain and translations
2. **Key Management**: Must maintain translation keys in code and CSV
3. **No Compile-Time Text**: Can't see actual text in domain code
4. **Parameter Mismatch**: Runtime errors if CSV parameters don't match code
5. **Initial Setup**: More complex than hardcoded strings

## Alternatives Considered

### 1. Direct TranslationServer Usage in Domain
- **Pros**: Simpler, no bridge needed
- **Cons**: Violates Clean Architecture, couples domain to Godot
- **Rejected**: Architectural integrity is paramount

### 2. Resource Files (.resx)
- **Pros**: Standard .NET approach, compile-time safety
- **Cons**: No Godot editor support, harder for designers
- **Rejected**: Loses Godot's localization benefits

### 3. JSON-based Translations
- **Pros**: Framework-agnostic, human-readable
- **Cons**: No TranslationServer integration, manual parsing
- **Rejected**: Duplicates Godot's existing functionality

### 4. Hardcoded Strings with Comments
- **Pros**: Simplest approach, no infrastructure
- **Cons**: No localization support, unprofessional
- **Rejected**: Localization is a core requirement

## Security Considerations

- **Injection Prevention**: Parameters are escaped, not interpolated
- **File Access**: Translation files are read-only resources
- **Language Codes**: Validate against allowed list

## Testing Strategy

```csharp
// Domain tests - no Godot dependency
[Fact]
public void AttackCommand_GeneratesCorrectMessage()
{
    var message = LocalizedMessage.Create(
        TranslationKey.AttackSuccess,
        ("attacker", "Hero"),
        ("damage", 10));
    
    message.Key.Should().Be(TranslationKey.AttackSuccess);
    message.Parameters["damage"].Should().Be(10);
}

// Infrastructure tests - mock TranslationServer
[Fact]
public void LocalizationService_TranslatesWithParameters()
{
    var mockTranslator = new Mock<ITranslationServer>();
    mockTranslator.Setup(t => t.Translate("combat.attack.success"))
        .Returns("{attacker} hits for {damage}!");
    
    var service = new GodotLocalizationService(mockTranslator.Object);
    var result = service.Translate(message);
    
    result.Should().Be("Hero hits for 10!");
}
```

## Migration Path

1. Start with new features using LocalizedMessage
2. Gradually migrate existing hardcoded strings
3. Build translation key constants as needed
4. Add languages incrementally

## References

- [Godot Internationalization](https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html)
- [ADR-011: Resource Bridge Pattern](ADR-011-godot-resource-bridge-pattern.md) - Similar pattern
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Principles
- [TranslationServer API](https://docs.godotengine.org/en/stable/classes/class_translationserver.html) - Godot docs