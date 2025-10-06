# ADR-005: Internationalization Architecture

**Status**: Approved
**Date**: 2025-10-06
**Last Updated**: 2025-10-06
**Decision Makers**: Tech Lead

**Changelog**:
- 2025-10-06 (v1.4): Added Data-Driven Integration subsection - Cross-referenced ADR-006 for template-based entities with translation keys
- 2025-10-06 (v1.3): Added UX Considerations section - Text expansion guidelines (+30% width buffer), font support (CJK deferred), RTL layout (deferred), cultural adaptation notes
- 2025-10-06 (v1.2): Robustness refinements - Added validation script to Phase 5, documented placeholder reordering for translators, clarified enforcement strategy (static analysis + code review)
- 2025-10-06 (v1.1): Refinement based on expert review - Removed string-splitting anti-pattern, documented deferred optimizations (type-safe codegen, modular CSVs) with clear trigger conditions
- 2025-10-06 (v1.0): Initial decision - Godot CSV-based i18n with translation key discipline

**Related ADRs**:
- [ADR-001: Clean Architecture Foundation](./ADR-001-clean-architecture-foundation.md) - Core must be Godot-free
- [ADR-002: Godot Integration Architecture](./ADR-002-godot-integration-architecture.md) - Presentation/Core boundary
- [ADR-003: Functional Error Handling](./ADR-003-functional-error-handling.md) - Error messages in Result<T>
- [ADR-004: Feature-Based Clean Architecture](./ADR-004-feature-based-clean-architecture.md) - Where translation keys live
- [ADR-006: Data-Driven Entity Design](./ADR-006-data-driven-entity-design.md) - Templates store translation keys

---

## Context

Darklands is a **text-heavy roguelike** with:
- **Numerous entities**: Actors (Player, Goblin, Orc), Items (Iron Sword, Health Potion), Skills (Fireball, Heal)
- **UI text**: Buttons, labels, tooltips, error messages, combat logs
- **Domain errors**: Validation messages ("Damage cannot be negative", "Item cannot be placed here")
- **Potential international audience**: English, Chinese, Japanese, etc.

**The Problem**: When should we internationalize?

### Option 1: Defer i18n Until Later
- ✅ **Pro**: Faster initial development (no translation overhead)
- ❌ **Con**: **10x cost refactoring** - changing `actor.Name = "Goblin"` to `actor.NameKey = "ACTOR_GOBLIN"` across entire codebase
- ❌ **Con**: Hardcoded strings spread throughout Domain/Presentation (100+ files)
- ❌ **Con**: Architecture doesn't support i18n (Clean Architecture violation if Domain returns English)

### Option 2: Build i18n Infrastructure Now
- ✅ **Pro**: **Near-zero ongoing cost** - just habit (like using `Result<T>`)
- ✅ **Pro**: **Aligns with Clean Architecture** - Domain returns keys (locale-agnostic), Presentation translates (UI concern)
- ✅ **Pro**: **Future-proof** - adding Chinese later = just create `zh_CN.csv` (zero code changes)
- ❌ **Con**: Upfront architectural work (4-8 hours)
- ❌ **Con**: Discipline required (always use keys, never hardcode strings)

**Decision**: **Build infrastructure now, defer actual translation until Phase 1 validated.**

**Rationale**: This is **risk management**, not premature optimization:
- **Low upfront cost** (4-8 hours) vs **high deferred cost** (10x refactor)
- **Architectural scaffolding** (habit-forming) vs **full implementation** (actual translation work)
- **Smart deferral** - we build the pattern NOW, populate other languages LATER (after market validation)

---

## Decision

We adopt **Translation Key Discipline with Godot's CSV-based i18n system**:

1. **Domain returns translation keys** (e.g., `"ACTOR_GOBLIN"`, `"ERROR_DAMAGE_NEGATIVE"`)
2. **Presentation translates keys** using Godot's `tr()` function
3. **English-only for now** - deferred: Chinese/Japanese translations until Phase 1 validated
4. **Logs use English fallback** - developers read logs, not players

---

## Architecture Pattern

### Core Principle: Domain Returns Keys, Presentation Translates

