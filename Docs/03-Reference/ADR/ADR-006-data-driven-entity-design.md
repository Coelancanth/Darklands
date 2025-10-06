# ADR-006: Data-Driven Entity Design

**Status**: Approved
**Date**: 2025-10-06
**Last Updated**: 2025-10-06
**Decision Makers**: Tech Lead

**Changelog**:
- 2025-10-06 (v1.1): Added comprehensive Control Flow section - Three execution phases (Design-Time, Startup, Runtime), critical insights, hot-reload flow
- 2025-10-06 (v1.0): Initial decision - Godot Resources for entity templates, designer autonomy, iteration speed

**Related ADRs**:
- [ADR-001: Clean Architecture Foundation](./ADR-001-clean-architecture-foundation.md) - Core must be Godot-free
- [ADR-002: Godot Integration Architecture](./ADR-002-godot-integration-architecture.md) - Infrastructure can use Godot APIs
- [ADR-003: Functional Error Handling](./ADR-003-functional-error-handling.md) - Template loading returns Result<T>
- [ADR-004: Feature-Based Clean Architecture](./ADR-004-feature-based-clean-architecture.md) - Templates are cross-cutting infrastructure
- [ADR-005: Internationalization Architecture](./ADR-005-internationalization-architecture.md) - Templates store translation keys

---

## Context

Darklands is a **content-heavy roguelike** requiring:
- **100+ entity types**: Actors (Player, Goblin, Orc, Skeleton, Bosses), Items (Swords, Potions, Armor), Skills (Fireball, Heal, Charge)
- **Frequent balance iteration**: Designers need to tweak health, damage, cooldowns daily
- **Designer autonomy**: Game designers should iterate without programmer intervention
- **Rapid prototyping**: Test new enemy types in minutes, not hours

### The Problem: Hard-Coded Entities Don't Scale

**Hard-coded approach** (common in early prototypes):
```csharp
public static class Entities
{
    public static Actor Goblin() => new Actor(
        ActorId.NewId(),
        nameKey: "ACTOR_GOBLIN",
        health: Health.Create(100, 100).Value,
        damage: 15f
    );

    public static Actor Orc() => new Actor(/*...*/);
    public static Actor Skeleton() => new Actor(/*...*/);
    // ... 100+ more entities
}
```

**Problems**:
- ❌ **Zero designer autonomy** - Programmers become bottleneck for balance tweaks
- ❌ **Slow iteration** - Change goblin health 100→120 requires: edit code → recompile → restart game → test
- ❌ **Merge conflicts** - Multiple designers tweaking same Entities.cs file
- ❌ **No hot-reload** - Can't tweak values and see changes instantly
- ❌ **Not data-driven** - Entities are code, not data (impossible to mod)

**The Question**: How do we enable **designer autonomy** while preserving **Clean Architecture**?

---

## Decision

We adopt **Godot Resources (.tres files) as entity template system**:

1. **Designer workflow**: Create entity templates in Godot Editor (visual, type-safe)
2. **Storage format**: Godot Resources (.tres files) - text-based, version-controllable
3. **Loading mechanism**: Infrastructure layer loads templates via `GD.Load<T>()`
4. **Domain integration**: Pure Domain entities created FROM template data (not inheritance)
5. **Validation**: Build-time scripts + load-time checks ensure data integrity

**Pattern**: Templates are **configuration data** that flow FROM Infrastructure TO Domain.

---

## Architecture Pattern

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────┐
│ PRESENTATION LAYER (Godot Editor)                  │
│ - Designer creates goblin.tres in Inspector        │
│ - Sets properties visually (NameKey, Health, etc.) │
│ - Saves to res://data/entities/                    │
└───────────────────┬─────────────────────────────────┘
                    │ .tres file (text-based)
                    ↓
┌─────────────────────────────────────────────────────┐
│ INFRASTRUCTURE LAYER                                │
│ - TemplateService loads .tres via GD.Load<T>()     │
│ - Validates template data (keys exist, valid stats)│
│ - Provides ITemplateService abstraction             │
└───────────────────┬─────────────────────────────────┘
                    │ ActorTemplate (DTO)
                    ↓
┌─────────────────────────────────────────────────────┐
│ APPLICATION LAYER                                   │
│ - SpawnActorCommand receives "goblin" template ID  │
│ - Handler gets template from ITemplateService       │
│ - Creates pure Actor entity from template data      │
└───────────────────┬─────────────────────────────────┘
                    │ Actor (pure entity)
                    ↓