```csharp
// ===== DOMAIN LAYER (Pure C#, no Godot) =====
// Domain/Common/Actor.cs
public record Actor(
    ActorId Id,
    string NameKey  // "ACTOR_GOBLIN" (translation key, NOT translated text)
);

// Domain/Common/Health.cs
public sealed record Health
{
    public Result<Health> TakeDamage(float amount)
    {
        if (amount < 0)
            return Result.Failure<Health>("ERROR_DAMAGE_NEGATIVE");  // Error key

        var newCurrent = Math.Max(0, Current - amount);
        return Result.Success(new Health(newCurrent, Maximum));
    }
}

// ===== APPLICATION LAYER =====
// Application/Events/HealthChangedEvent.cs
public record HealthChangedEvent(
    ActorId ActorId,
    string ActorNameKey,  // "ACTOR_GOBLIN" (for logs/UI)
    float OldHealth,
    float NewHealth,
    bool IsDead
) : INotification;

// Application/Commands/TakeDamageCommandHandler.cs
public class TakeDamageCommandHandler : IRequestHandler<TakeDamageCommand, Result>
{
    private readonly ILogger<TakeDamageCommandHandler> _logger;

    public async Task<Result> Handle(TakeDamageCommand cmd, CancellationToken ct)
    {
        return await GetActor(cmd.ActorId)
            .Bind(actor => actor.Health.TakeDamage(cmd.Amount)
                .Tap(newHealth => {
                    // Log with translation key (will be auto-translated to English)
                    _logger.LogInformation(
                        "Actor {ActorName} took {Damage} damage",
                        actor.NameKey,  // "ACTOR_GOBLIN" → "Goblin" (via logger enricher)
                        cmd.Amount
                    );

                    // Publish event with key (not translated text)
                    _mediator.Publish(new HealthChangedEvent(
                        actor.Id,
                        actor.NameKey,  // Presentation will translate
                        oldHealth: actor.Health.Current,
                        newHealth: newHealth.Current,
                        isDead: newHealth.Current <= 0
                    ));
                }));
    }
}

// ===== PRESENTATION LAYER (Godot C#) =====
// Components/HealthBarNode.cs
public partial class HealthBarNode : EventAwareNode
{
    [Export] private Label _actorNameLabel = null!;
    [Export] private ProgressBar _healthBar = null!;

    private void OnHealthChanged(HealthChangedEvent evt)
    {
        if (evt.ActorId != _actorId) return;

        // ✅ TRANSLATE HERE: Godot's tr() function
        _actorNameLabel.Text = tr(evt.ActorNameKey);  // "ACTOR_GOBLIN" → "Goblin" (English) or "哥布林" (Chinese)

        _healthBar.Value = evt.NewHealth;
        _healthBar.Modulate = evt.IsDead ? Colors.Red : Colors.Green;
    }
}

// Components/DamageButtonNode.cs
public partial class DamageButtonNode : Button
{
    private IMediator _mediator = null!;

    private async void OnPressed()
    {
        var result = await _mediator.Send(new TakeDamageCommand(_actorId, 10f));

        result.Match(
            onSuccess: () => GD.Print("Damage applied"),
            onFailure: err => {
                // ✅ TRANSLATE ERROR KEY
                var translatedError = tr(err);  // "ERROR_DAMAGE_NEGATIVE" → "Damage cannot be negative"
                ShowErrorPopup(translatedError);
            }
        );
    }
}
```

### Key Naming Convention

**CRITICAL**: Consistent naming prevents key collisions and improves discoverability.

| Prefix | Purpose | Example | Domain/Feature Location |
|--------|---------|---------|------------------------|
| `ACTOR_*` | Entity names (actors, NPCs, bosses) | `ACTOR_GOBLIN` | `Domain/Common/` (shared) |
| `ITEM_*` | Item names | `ITEM_SWORD_IRON` | `Features/Inventory/translations/` |
| `SKILL_*` | Ability/skill names | `SKILL_FIREBALL` | `Features/Magic/translations/` |
| `ERROR_*` | Error messages (validation, business rules) | `ERROR_DAMAGE_NEGATIVE` | `Domain/Common/translations/` or feature-specific |
| `UI_*` | UI labels, buttons, tooltips | `UI_BUTTON_ATTACK` | `Presentation/translations/` |
| `DESC_*` | Long-form descriptions | `DESC_ITEM_SWORD` | Feature-specific |
| `STATUS_*` | Status effect names | `STATUS_POISONED` | `Features/Status/translations/` |

**Naming Rules**:
- ✅ **SCREAMING_SNAKE_CASE** - easy to identify as translation keys
- ✅ **Hierarchical** - `ITEM_WEAPON_SWORD_IRON` (category → subcategory → specific)
- ✅ **No abbreviations** - `ACTOR_GOBLIN`, not `ACT_GOB`
- ❌ **No hardcoded strings** - NEVER `_label.Text = "Goblin"` in Presentation

---

## Translation Files (Godot CSV System)

### File Structure

```
godot_project/
└── translations/
    ├── en.csv         # English (default/fallback)
    ├── zh_CN.csv      # Simplified Chinese (DEFERRED until Phase 1 validated)
    └── ja.csv         # Japanese (DEFERRED)
```

### Example: en.csv (English - Default/Fallback)

```csv
keys,en
ACTOR_PLAYER,Player
ACTOR_GOBLIN,Goblin
ACTOR_ORC,Orc
ACTOR_SKELETON,Skeleton
ITEM_SWORD_IRON,Iron Sword
ITEM_POTION_HEALTH,Health Potion
SKILL_FIREBALL,Fireball
SKILL_HEAL,Heal
ERROR_DAMAGE_NEGATIVE,Damage cannot be negative
ERROR_HEALTH_DEPLETED,Health is already depleted
ERROR_ITEM_PLACEMENT_INVALID,Item cannot be placed here
UI_BUTTON_ATTACK,Attack
UI_BUTTON_DEFEND,Defend
UI_BUTTON_INVENTORY,Inventory
UI_LABEL_HEALTH,Health
DESC_ITEM_SWORD,A sturdy iron sword. Deals moderate damage.
STATUS_POISONED,Poisoned
STATUS_STUNNED,Stunned
```

### Example: zh_CN.csv (Simplified Chinese - DEFERRED)

```csv
keys,zh_CN
ACTOR_PLAYER,玩家
ACTOR_GOBLIN,哥布林
ACTOR_ORC,兽人
ACTOR_SKELETON,骷髅
ITEM_SWORD_IRON,铁剑
ITEM_POTION_HEALTH,生命药水
SKILL_FIREBALL,火球术
SKILL_HEAL,治疗
ERROR_DAMAGE_NEGATIVE,伤害值不能为负数
ERROR_HEALTH_DEPLETED,生命值已耗尽
ERROR_ITEM_PLACEMENT_INVALID,无法在此处放置物品
UI_BUTTON_ATTACK,攻击
UI_BUTTON_DEFEND,防御
UI_BUTTON_INVENTORY,背包
UI_LABEL_HEALTH,生命值
DESC_ITEM_SWORD,一把坚固的铁剑。造成中等伤害。
STATUS_POISONED,中毒
STATUS_STUNNED,眩晕
```

### Godot Configuration (project.godot)

```gdscript
[internationalization]
locale/translations = PackedStringArray(
    "res://translations/en.csv",
    "res://translations/zh_CN.csv"
)
locale/fallback = "en"
locale/test = ""  # Empty = use system locale
```

**How `tr()` Works**:
1. User's system locale detected (e.g., `zh_CN`)
2. `tr("ACTOR_GOBLIN")` looks up in `zh_CN.csv` → returns `"哥布林"`
3. If key missing in `zh_CN.csv`, fallback to `en.csv` → returns `"Goblin"`
4. If key missing in both → returns raw key `"ACTOR_GOBLIN"` (visible bug, not crash)

---

## Logging Exception (Developer Readability)

**Problem**: If logs use raw keys, they're unreadable:
```
ERROR: Actor ACTOR_GOBLIN took ERROR_DAMAGE_NEGATIVE
```

**Solution**: Logger middleware auto-translates keys to English (fallback locale):

```csharp
// Infrastructure/Logging/TranslationLogEnricher.cs
using Serilog.Core;
using Serilog.Events;

public class TranslationLogEnricher : ILogEventEnricher
{
    private readonly IReadOnlyDictionary<string, string> _translations;

    public TranslationLogEnricher()
    {
        // Load en.csv translations for log enrichment
        _translations = LoadTranslations("translations/en.csv");
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
    {
        foreach (var property in logEvent.Properties)
        {
            if (property.Value is ScalarValue { Value: string value } && IsTranslationKey(value))
            {
                // Translate key to English for logs
                var translated = _translations.GetValueOrDefault(value, value);
                logEvent.AddOrUpdateProperty(factory.CreateProperty(
                    property.Key,
                    translated));
            }
        }
    }

    private static bool IsTranslationKey(string value) =>
        value.Length > 2 &&
        char.IsUpper(value[0]) &&
        value.Contains('_');
}

// Infrastructure/DependencyInjection/GameStrapper.cs
Log.Logger = new LoggerConfiguration()
    .Enrich.With<TranslationLogEnricher>()  // Auto-translate keys in logs
    .WriteTo.Console()
    .WriteTo.File("logs/darklands.log")
    .CreateLogger();
```

**Result**: Logs are developer-readable:
```
INFO: Actor Goblin took Damage cannot be negative
```