┌─────────────────────────────────────────────────────┐
│ DOMAIN LAYER (Pure C#, no Godot)                   │
│ - Actor entity is pure (no Resource dependency)    │
│ - Testable, framework-agnostic                     │
└─────────────────────────────────────────────────────┘
```

**Key Insight**: Templates live in **Infrastructure**, not Domain. Domain remains pure.

---

## Control Flow (Three Execution Phases)

Understanding **when** and **how** data flows through the system is critical. There are three distinct execution phases:

### Phase 1: Design-Time Flow (Designer Creates Template)

**When**: During development, before game runs
**Who**: Game designer in Godot Editor
**Goal**: Author entity configuration data

```
Designer Opens Godot Editor
    ↓
Right-click res://data/entities/ → Create New → Resource → ActorTemplate
    ↓
Godot Inspector shows [Export] properties (editable fields)
    ↓
Designer fills values:
  - Id = "goblin"
  - NameKey = "ACTOR_GOBLIN"
  - MaxHealth = 100
  - Damage = 15
  - Sprite = res://sprites/goblin.png
    ↓
Designer saves → goblin.tres written to disk (text file)
    ↓
✅ Template exists as data file (no code executed)
```

**Key Point**: No code runs during design-time. This is pure **data authoring**.

---

### Phase 2: Startup Flow (Game Loads Templates)

**When**: Game startup, before first frame
**Who**: Infrastructure layer (GodotTemplateService)
**Goal**: Load all .tres files into in-memory cache

```
User launches game → GameStrapper._Ready()
    ↓
GameStrapper registers DI services:
  services.AddSingleton<ITemplateService<ActorTemplate>,
      GodotTemplateService<ActorTemplate>>()
    ↓
GameStrapper calls templateService.LoadTemplates()
    ↓
GodotTemplateService.LoadTemplates():
  var dir = DirAccess.Open("res://data/entities/")
    ↓
Loop through directory files:
  foreach (var file in dir.GetFiles())
    ↓
For each .tres file:
  - path = "res://data/entities/goblin.tres"
  - template = GD.Load<ActorTemplate>(path)  ← Godot deserializes file
  - Godot creates ActorTemplate C# object from .tres data
    ↓
Extract ID: var id = template.Id; // "goblin"
    ↓
Cache in dictionary: _templates["goblin"] = template
    ↓
Validation (fail-fast):
  - if (template.NameKey is empty) → ERROR
  - if (template.MaxHealth <= 0) → ERROR
  - if (template.Sprite == null) → ERROR
    ↓
Log results: "Loaded 15 templates"
    ↓
✅ Templates cached in memory:
   Dictionary<string, ActorTemplate> _templates
   ["goblin"] → ActorTemplate {Id="goblin", MaxHealth=100, ...}
   ["orc"] → ActorTemplate {Id="orc", MaxHealth=150, ...}
```

**Key Point**: Templates loaded **once at startup**, then cached in-memory. All subsequent lookups are O(1) dictionary access (fast).

---

### Phase 3: Runtime Flow (Game Spawns Entity)

**When**: During gameplay (player enters room, spawns enemy)
**Who**: Application layer (SpawnActorCommandHandler)
**Goal**: Create actual game entity from cached template

```
Game event: "Player enters Dungeon Level 1" → Need 3 goblins
    ↓
Presentation layer sends command:
  var cmd = new SpawnActorCommand(
      TemplateId: "goblin",
      Position: new Vector2(100, 200)
  );
  await _mediator.Send(cmd);
    ↓
MediatR routes → SpawnActorCommandHandler.Handle(cmd, ct)
    ↓
Handler fetches template from cache:
  var templateResult = _templates.GetTemplate("goblin")
  // O(1) dictionary lookup: _templates["goblin"]
    ↓
Check existence:
  if (templateResult.IsFailure)
      return Result.Failure("Template not found: goblin")
    ↓ (Success path)
Extract template: var template = templateResult.Value
  // template.NameKey = "ACTOR_GOBLIN"
  // template.MaxHealth = 100
  // template.Damage = 15
    ↓
Create Domain value objects FROM template data:
  var healthResult = Health.Create(
      template.MaxHealth,  // 100
      template.MaxHealth   // 100
  );
    ↓
Check value object:
  if (healthResult.IsFailure)
      return Result.Failure(healthResult.Error)
    ↓ (Success path)
Create pure Domain entity:
  var actor = new Actor(
      id: ActorId.NewId(),           // Generate new ID
      nameKey: template.NameKey,      // "ACTOR_GOBLIN" (from template)
      health: healthResult.Value,     // Health object
      damage: template.Damage         // 15 (from template)
  );

  ★ CRITICAL: Actor has NO reference to template!
  ★ Actor is pure Domain - zero Godot dependency!
    ↓
Register actor: _actorRepository.Add(actor)
    ↓
Log event: "Spawned ACTOR_GOBLIN from template goblin"
    ↓
Publish domain event:
  await _mediator.Publish(new ActorSpawnedEvent(
      ActorId: actor.Id,
      NameKey: actor.NameKey,
      Position: cmd.Position
  ))
    ↓
Presentation layer handles event → ActorNode.OnActorSpawned(evt)
    ↓
Create Godot visual node:
  var sprite = new Sprite2D();
  sprite.Texture = template.Sprite;  // goblin.png
  sprite.Position = evt.Position;
  AddChild(sprite);
    ↓
Display translated name (ADR-005 integration):
  var nameLabel = new Label();
  nameLabel.Text = tr(actor.NameKey);  // "ACTOR_GOBLIN" → "Goblin" or "哥布林"
  AddChild(nameLabel);
    ↓
✅ Goblin visible on screen!
   [Sprite shows goblin.png]
   [Label shows "Goblin" in current locale]
   [Actor exists in Domain repository]
```

**Key Points**:
- Template is **read-only configuration** (never modified)
- Actor is **created from** template data (copied, not referenced)
- Template cached once, many Actor instances created from it
- Actor has **no ongoing dependency** on template (independent lifecycle)

---

### Critical Flow Insights

**1. Data Flow is One-Way**:
```
Template Data (Infrastructure)
    ↓ (data copied into)
Actor Entity (Domain)
    ↓ (referenced by)
ActorNode (Presentation)
```

**2. Template vs Entity Lifetimes**:
- **Templates**: Loaded once at startup, cached forever, read-only, shared across ALL entities
- **Entities**: Created dynamically, many from one template (3 goblins from 1 goblin.tres), mutable (health changes), independent lifecycles

**Analogy**: Template is a **cookie cutter**, entities are **cookies**. Cookie cutter used many times, each cookie is independent, breaking a cookie doesn't break the cutter.

**3. Layer Boundaries Preserved**:
- **Infrastructure**: Uses Godot (GD.Load, ActorTemplate : Resource)
- **Application**: Orchestrates (gets template, creates entity, publishes event)
- **Domain**: Pure C# (Actor has no template reference, no Godot types)
- **Result**: Unit tests can **mock ITemplateService**, no Godot runtime needed

---

### Bonus: Hot-Reload Flow (Designer Iteration)

**When**: Designer edits .tres while game is running

```
Designer edits goblin.tres in external editor:
  MaxHealth: 100 → 120
  Ctrl+S (save)
    ↓
Godot detects file change (FileSystemWatcher)
    ↓
Godot auto-reloads resource:
  GD.Load<ActorTemplate>("res://data/entities/goblin.tres")
  // Re-reads from disk, creates new template object
    ↓
GodotTemplateService cache auto-updates:
  _templates["goblin"] = newly loaded template
  // MaxHealth now 120 in cache
    ↓
EXISTING goblins NOT affected:
  - goblin1.Health = 50/100 (still max 100)
  - goblin2.Health = 80/100 (still max 100)
  ★ Entities already created retain old values (correct behavior)
    ↓
NEXT spawned goblin uses new template:
  SpawnActorCommand("goblin") → new Actor(MaxHealth: 120) ✅
    ↓
✅ Designer sees change in < 5 seconds (no recompile, no restart)
```

**Why existing entities don't update**: Entities are **independent instances** with **copied data**, not live references to templates. This prevents mid-game stat changes (imagine player's health suddenly changing!).

---

### Template Definition (Infrastructure Layer)

```csharp
// Infrastructure/Templates/ActorTemplate.cs
using Godot;

[GlobalClass]
public partial class ActorTemplate : Resource
{
    // Identity
    [Export] public string Id { get; set; } = "";  // "goblin", "orc", "skeleton"

    // i18n (ADR-005 integration)
    [Export] public string NameKey { get; set; } = "";  // "ACTOR_GOBLIN"
    [Export] public string DescriptionKey { get; set; } = "";  // "DESC_ACTOR_GOBLIN"

    // Stats
    [Export] public float MaxHealth { get; set; } = 100f;
    [Export] public float Damage { get; set; } = 10f;
    [Export] public float MoveSpeed { get; set; } = 5f;

    // Visuals
    [Export] public Texture2D Sprite { get; set; } = null!;
    [Export] public Color Tint { get; set; } = Colors.White;

    // Behavior (optional - for future AI integration)
    [Export] public string BehaviorTree { get; set; } = "";  // Path to behavior tree resource
}
```

**Why `[GlobalClass]`**: Makes ActorTemplate appear in Godot's "Create New Resource" menu (designer UX).

**Why `[Export]`**: Godot Inspector shows these properties with type-safe editing (dropdowns, color pickers, resource pickers).

---

### Template Service (Infrastructure Layer)

```csharp
// Infrastructure/Services/ITemplateService.cs (abstraction for testing)
public interface ITemplateService<T> where T : Resource
{
    Result<T> GetTemplate(string id);
    IReadOnlyDictionary<string, T> GetAllTemplates();
}

// Infrastructure/Services/GodotTemplateService.cs (implementation)
public class GodotTemplateService<T> : ITemplateService<T> where T : Resource
{
    private readonly Dictionary<string, T> _templates = new();
    private readonly ILogger<GodotTemplateService<T>> _logger;
    private readonly string _resourcePath;

    public GodotTemplateService(ILogger<GodotTemplateService<T>> logger, string resourcePath)
    {
        _logger = logger;
        _resourcePath = resourcePath;
    }

    public Result LoadTemplates()
    {
        try
        {
            var dir = DirAccess.Open(_resourcePath);
            if (dir == null)
                return Result.Failure($"Template directory not found: {_resourcePath}");

            foreach (var file in dir.GetFiles())
            {
                if (!file.EndsWith(".tres")) continue;

                var path = $"{_resourcePath}/{file}";
                var template = GD.Load<T>(path);

                if (template == null)
                {
                    _logger.LogWarning("Failed to load template: {Path}", path);
                    continue;
                }

                // Extract ID from template (assumes T has Id property via reflection or known interface)
                var idProperty = typeof(T).GetProperty("Id");
                var id = idProperty?.GetValue(template) as string;

                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Template missing Id property: {Path}", path);
                    continue;
                }

                _templates[id] = template;
                _logger.LogInformation("Loaded template: {Id} from {Path}", id, path);
            }

            _logger.LogInformation("Loaded {Count} templates from {Path}", _templates.Count, _resourcePath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to load templates: {ex.Message}");
        }
    }

    public Result<T> GetTemplate(string id)
    {
        if (_templates.TryGetValue(id, out var template))
            return Result.Success(template);

        return Result.Failure<T>($"Template not found: {id}");
    }

    public IReadOnlyDictionary<string, T> GetAllTemplates() => _templates;
}
```

**Key Points**:
- Returns `Result<T>` (ADR-003 integration)
- Uses `ILogger` (ADR-001 - abstractions only)
- Godot dependency (`GD.Load`) isolated to Infrastructure (ADR-001, ADR-002 compliant)
- Abstraction `ITemplateService<T>` enables testing (mock service, no Godot needed)

---

### Domain Entity Creation (Application Layer)

```csharp
// Application/Commands/SpawnActorCommand.cs
public record SpawnActorCommand(string TemplateId, Vector2 Position) : IRequest<Result<Actor>>;

// Application/Commands/SpawnActorCommandHandler.cs
public class SpawnActorCommandHandler : IRequestHandler<SpawnActorCommand, Result<Actor>>
{
    private readonly ITemplateService<ActorTemplate> _templates;
    private readonly ILogger<SpawnActorCommandHandler> _logger;

    public SpawnActorCommandHandler(
        ITemplateService<ActorTemplate> templates,
        ILogger<SpawnActorCommandHandler> logger)
    {
        _templates = templates;
        _logger = logger;
    }

    public async Task<Result<Actor>> Handle(SpawnActorCommand cmd, CancellationToken ct)
    {
        // 1. Get template from Infrastructure
        var templateResult = _templates.GetTemplate(cmd.TemplateId);
        if (templateResult.IsFailure)
            return Result.Failure<Actor>($"Cannot spawn actor: {templateResult.Error}");

        var template = templateResult.Value;

        // 2. Create pure Domain entity from template data
        var healthResult = Health.Create(template.MaxHealth, template.MaxHealth);
        if (healthResult.IsFailure)
            return Result.Failure<Actor>(healthResult.Error);

        var actor = new Actor(
            ActorId.NewId(),
            nameKey: template.NameKey,  // i18n integration (ADR-005)
            health: healthResult.Value,
            damage: template.Damage
        );

        _logger.LogInformation("Spawned actor {NameKey} from template {TemplateId}",
            template.NameKey, cmd.TemplateId);

        // 3. Return pure entity (no Godot dependency)
        return Result.Success(actor);
    }
}
```

**Key Insight**: Domain entity (`Actor`) has **no dependency on template**. Template is pure configuration data consumed during creation.

---

### Designer Workflow (Godot Editor)

**Step 1: Create Template**
```
1. In Godot Editor: res://data/entities/ (right-click)
2. Create New → Resource → ActorTemplate
3. Inspector shows all [Export] properties:
   - Id: "goblin"
   - NameKey: "ACTOR_GOBLIN"
   - MaxHealth: 100
   - Damage: 15
   - Sprite: [drag PNG file]
4. Save as "goblin.tres"
```

**Step 2: Iterate**
```
1. Change MaxHealth: 100 → 120
2. Ctrl+S (save)
3. Game hot-reloads (Godot feature)
4. Test immediately - no recompile!
```

**Step 3: Version Control**
```bash
git add data/entities/goblin.tres
git commit -m "Balance: Increase goblin health 100→120"
git push
```

**Result**: .tres is text file, version control friendly, merge-able.

---

## Integration with ADR-005 (i18n)

### Translation Keys in Templates

Templates store **translation keys**, not translated text:

```gdscript
# data/entities/goblin.tres
[gd_resource type="ActorTemplate"]

[resource]
Id = "goblin"
NameKey = "ACTOR_GOBLIN"  # Translation key, not "Goblin"
DescriptionKey = "DESC_ACTOR_GOBLIN"
MaxHealth = 100.0
```

```csv
# godot_project/translations/en.csv
ACTOR_GOBLIN,Goblin
DESC_ACTOR_GOBLIN,A weak but cunning creature that attacks in groups.

# godot_project/translations/zh_CN.csv
ACTOR_GOBLIN,哥布林
DESC_ACTOR_GOBLIN,虚弱但狡猾的生物，喜欢群体攻击。
```

**Workflow Integration**:
1. Designer creates goblin.tres, sets `NameKey = "ACTOR_GOBLIN"`
2. Designer adds `ACTOR_GOBLIN,Goblin` to `en.csv`
3. Application creates Actor with `NameKey` from template
4. Presentation calls `tr("ACTOR_GOBLIN")` → displays "Goblin" or "哥布林"

**No conflict** - data-driven entities work seamlessly with i18n architecture.

---

## Validation Strategy

### Three-Layer Validation

**1. Design-Time Validation** (Godot Editor):
- `[Export]` attributes provide type checking
- Can't assign string to int property
- Color picker for Color properties
- Resource picker for Texture2D properties

**2. Build-Time Validation** (CI/CD):
```bash
# scripts/validate-templates.sh

#!/bin/bash
echo "Validating entity templates..."

ERRORS=0

# Check 1: All NameKey values exist in en.csv
for template in data/entities/*.tres; do
    NAME_KEY=$(grep 'NameKey = ' "$template" | cut -d'"' -f2)

    if [ -n "$NAME_KEY" ] && ! grep -q "^$NAME_KEY," godot_project/translations/en.csv; then
        echo "❌ ERROR: Missing translation key: $NAME_KEY (from $template)"
        ((ERRORS++))
    fi
done

# Check 2: All template IDs are unique
DUPLICATE_IDS=$(grep 'Id = ' data/entities/*.tres | cut -d'"' -f2 | sort | uniq -d)
if [ -n "$DUPLICATE_IDS" ]; then
    echo "❌ ERROR: Duplicate template IDs found:"
    echo "$DUPLICATE_IDS"
    ((ERRORS++))
fi

# Check 3: All MaxHealth values are positive
BAD_HEALTH=$(grep 'MaxHealth = ' data/entities/*.tres | grep -E 'MaxHealth = (0\.0|-)')
if [ -n "$BAD_HEALTH" ]; then
    echo "❌ ERROR: Invalid MaxHealth values (must be positive):"
    echo "$BAD_HEALTH"
    ((ERRORS++))
fi

if [ $ERRORS -eq 0 ]; then
    echo "✅ All templates validated successfully"
    exit 0
else
    echo "❌ Validation failed with $ERRORS error(s)"
    exit 1
fi
```

**3. Load-Time Validation** (Runtime):
```csharp
public Result LoadTemplates()
{
    // ... load templates ...

    // Validate after loading
    var validationErrors = new List<string>();

    foreach (var (id, template) in _templates)
    {
        if (string.IsNullOrEmpty(template.NameKey))
            validationErrors.Add($"Template '{id}' missing NameKey");

        if (template.MaxHealth <= 0)
            validationErrors.Add($"Template '{id}' has invalid MaxHealth: {template.MaxHealth}");

        if (template.Sprite == null)
            validationErrors.Add($"Template '{id}' missing Sprite");
    }

    if (validationErrors.Any())
    {
        var errors = string.Join("\n", validationErrors);
        return Result.Failure($"Template validation failed:\n{errors}");
    }

    return Result.Success();
}
```

**Fail-Fast Philosophy**: Invalid templates cause startup failure (development), not runtime crash (production).

---

## Schema Evolution

### Adding New Properties

**Scenario**: Add `ArmorRating` property to ActorTemplate.

**Step 1**: Update template class
```csharp
[GlobalClass]
public partial class ActorTemplate : Resource
{
    // ... existing properties ...

    [Export] public float ArmorRating { get; set; } = 0f;  // Default value for backward compatibility
}
```

**Step 2**: Existing templates auto-upgrade
```
Old goblin.tres (doesn't have ArmorRating):
    ↓
Godot loads with default value (ArmorRating = 0f)
    ↓
No error, backward compatible ✅
```

**Step 3**: Designer updates templates
```
Open goblin.tres in Inspector
    ↓
New property appears: ArmorRating = 0
    ↓
Set ArmorRating = 5
    ↓
Save
```

**Godot Resources are schema-flexible** - adding properties with defaults is non-breaking.

---

### Removing Properties (Breaking Change)

**Scenario**: Remove deprecated `OldProperty` from ActorTemplate.

**Migration Process**:
1. **Mark deprecated first**:
   ```csharp
   [Obsolete("Use NewProperty instead. Remove in v2.0")]
   [Export] public float OldProperty { get; set; } = 0f;
   ```

2. **Add migration period** (3-6 months):
   - Old property still exists, logs warning if used
   - Designer updates templates to use NewProperty

3. **Batch migration script**:
   ```bash
   # scripts/migrate-old-property.sh
   for template in data/entities/*.tres; do
       # Read OldProperty value
       OLD_VAL=$(grep 'OldProperty = ' "$template" | cut -d' ' -f3)

       # Write as NewProperty
       sed -i "s/OldProperty = /NewProperty = /" "$template"
   done
   ```

4. **After migration complete, remove OldProperty**

**Text-based .tres files make batch migration possible** (unlike binary formats).

---

## Testing Strategy

### Unit Tests (Mock Template Service)

```csharp
public class SpawnActorCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidTemplate_ShouldCreateActor()
    {
        // Arrange
        var mockTemplates = new Mock<ITemplateService<ActorTemplate>>();
        var goblinTemplate = new ActorTemplate
        {
            Id = "goblin",
            NameKey = "ACTOR_GOBLIN",
            MaxHealth = 100f,
            Damage = 15f
        };

        mockTemplates
            .Setup(t => t.GetTemplate("goblin"))
            .Returns(Result.Success(goblinTemplate));

        var handler = new SpawnActorCommandHandler(mockTemplates.Object, Mock.Of<ILogger>());

        // Act
        var result = await handler.Handle(new SpawnActorCommand("goblin", Vector2.Zero), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NameKey.Should().Be("ACTOR_GOBLIN");
        result.Value.Health.Maximum.Should().Be(100f);
    }

    [Fact]
    public async Task Handle_InvalidTemplateId_ShouldReturnFailure()
    {
        // Arrange
        var mockTemplates = new Mock<ITemplateService<ActorTemplate>>();
        mockTemplates
            .Setup(t => t.GetTemplate("invalid"))
            .Returns(Result.Failure<ActorTemplate>("Template not found: invalid"));

        var handler = new SpawnActorCommandHandler(mockTemplates.Object, Mock.Of<ILogger>());

        // Act
        var result = await handler.Handle(new SpawnActorCommand("invalid", Vector2.Zero), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Template not found");
    }
}
```

**No Godot dependency** - mock ITemplateService for fast unit tests.

---

### Integration Tests (Real .tres Files)

```csharp
public class TemplateServiceIntegrationTests
{
    [Fact]
    public void LoadTemplates_ValidTemplates_ShouldLoadSuccessfully()
    {
        // WHY: Verify real .tres files are valid

        // Arrange
        var service = new GodotTemplateService<ActorTemplate>(
            Mock.Of<ILogger>(),
            "res://data/entities/"
        );

        // Act
        var result = service.LoadTemplates();

        // Assert
        result.IsSuccess.Should().BeTrue();
        service.GetAllTemplates().Should().NotBeEmpty();

        // Verify specific templates exist
        var goblin = service.GetTemplate("goblin");
        goblin.IsSuccess.Should().BeTrue();
        goblin.Value.NameKey.Should().Be("ACTOR_GOBLIN");
    }
}
```

**Integration tests use real .tres files** - catch template validation errors.

---

## Alternatives Considered

### Alternative 1: JSON Files

**Pattern**: Store entity data in JSON files.

```json
// data/entities/goblin.json
{
  "id": "goblin",
  "name_key": "ACTOR_GOBLIN",
  "max_health": 100,
  "damage": 15,
  "sprite_path": "res://sprites/goblin.png"
}
```

**Pros**:
- ✅ Tool-agnostic (any text editor)
- ✅ Portable (can use in non-Godot engine)
- ✅ Simple parsing (JSON.Parse)

**Cons**:
- ❌ **No Godot editor integration** - Designer edits in external tool (VSCode), not Godot Inspector
- ❌ **No type safety** - Typo `"max_helth"` won't be caught until runtime
- ❌ **Manual parsing** - Need custom JSON→ActorTemplate converter
- ❌ **No visual editing** - Can't pick colors, sprites visually
- ❌ **No hot-reload** - Godot doesn't auto-reload JSON changes

**Rejected**: Godot Resources provide superior designer UX with zero parsing code.

---

### Alternative 2: SQLite Database

**Pattern**: Store entity templates in SQLite database.

```sql
CREATE TABLE ActorTemplates (
    id TEXT PRIMARY KEY,
    name_key TEXT NOT NULL,
    max_health REAL NOT NULL,
    damage REAL NOT NULL,
    sprite_path TEXT NOT NULL
);
```

**Pros**:
- ✅ Query-able (find all actors with health > 100)
- ✅ Relational (can join tables)
- ✅ Scalable (MMO-scale content)

**Cons**:
- ❌ **Overkill for single-player game** - We don't need relational queries
- ❌ **External dependency** - SQLite library, database file management
- ❌ **Harder to version control** - Binary file, merge conflicts nightmare
- ❌ **No visual editing** - Designer uses database GUI tool, not Godot
- ❌ **Migration complexity** - Database schema changes require SQL migrations

**Rejected**: Database is for MMO-scale content with complex queries. Roguelikes don't need this.

---

### Alternative 3: Hard-Coded in C#

**Pattern**: Define entities directly in code.

```csharp
public static class EntityLibrary
{
    public static Actor Goblin() => new Actor(
        ActorId.NewId(),
        nameKey: "ACTOR_GOBLIN",
        health: Health.Create(100, 100).Value,
        damage: 15f
    );
}
```

**Pros**:
- ✅ **Type-safe** - Compile-time checked
- ✅ **Simple** - No external files, no parsing
- ✅ **Fast** - No I/O, no deserialization

**Cons**:
- ❌ **Zero designer autonomy** - Programmer bottleneck for all balance changes
- ❌ **Slow iteration** - Change → recompile → restart → test (minutes per tweak)
- ❌ **Merge conflicts** - All designers edit same EntityLibrary.cs
- ❌ **No hot-reload** - Can't see changes without restarting
- ❌ **Not data-driven** - Impossible to mod, impossible to data-mine

**Rejected**: Acceptable for prototypes, unacceptable for content-heavy games.

---

### Alternative 4: ScriptableObjects Pattern (Unity-style)

**Pattern**: Use C# classes that inherit from Resource, with behavior.

```csharp
[GlobalClass]
public partial class GoblinTemplate : Resource
{
    [Export] public float MaxHealth { get; set; } = 100f;

    // Behavior mixed with data
    public Actor CreateActor()
    {
        return new Actor(ActorId.NewId(), "ACTOR_GOBLIN", Health.Create(MaxHealth, MaxHealth).Value);
    }
}
```

**Pros**:
- ✅ Godot editor integration
- ✅ Can embed behavior (CreateActor logic in template)

**Cons**:
- ❌ **Violates Single Responsibility** - Templates are data + behavior
- ❌ **Harder to test** - Behavior in template requires Godot runtime
- ❌ **Not portable** - Behavior tied to Godot Resource lifecycle

**Rejected**: Our templates are **pure data** (no behavior). Application layer handles creation logic. Keeps templates simple and testable.

---

## Consequences

### Positive

✅ **Designer Autonomy**: Balance tweaks without programmer intervention (edit → save → test, no recompile)
✅ **Rapid Iteration**: Hot-reload enables instant feedback (seconds, not minutes)
✅ **Version Control Friendly**: .tres files are text-based, merge-able, diffable
✅ **Type-Safe Editing**: [Export] attributes provide Godot Inspector validation (can't assign string to int)
✅ **Visual Workflow**: Designers work in Godot Editor (color pickers, sprite pickers, dropdowns)
✅ **Clean Architecture Compliant**: Domain remains pure (no Resource dependency), templates are Infrastructure concern
✅ **Testable**: Mock ITemplateService for unit tests (no Godot dependency)
✅ **Modding Potential**: Players can create custom .tres files (future extensibility)
✅ **i18n Compatible**: Templates store translation keys (ADR-005 integration), no hardcoded text
✅ **Schema Flexible**: Adding properties with defaults is backward-compatible
✅ **Scalable**: Supports 100+ entity types without performance issues

### Negative

❌ **Runtime Validation Needed**: No compile-time safety for template data (typos in NameKey won't be caught until validation)
❌ **Godot-Specific**: .tres files only work in Godot (not portable to other engines without conversion)
❌ **Validation Discipline Required**: Team must run validation scripts before commit (CI enforces this)
❌ **Designer Learning Curve**: Designers must learn Godot Inspector (but simpler than JSON editing)
❌ **Key Management**: Template IDs and NameKeys must be manually kept unique (validation scripts help)

### Neutral

➖ **Data vs Code Trade-Off**: Gained iteration speed, lost compile-time safety (acceptable trade-off for content-heavy games)
➖ **Infrastructure Complexity**: TemplateService adds layer vs hard-coded entities (but enables testing + flexibility)

---

## Success Metrics

✅ **Designer can create new enemy** in < 5 minutes (create .tres, set properties, test)
✅ **Balance iteration cycle** < 30 seconds (edit health → save → hot-reload → test)
✅ **Zero programmer involvement** for balance tweaks (designers self-serve)
✅ **100% template validation** at build time (CI catches invalid templates before merge)
✅ **All translation keys validated** (no missing keys in production)
✅ **Unit tests don't require Godot** (mock ITemplateService works)
✅ **Merge conflicts rare** (each entity in separate .tres file)

---

## Implementation Checklist

**Phase 1: Infrastructure Setup** (2-3 hours):
- [ ] Create ActorTemplate.cs with [GlobalClass] + [Export] properties
- [ ] Create ITemplateService<T> interface
- [ ] Create GodotTemplateService<T> implementation
- [ ] Register service in DI container (GameStrapper)
- [ ] Create res://data/entities/ directory

**Phase 2: Validation Scripts** (1-2 hours):
- [ ] Create validate-templates.sh (check keys exist, unique IDs, valid stats)
- [ ] Add to CI pipeline (.github/workflows or .husky/pre-commit)
- [ ] Test with invalid template (ensure validation catches it)

**Phase 3: First Template** (30 min):
- [ ] Create goblin.tres in Godot Editor
- [ ] Set all properties (Id, NameKey, MaxHealth, Damage, Sprite)
- [ ] Add ACTOR_GOBLIN to en.csv
- [ ] Test loading template in-game

**Phase 4: Integration** (1-2 hours):
- [ ] Update SpawnActorCommandHandler to use ITemplateService
- [ ] Test spawning actor from template
- [ ] Verify i18n works (tr(actor.NameKey) displays correctly)
- [ ] Test hot-reload (edit template, see changes without restart)

**Phase 5: Documentation** (30 min):
- [ ] Update CLAUDE.md with data-driven workflow
- [ ] Document designer workflow (how to create templates)
- [ ] Add quick reference for [Export] attribute usage

**Total Estimated Time**: 5-8 hours

---

## References

- [Godot Resources Documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/resources.html) - Official Godot Resource guide
- [Data-Oriented Design Principles](https://www.dataorienteddesign.com/dodbook/) - Why data-driven matters
- [ScriptableObjects in Unity](https://docs.unity3d.com/Manual/class-ScriptableObject.html) - Similar pattern in Unity (for reference)
- [ADR-001: Clean Architecture Foundation](./ADR-001-clean-architecture-foundation.md)
- [ADR-002: Godot Integration Architecture](./ADR-002-godot-integration-architecture.md)
- [ADR-005: Internationalization Architecture](./ADR-005-internationalization-architecture.md)

---

## Future Enhancements

**Deferred until content volume justifies**:

### Template Inheritance (When 50+ similar templates)

**Problem**: Many templates share properties (all humanoid enemies have similar stats).

**Solution**: Godot Resources support inheritance via `script` property:
```gdscript
# data/entities/base_humanoid.tres
[gd_resource]
MaxHealth = 100.0
MoveSpeed = 5.0

# data/entities/goblin.tres (inherits from base_humanoid)
[gd_resource script=base_humanoid.tres]
Damage = 15.0  # Override only what's different
```

**When to implement**: When >50% of templates share common properties.

---

### Visual Template Editor (Custom Godot Plugin)

**Problem**: Godot Inspector is generic (doesn't understand game-specific constraints).

**Solution**: Custom EditorPlugin for template editing:
- Visual stat bars (see health/damage relative to other entities)
- Constraint validation (damage < maxHealth * 0.3)
- Live preview (see sprite + stats together)

**When to implement**: When designers request "better editing UX" (after 50+ templates created).

---

### Template Hot-Reload Notifications

**Problem**: Designer changes template, doesn't know if hot-reload worked.

**Solution**: In-game notification system:
```
"goblin.tres reloaded - 3 instances updated"
```

**When to implement**: When hot-reload becomes primary workflow (after designer comfort level high).

---

**Decision**: **Approved** - Use Godot Resources for entity templates. Enables designer autonomy, rapid iteration, and clean architecture compliance. This is the foundation for content-driven development.