**Rationale**: Logs are for **developers** (debugging), not **players** (UI). English logs are fine.

---

## Integration with Clean Architecture

### Domain Layer (Pure C#)

**What Lives Here**:
- Translation keys as **data** (not UI strings)
- Entity properties: `Actor.NameKey = "ACTOR_GOBLIN"`
- Error keys: `Result.Failure("ERROR_DAMAGE_NEGATIVE")`

**What Does NOT Live Here**:
- ❌ Godot's `tr()` function (Godot dependency)
- ❌ Translation logic (UI concern)
- ❌ Hardcoded English strings (violates locale-agnostic principle)

**Example**:
```csharp
// ✅ CORRECT: Domain uses keys
public record Actor(ActorId Id, string NameKey);

// ❌ WRONG: Domain uses translated text
public record Actor(ActorId Id, string Name);  // Which language???
```

### Application Layer

**What Lives Here**:
- Commands/Queries with translation keys
- Events with translation keys
- Handler logic (no translation, just keys)

**Example**:
```csharp
// ✅ CORRECT: Event carries key
public record HealthChangedEvent(ActorId ActorId, string ActorNameKey, ...);

// ❌ WRONG: Event carries translated text
public record HealthChangedEvent(ActorId ActorId, string ActorName, ...);  // Locale?
```

### Infrastructure Layer

**What Lives Here**:
- Logger enricher (translates keys for logs)
- ❌ NOT: Translation service abstraction (Godot's `tr()` is good enough)

### Presentation Layer (Godot C#)

**What Lives Here**:
- ALL `tr()` calls (translates keys → displayable text)
- UI labels, buttons, tooltips
- Error message display

**Pattern**:
```csharp
// ✅ CORRECT: Translate at display time
_label.Text = tr(actor.NameKey);
_errorLabel.Text = tr(errorKey);

// ❌ WRONG: Hardcoded strings
_label.Text = "Goblin";
_errorLabel.Text = "Damage cannot be negative";
```

### Data-Driven Integration

Translation keys can originate from **external data files** (Godot Resources, JSON) rather than being hardcoded in C#.

**Pattern**: Entity templates store translation keys as data.

```csharp
// Infrastructure: Godot Resource template
[GlobalClass]
public partial class ActorTemplate : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string NameKey { get; set; } = "";  // "ACTOR_GOBLIN" - translation key
    [Export] public float MaxHealth { get; set; } = 100f;
}

// Application: Create entity from template
var template = _templateService.GetTemplate("goblin");
var actor = new Actor(ActorId.NewId(), template.NameKey);  // NameKey from data, not hardcoded

// Presentation: Translate key from data-driven entity
_nameLabel.Text = tr(actor.NameKey);  // "ACTOR_GOBLIN" → "Goblin" or "哥布林"
```

**No architecture conflict**: Data-driven entities work seamlessly with i18n. Keys flow from **template data → Domain entity → Presentation translation**.

**See [ADR-006: Data-Driven Entity Design](./ADR-006-data-driven-entity-design.md)** for comprehensive content authoring workflow, template validation strategy, and integration patterns.

---

## Migration Path (Refactoring Existing Code)

### Step 1: Create Translation Files (1 hour)

```bash
# Create translations directory
mkdir -p godot_project/translations

# Create en.csv with existing strings
cat > godot_project/translations/en.csv <<EOF
keys,en
ACTOR_PLAYER,Player
ERROR_DAMAGE_NEGATIVE,Damage cannot be negative
UI_BUTTON_ATTACK,Attack
EOF
```

### Step 2: Configure Godot (10 minutes)

Edit `project.godot`:
```gdscript
[internationalization]
locale/translations = PackedStringArray("res://translations/en.csv")
locale/fallback = "en"
```

### Step 3: Add NameKey to Actor Entity (30 minutes)

```csharp
// BEFORE
public record Actor(ActorId Id, string Name);

// AFTER
public record Actor(
    ActorId Id,
    string NameKey  // "ACTOR_GOBLIN"
);
```

Update all Actor instantiations:
```csharp
// BEFORE
var player = new Actor(ActorId.NewId(), "Player");

// AFTER
var player = new Actor(ActorId.NewId(), "ACTOR_PLAYER");
```

### Step 4: Refactor Presentation to Use tr() (2 hours)

```csharp
// BEFORE (Presentation layer)
_actorNameLabel.Text = actor.Name;

// AFTER
_actorNameLabel.Text = tr(actor.NameKey);
```

```csharp
// BEFORE (Godot nodes)
_attackButton.Text = "Attack";

// AFTER
_attackButton.Text = tr("UI_BUTTON_ATTACK");
```

### Step 5: Validate (30 minutes)

```bash
# Grep for hardcoded strings (should return ZERO results in Presentation layer)
grep -r '_label.Text = "' Components/
grep -r '_button.Text = "' Components/

# Grep for .Name property (should be NameKey now)
grep -r '\.Name[^K]' src/Darklands.Core/Domain/

# Run architecture tests (future: automate this)
dotnet test --filter "Category=Architecture"
```

---

## Testing Strategy

### Domain Tests (Assert Against Keys)

```csharp
// Domain/Tests/HealthTests.cs
[Fact]
public void TakeDamage_NegativeAmount_ReturnsErrorKey()
{
    // Arrange
    var health = Health.Create(100, 100).Value;

    // Act
    var result = health.TakeDamage(-10);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be("ERROR_DAMAGE_NEGATIVE");  // Assert against KEY
}
```

**Why Keys in Tests**: Domain tests are locale-agnostic. Keys are stable, translations change.

### Presentation Tests (Mock tr())

```csharp
// Presentation/Tests/HealthBarNodeTests.cs
[Fact]
public void OnHealthChanged_TranslatesActorName()
{
    // Arrange
    var evt = new HealthChangedEvent(
        ActorId: _actorId,
        ActorNameKey: "ACTOR_GOBLIN",
        OldHealth: 100,
        NewHealth: 90,
        IsDead: false
    );

    // Mock Godot's tr() function
    _mockTranslator.Setup(t => t.Translate("ACTOR_GOBLIN")).Returns("Goblin");

    // Act
    _healthBarNode.OnHealthChanged(evt);

    // Assert
    _actorNameLabel.Text.Should().Be("Goblin");  // Translated text
}
```

### Architecture Tests (Enforce Discipline)

```csharp
// Tests/Architecture/I18nArchitectureTests.cs
using NetArchTest.Rules;

[Fact]
public void Domain_ShouldNotUseGodotTranslation()
{
    // WHY: Domain must be Godot-free (ADR-001)
    var result = Types.InAssembly(typeof(Actor).Assembly)
        .That().ResideInNamespace("Darklands.Core.Domain")
        .ShouldNot().HaveDependencyOn("Godot")
        .GetResult();

    result.IsSuccessful.Should().BeTrue("Domain must not reference Godot");
}

[Fact]
public void Presentation_ShouldNotHaveHardcodedStrings()
{
    // WHY: All UI strings must use tr() for i18n
    var presentationFiles = Directory.GetFiles("Components", "*.cs", SearchOption.AllDirectories);

    foreach (var file in presentationFiles)
    {
        var content = File.ReadAllText(file);

        // Detect hardcoded strings assigned to UI properties
        var hardcodedPattern = @"\.(Text|TooltipText)\s*=\s*""[^{]"; // Ignore interpolated strings
        var matches = Regex.Matches(content, hardcodedPattern);

        matches.Should().BeEmpty($"{file} contains hardcoded UI strings. Use tr() instead.");
    }
}
```

**Enforcement Strategy**: Static analysis (regex, architecture tests) provides **first-line defense** against hardcoded strings. **Code review remains the ultimate enforcement mechanism** - human judgment catches patterns that bypass automation (e.g., assigning hardcoded string to variable, then to UI property).

---

## Error Handling Patterns

### Pattern 1: Domain Validation Errors

```csharp
// Domain returns error key
public Result<Health> TakeDamage(float amount)
{
    if (amount < 0)
        return Result.Failure<Health>("ERROR_DAMAGE_NEGATIVE");

    if (Current <= 0)
        return Result.Failure<Health>("ERROR_HEALTH_DEPLETED");

    return Result.Success(new Health(Math.Max(0, Current - amount), Maximum));
}

// Presentation translates error key
result.Match(
    onSuccess: h => UpdateHealthBar(h),
    onFailure: err => ShowError(tr(err))  // "ERROR_DAMAGE_NEGATIVE" → "Damage cannot be negative"
);
```

### Pattern 2: Parameterized Error Messages (Structured DTOs)

**Problem**: Some errors need dynamic values (e.g., "Need 50 gold, have 20").

**Solution**: Use `Result<T, TError>` with structured error DTOs.

```csharp
// Application layer returns structured error DTO
public record InsufficientGoldError(int Required, int Current);

public Result<PurchaseResult, InsufficientGoldError> PurchaseItem(int cost)
{
    if (Gold < cost)
        return Result.Failure<PurchaseResult, InsufficientGoldError>(
            new InsufficientGoldError(cost, Gold));

    return Result.Success(new PurchaseResult(...));
}

// Presentation uses DTO values with translation key
result.Match(
    onSuccess: r => CompleteTransaction(r),
    onFailure: err => {
        // en.csv: "ERROR_INSUFFICIENT_GOLD,Need {0} gold, have {1}"
        var message = tr("ERROR_INSUFFICIENT_GOLD").format(err.Required, err.Current);
        ShowError(message);
    }
);
```

**Why Structured DTOs**:
- ✅ **Type-safe**: Compile-time validation of parameters
- ✅ **Testable**: Easy to assert against specific error values
- ✅ **Refactorable**: Renaming fields is safe (compiler checks)
- ❌ **Anti-pattern**: Never use string splitting (`"ERROR|param1|param2"`) - fragile, error-prone, no type safety

**Pattern**: Simple validation errors return `Result.Failure("ERROR_KEY")`. Complex errors with parameters use `Result<T, CustomErrorDTO>`.

**Note on Language Word Order**: Translators can reorder placeholders to match target language grammar. Godot's `tr().format()` supports arbitrary placeholder order.

Example:
```csv
# en.csv
ERROR_INSUFFICIENT_GOLD,Need {0} gold, have {1}

# ja.csv (placeholders reordered for Japanese grammar)
ERROR_INSUFFICIENT_GOLD,{1}しか持っていません。{0}ゴールドが必要です。
```

---

## UX Considerations (i18n-Specific)

### Text Expansion (Critical for UI Layout)

**Problem**: Translated text length varies significantly across languages. German/French text is typically **30-40% longer** than English, while Chinese/Japanese is often **shorter**.

**Impact on UI Design**:
- **Buttons**: English "Attack" (6 chars) → German "Angreifen" (10 chars) = 67% longer
- **Labels**: English "Health" (6 chars) → German "Gesundheit" (10 chars) = 67% longer
- **Error messages**: Can be 2x longer in some languages

**Design Guidelines**:
```csharp
// ❌ BAD: Fixed-width button (text will overflow in German)
_attackButton.Size = new Vector2(80, 40);  // Sized for "Attack" only

// ✅ GOOD: Auto-sizing container with padding reserve
_attackButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
_attackButton.CustomMinimumSize = new Vector2(100, 40);  // 25% buffer for expansion
```

**Recommendation**: Reserve **+30% width** for all UI text containers to accommodate German/French translations.

---

### Font Support (Deferred)

**Current**: English-only uses Latin fonts (Godot default fonts support ASCII/Latin-1).

**When localizing to CJK** (Chinese, Japanese, Korean):
- **Requirement**: Must use CJK-compatible fonts (e.g., Noto Sans CJK, Source Han Sans)
- **File size**: CJK fonts are large (5-15 MB per font file)
- **Performance**: Godot's DynamicFont handles CJK well, but initial load time increases
- **Fallback fonts**: Configure font fallback chain for mixed scripts

**Deferred until**: Chinese/Japanese translation begins (Phase 1 validation complete).

---

### RTL Layout Support (Deferred)

**Current**: LTR (Left-to-Right) layout only.

**When localizing to RTL languages** (Arabic, Hebrew):
- **Requirement**: Mirror entire UI horizontally (buttons on right, menus on left)
- **Godot support**: Godot 4.x has built-in RTL support via `Control.layout_direction`
- **Complexity**: Requires testing every UI screen in mirrored mode
- **Asset impact**: Asymmetric icons/sprites may need mirrored versions

**Deferred until**: Arabic/Hebrew translation happens (low priority until market demand validated).

---

### Cultural Adaptation (Future Consideration)

**Colors**:
- Red = danger (Western) but prosperity/luck (Chinese)
- Green = safe (Western) but caution (some Asian cultures)

**Icons/Symbols**:
- Thumbs up = positive (Western) but offensive (some Middle Eastern cultures)
- Hand gestures vary widely

**Recommendation**: Use **universal game conventions** (red health bar, green stamina) rather than cultural-specific symbols. Defer cultural adaptation until specific markets targeted.

---

## Consequences

### Positive

✅ **Future-Proof**: Adding Chinese = create `zh_CN.csv` (zero code changes)
✅ **Clean Architecture Alignment**: Domain returns keys (locale-agnostic), Presentation translates (UI concern)
✅ **Developer-Friendly**: Logs auto-translate to English (readable)
✅ **Testable**: Domain tests assert against keys (stable), Presentation tests mock `tr()`
✅ **Designer-Friendly**: CSV editable in Excel/Google Sheets (no code changes)
✅ **Performance**: Godot caches translations at startup (O(1) lookup, zero runtime cost)
✅ **Incremental**: English-only NOW, add languages LATER (risk management)

### Negative

❌ **Discipline Required**: Developers must always use keys, never hardcode strings
❌ **Learning Curve**: Team must learn key naming convention (`ACTOR_*`, `ERROR_*`, etc.)
❌ **Verbose**: `tr("UI_BUTTON_ATTACK")` vs `"Attack"` (4x more characters)
❌ **Key Typos**: `"ERROR_DAMAGE_NEGATIV"` silently shows raw key (not crash, but ugly)
❌ **Tooling Gap**: Godot editor doesn't autocomplete translation keys (IDE support limited)

### Neutral

➖ **Upfront Cost**: 4-8 hours to set up infrastructure (small investment for 10x future savings)
➖ **Deferred Translation Work**: English-only for Phase 1, actual translation work deferred until market validated
➖ **CSV Format**: Not as powerful as gettext `.po` files, but good enough for games

---

## Alternatives Considered

### Alternative 1: English Hardcoded Strings (Defer i18n)

```csharp
// Domain
public record Actor(ActorId Id, string Name);  // "Goblin"

// Presentation
_label.Text = actor.Name;  // Always English
```

**Pros**:
- ✅ Simple (no translation complexity)
- ✅ Fast initial development

**Cons**:
- ❌ **10x refactoring cost** when internationalization needed
- ❌ English-only game (limits market)
- ❌ Hardcoded strings spread across 100+ files

**Rejected**: Not future-proof. Refactoring entire codebase later is expensive and error-prone.

---

### Alternative 2: Custom JSON Translation System

```json
{
  "en": {
    "actors": {
      "goblin": "Goblin",
      "orc": "Orc"
    }
  }
}
```

**Pros**:
- ✅ Hierarchical structure (nested keys)
- ✅ JSON is widely supported

**Cons**:
- ❌ **Reinvents Godot's wheel** - Godot already has CSV-based i18n
- ❌ Custom code needed (loader, caching, `tr()` equivalent)
- ❌ Not designer-friendly (JSON harder to edit than CSV)
- ❌ No Godot editor integration

**Rejected**: Don't reinvent what Godot provides. CSV system is battle-tested and well-documented.

---

### Alternative 3: Translation Service Abstraction (ITranslationService)

```csharp
// Infrastructure
public interface ITranslationService
{
    string Translate(string key, string? fallback = null);
}

// Core uses abstraction
var translatedName = _translator.Translate(actor.NameKey);
```

**Pros**:
- ✅ Core doesn't depend on Godot directly
- ✅ Testable (mock ITranslationService)

**Cons**:
- ❌ **Violates Clean Architecture** - Domain/Application shouldn't know about translation (UI concern)
- ❌ Adds complexity (new abstraction + DI registration)
- ❌ Core would need dependency on ITranslationService (leaks UI concern into Core)

**Rejected**: Translation is a **Presentation concern**, not Domain/Application. Domain returns keys, Presentation translates. No abstraction needed.

---

### Alternative 4: Defer Entirely (Build i18n When Needed)

**Pros**:
- ✅ Zero upfront cost
- ✅ Don't build what you don't need yet

**Cons**:
- ❌ **10x cost when you do need it** (massive refactor)
- ❌ Architectural mismatch - Clean Architecture wants Domain to be locale-agnostic
- ❌ Risk of "never getting around to it" - hardcoded strings become legacy debt

**Rejected**: This is **cheap insurance** (4-8 hours) against **expensive refactor** (40-80 hours). Smart risk management.

---

## Success Metrics

✅ **Zero hardcoded strings** in Presentation layer (enforce via architecture tests)
✅ **All entity names** use translation keys (`Actor.NameKey = "ACTOR_GOBLIN"`)
✅ **All UI text** uses `tr()` function
✅ **Logs readable** in English (auto-translated via logger enricher)
✅ **Domain tests** assert against keys (not English text)
✅ **Adding new language** requires ONLY creating new CSV file (zero code changes)
✅ **Team discipline** - developers habitually use keys without reminders

---

## Implementation Checklist

**Phase 1: Infrastructure Setup** (1-2 hours):
- [ ] Create `godot_project/translations/` directory
- [ ] Create `en.csv` with all existing UI strings
- [ ] Configure `project.godot` i18n settings
- [ ] Add logger enricher for translation keys

**Phase 2: Domain Refactoring** (1 hour):
- [ ] Add `NameKey` property to `Actor` entity
- [ ] Update `Actor` constructor calls to use keys
- [ ] Refactor validation errors to return keys

**Phase 3: Presentation Refactoring** (2-3 hours):
- [ ] Replace hardcoded strings with `tr()` calls
- [ ] Update all UI labels/buttons to use keys
- [ ] Test all scenes for visual correctness

**Phase 4: Documentation** (1 hour):
- [ ] Update CLAUDE.md with i18n discipline
- [ ] Update dev-engineer persona protocol
- [ ] Update test-specialist persona protocol
- [ ] Create quick reference for key naming convention

**Phase 5: Validation** (30 min):
- [ ] Run architecture tests
- [ ] Run translation validation script (checks duplicates, naming format, auto-sorts CSV)
- [ ] Grep for hardcoded strings (first-line defense, not exhaustive)
- [ ] Manual QA: All UI text displays correctly
- [ ] Verify logs are readable
- [ ] Code review: Final human check for hardcoded strings (ultimate enforcement)

---

## References

- [Godot Internationalization Docs](https://docs.godotengine.org/en/stable/tutorials/i18n/internationalizing_games.html) - Official Godot i18n guide
- [Clean Architecture - Presentation Concerns](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Translation is UI layer
- [Domain-Driven Design - Ubiquitous Language](https://martinfowler.com/bliki/UbiquitousLanguage.html) - Keys are domain language
- [ADR-001: Clean Architecture Foundation](./ADR-001-clean-architecture-foundation.md)
- [ADR-002: Godot Integration Architecture](./ADR-002-godot-integration-architecture.md)

---

## Future Enhancements (Out of Scope for Now)

### Deferred Optimizations (Developer Experience)

**1. Type-Safe Translation Keys (Code Generation)**

**Problem**: Hand-written string keys (`"ACTOR_GOBLIN"`) cause typos, no IDE autocomplete, difficult refactoring.

**Solution**: Auto-generate static class from CSV:
```csharp
// Generated from en.csv - DO NOT EDIT
public static class TranslationKeys
{
    public const string ACTOR_GOBLIN = "ACTOR_GOBLIN";
    public const string UI_BUTTON_ATTACK = "UI_BUTTON_ATTACK";
    // ...
}

// Usage: tr(TranslationKeys.ACTOR_GOBLIN) - compile-time checked!
```

**Benefits**: Compile-time key validation, IDE autocomplete, safe refactoring.

**When to implement**: When we have **50+ translation keys** AND observe **3+ typo bugs in production**. Current status: <10 keys, manual testing catches typos faster than building generator.

**Trigger**: Create TD_XXX when key count exceeds 50 OR typo bugs become frequent.

---

**2. Modular Translation Files (Per-Feature)**

**Problem**: Single giant CSV causes merge conflicts, hard to manage at scale.

**Solution**: Per-feature CSVs merged at build:
```
Features/Inventory/translations/en.csv
Features/Combat/translations/en.csv
→ Build script merges → godot_project/translations/en.csv
```

**Benefits**: High cohesion (translations with code), reduced merge conflicts, clear ownership.

**When to implement**: When we have **3+ developers** editing translations **in parallel** AND see **5+ merge conflicts per week**. Current status: Solo developer, no conflicts possible.

**Trigger**: Create TD_XXX when team grows to 3+ devs AND conflicts become frequent.

---

### Deferred Features (Language Support)

**Deferred until Phase 1 validated**:
- Pluralization support (`tr_n()` for "1 item" vs "2 items")
- Context-aware translation (`tr("ATTACK", "noun")` vs `tr("ATTACK", "verb")`)
- Gender-aware translation (some languages need gendered text)
- Right-to-left language support (Arabic, Hebrew)
- Translation QA process (proofreading, consistency checks)
- Automated translation memory (reuse translations across similar strings)

**When to implement**: After 3+ languages added, when complexity justifies tooling investment.

---

**Translation Platform Recommendation**: When ready to localize, consider **Paratranz** (https://paratranz.cn) - a game-focused collaborative translation platform. Provides web UI for translators, context screenshots, translation memory, and exports to Godot-compatible CSV. Integrates seamlessly via CI/CD (upload `en.csv` source, download `zh_CN.csv`/`ja.csv` output). Zero architecture changes needed - purely workflow enhancement.

---

**Decision**: **Approved** - Build i18n infrastructure now (4-8 hours), defer actual translation until Phase 1 validated. This is smart risk management: low upfront cost, prevents 10x refactor later, aligns perfectly with Clean Architecture principles.
