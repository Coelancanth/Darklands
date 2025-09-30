# Darklands Development Backlog


**Last Updated**: 2025-09-30 15:38 (VS_003 approved with category-based design - Tech Lead decision)

**Last Aging Check**: 2025-08-29
> 📚 See BACKLOG_AGING_PROTOCOL.md for 3-10 day aging rules

## 🔢 Next Item Numbers by Type
**CRITICAL**: Before creating new items, check and update the appropriate counter.

- **Next BR**: 001
- **Next TD**: 001
- **Next VS**: 005 


**Protocol**: Check your type's counter → Use that number → Increment the counter → Update timestamp

## 📖 How to Use This Backlog

### 🧠 Owner-Based Protocol

**Each item has a single Owner persona responsible for decisions and progress.**

#### When You Embody a Persona:
1. **Filter** for items where `Owner: [Your Persona]`
3. **Quick Scan** for other statuses you own (<2 min updates)
4. **Update** the backlog before ending your session
5. **Reassign** owner when handing off to next persona


### Default Ownership Rules
| Item Type | Status | Default Owner | Next Owner |
|-----------|--------|---------------|------------|
| **VS** | Proposed | Product Owner | → Tech Lead (breakdown) |
| **VS** | Approved | Tech Lead | → Dev Engineer (implement) |
| **BR** | New | Test Specialist | → Debugger Expert (complex) |
| **TD** | Proposed | Tech Lead | → Dev Engineer (approved) |

### Pragmatic Documentation Approach
- **Quick items (<1 day)**: 5-10 lines inline below
- **Medium items (1-3 days)**: 15-30 lines inline (like VS_001-003 below)
- **Complex items (>3 days)**: Create separate doc and link here

**Rule**: Start inline. Only extract to separate doc if it grows beyond 30 lines or needs diagrams.

### Adding New Items
```markdown
### [Type]_[Number]: Short Name
**Status**: Proposed | Approved | In Progress | Done
**Owner**: [Persona Name]  ← Single responsible persona
**Size**: S (<4h) | M (4-8h) | L (1-3 days) | XL (>3 days)
**Priority**: Critical | Important | Ideas
**Markers**: [ARCHITECTURE] [SAFETY-CRITICAL] etc. (if applicable)

**What**: One-line description
**Why**: Value in one sentence  
**How**: 3-5 technical approach bullets (if known)
**Done When**: 3-5 acceptance criteria
**Depends On**: Item numbers or None

**[Owner] Decision** (date):  ← Added after ultra-think
- Decision rationale
- Risks considered
- Next steps
```

## 🔥 Critical (Do First)
*Blockers preventing other work, production bugs, dependencies for other features*


### VS_003: Infrastructure - Logging System with Category-Based Filtering [ARCHITECTURE]
**Status**: In Progress (Phase 3/4 Complete - Godot Integration ✅)
**Owner**: Dev Engineer
**Size**: S (4-5h estimated, ~3.5h actual so far)
**Priority**: Critical (Prerequisite for debugging all other features)
**Markers**: [ARCHITECTURE] [INFRASTRUCTURE] [DEVELOPER-EXPERIENCE]
**Created**: 2025-09-30
**Approved**: 2025-09-30 15:38 (category-based filtering)
**Started**: 2025-09-30 16:00
**Reference**: [Control Flow Analysis](../03-Reference/Logging-Control-Flow-Analysis.md)

**What**: Production-grade logging with Serilog, category-based filtering, and three output sinks (Console, File, Godot UI)
**Why**: Enable efficient debugging by filtering domain categories (Combat, Movement, AI) rather than toggling infrastructure (sinks)

---

## 🎯 Tech Lead Decision (2025-09-30 15:38)

**APPROVED: Category-Based Filtering Approach**

### Why This Design Won

1. ✅ **Solves Real User Problem**: Developers debug by domain ("hide Combat spam") not infrastructure ("disable Console sink")
2. ✅ **Better Performance**: O(1) category check before sinks vs O(N) per-sink checks
3. ✅ **Simpler**: ~60 lines vs ~300 lines custom code
4. ✅ **Faster Delivery**: 4-5h vs 6-8h implementation time
5. ✅ **Automatic Categorization**: Extracts from namespaces (zero manual tagging)
6. ✅ **Leverages Serilog**: Uses built-in `Filter.ByIncludingOnly` (battle-tested)
7. ✅ **Extensible**: Can add sink toggling later if proven need emerges (YAGNI)

### Core Architecture Principles

**Single Logger, Three Sinks, Category Filtering**:
- ✅ **ONE** `Log.Logger` configured in GameStrapper
- ✅ **THREE** sinks: Console (ANSI), File (plain text), Godot (BBCode)
- ✅ **Each sink has own formatter** (different outputs need different rendering)
- ✅ **Category filter BEFORE sinks** (efficient: filter once, not per-sink)
- ✅ **Automatic categorization** from namespaces (`Commands.Combat.Handler` → "Combat")
- ✅ **Thread-safe** Godot sink via CallDeferred marshalling
- ✅ **Simple file overwrite** (current session only, no rolling for development)

**Layer Boundaries**:
```
┌─────────────────────────────────────────────────┐
│ Core (Darklands.Core.csproj - NET SDK)         │
│ ✅ Uses: ILogger<T> (MS.Ext.Logging.Abstractions)│
│ ❌ NO ILoggingService interface (YAGNI)         │
│ ❌ NO Serilog packages                          │
└──────────────────┬──────────────────────────────┘
                   │ One-way dependency
                   ↓
┌─────────────────────────────────────────────────┐
│ Presentation (Darklands.csproj - Godot SDK)    │
│ ✅ Implements: LoggingService (simple class)    │
│ ✅ Provides: Serilog as MS.Ext.Logging provider │
│ ✅ Creates: GodotRichTextSink + BBCodeFormatter │
└─────────────────────────────────────────────────┘
```

**Why This is Elegant**:
1. **User-Centric**: Filter by domain (Combat, Movement) not infrastructure (sinks)
2. **Performance**: O(1) category check vs O(N) sink checks
3. **Dependency Inversion**: Core only knows `ILogger<T>` abstraction
4. **Single Responsibility**: Each formatter renders for its medium
5. **Zero Manual Work**: Categories auto-discovered from assembly namespaces
6. **Industry Standard**: Same pattern as ASP.NET Core structured logging

---

## 📋 Implementation Breakdown (4 Phases)

**Total Time**: ~4-5 hours

### **Phase 1: Basic Serilog Setup** (~1h)
**Goal**: Configure Serilog with Console + File sinks, verify Core can log

**Packages to Add** (Darklands.csproj):
```xml
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
```

**GameStrapper Configuration**:
```csharp
// Infrastructure/GameStrapper.cs
public override void _Ready()
{
    // Configure Serilog with TWO sinks (Console + File)
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console(
            theme: AnsiConsoleTheme.Code,
            outputTemplate: "[{Level:u3}] {Timestamp:HH:mm:ss} {SourceContext:l}: {Message:lj}{NewLine}"
        )
        .WriteTo.File(
            "logs/darklands.log",
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext:l}: {Message:lj}{NewLine}",
            shared: true  // ← Simple overwrite (no rolling)
        )
        .CreateLogger();

    // Bridge Serilog → MS.Extensions.Logging
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(Log.Logger, dispose: true);
    });
}
```

**Helper Function**:
```csharp
// Extract category from SourceContext
private static string ExtractCategory(string sourceContext)
{
    // Input: "Darklands.Core.Application.Commands.Combat.ExecuteAttackCommandHandler"
    // Output: "Combat"

    var parts = sourceContext.Split('.');
    var commandsIndex = Array.IndexOf(parts, "Commands");
    if (commandsIndex >= 0 && commandsIndex + 1 < parts.Length)
        return parts[commandsIndex + 1];

    var queriesIndex = Array.IndexOf(parts, "Queries");
    if (queriesIndex >= 0 && queriesIndex + 1 < parts.Length)
        return parts[queriesIndex + 1];

    return "Infrastructure";
}
```

**Test**:
```bash
# Run any command that logs from Core
./scripts/core/build.ps1 test --filter "Category=Phase1"

# Verify:
# - Console shows colored output with SourceContext
# - logs/darklands.log created with plain text
# - Core.csproj has NO Serilog packages (only MS.Ext.Logging.Abstractions)
```

**Commit**: `feat(logging): add basic Serilog with Console + File sinks [VS_003 Phase1/4]`

**✅ Phase 1 Complete (2025-09-30 16:08 | ~1.5h)**:
- Enhanced GameStrapper.Initialize() with configuration action pattern
- Configured Serilog in DIBootstrapTest with Console (ANSI) + File (plain text) sinks
- Bridged Serilog → MS.Extensions.Logging via services.AddSerilog()
- Added test validating configuration callback mechanism
- Fixed test isolation with xUnit Collection (prevents parallel execution)
- Added WHY/ARCHITECTURE comments to all DI tests per CLAUDE.md standards
- **All 14 tests pass** (7 Phase1 + 7 Phase2)
- **Package verification**: Core has ONLY abstractions, Presentation has Serilog
- **Layer boundaries maintained**: Core cannot reference Serilog (compile-time enforced)
- **Time**: ~1.5h actual (includes troubleshooting test isolation and adding comments)

---

### **Phase 2: Category Filtering** (~1.5h)
**Goal**: Add runtime category filtering before sinks

**Files to Create**:
```
Infrastructure/Logging/LoggingService.cs
```

**Update GameStrapper**:
```csharp
public override void _Ready()
{
    // Create shared state for category filtering
    var enabledCategories = new HashSet<string>
    {
        "Combat", "Movement", "AI", "Infrastructure", "Network"
    };

    // Configure Serilog with category filter BEFORE sinks
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()

        // ===== Category Filter (checks once before sinks) =====
        .Filter.ByIncludingOnly(logEvent =>
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var ctx))
            {
                var fullName = ctx.ToString().Trim('"');
                var category = ExtractCategory(fullName);
                return enabledCategories.Contains(category);
            }
            return true;  // Include logs without SourceContext
        })

        .WriteTo.Console(theme: AnsiConsoleTheme.Code, ...)
        .WriteTo.File("logs/darklands.log", ...)
        .CreateLogger();

    // Bridge to MS.Extensions.Logging
    services.AddLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddSerilog(Log.Logger, dispose: true);
    });

    // Register category control service (NO interface in Core!)
    services.AddSingleton(new LoggingService(enabledCategories));
}
```

**LoggingService Implementation**:
```csharp
// Infrastructure/Logging/LoggingService.cs
public class LoggingService
{
    private readonly HashSet<string> _enabledCategories;

    public LoggingService(HashSet<string> enabledCategories)
    {
        _enabledCategories = enabledCategories;
    }

    public void EnableCategory(string category)
    {
        _enabledCategories.Add(category);
    }

    public void DisableCategory(string category)
    {
        _enabledCategories.Remove(category);
    }

    public void ToggleCategory(string category)
    {
        if (_enabledCategories.Contains(category))
            _enabledCategories.Remove(category);
        else
            _enabledCategories.Add(category);
    }

    public IReadOnlySet<string> GetEnabledCategories()
    {
        return _enabledCategories;
    }

    // Auto-discover categories from assembly
    public IReadOnlyList<string> GetAvailableCategories()
    {
        return typeof(GameStrapper).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.Contains("Commands") == true ||
                       t.Namespace?.Contains("Queries") == true)
            .Select(t => ExtractCategory(t.Namespace))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    private static string ExtractCategory(string ns)
    {
        if (string.IsNullOrEmpty(ns)) return null;
        var parts = ns.Split('.');
        var commandsIndex = Array.IndexOf(parts, "Commands");
        if (commandsIndex >= 0 && commandsIndex + 1 < parts.Length)
            return parts[commandsIndex + 1];
        var queriesIndex = Array.IndexOf(parts, "Queries");
        if (queriesIndex >= 0 && queriesIndex + 1 < parts.Length)
            return parts[queriesIndex + 1];
        return null;
    }
}
```

**Test**:
```bash
# Create test that toggles categories at runtime
./scripts/core/build.ps1 test --filter "Category=Phase2"

# Verify:
# - DisableCategory("Combat") → Combat logs disappear from ALL sinks
# - EnableCategory("Combat") → Combat logs resume immediately
# - GetAvailableCategories() returns discovered categories
```

**Commit**: `feat(logging): add category-based runtime filtering [VS_003 Phase2/4]`

**✅ Phase 2 Complete (2025-09-30 16:45 | ~1h)**:
- Created LoggingService with Enable/Disable/Toggle/GetEnabled/GetAvailable methods
- Implemented ExtractCategory() for auto-categorization from CQRS namespaces
- Integrated Filter.ByIncludingOnly() with shared HashSet (O(1) performance)
- Configured default categories: Combat, Movement, AI, Infrastructure, Network
- Manual testing via DIBootstrapTest validates all functionality
- **Screenshot verified**: Category filtering works correctly at runtime
- **All 14 Core tests pass** (Presentation can't be unit tested due to Godot SDK)
- **Time**: 1h actual (as estimated)

---

### **Phase 3: Godot Integration** (~1.5h)
**Goal**: Add GodotRichTextSink with BBCode formatting

**Files to Create**:
```
Infrastructure/Logging/GodotRichTextSink.cs
Infrastructure/Logging/GodotBBCodeFormatter.cs
```

**GodotRichTextSink Implementation**:
```csharp
// Infrastructure/Logging/GodotRichTextSink.cs
public class GodotRichTextSink : ILogEventSink
{
    private readonly RichTextLabel _richTextLabel;
    private readonly ITextFormatter _formatter;

    public GodotRichTextSink(RichTextLabel richTextLabel, ITextFormatter formatter)
    {
        _richTextLabel = richTextLabel;
        _formatter = formatter;
    }

    public void Emit(LogEvent logEvent)
    {
        // Format on background thread (cheap)
        var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        var formatted = writer.ToString();

        // Marshal to main thread (thread-safe)
        _richTextLabel.CallDeferred(
            RichTextLabel.MethodName.AppendText,
            formatted
        );
    }
}
```

**GodotBBCodeFormatter Implementation**:
```csharp
// Infrastructure/Logging/GodotBBCodeFormatter.cs
public class GodotBBCodeFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var level = logEvent.Level;
        var timestamp = logEvent.Timestamp.ToString("HH:mm:ss.fff");
        var message = logEvent.RenderMessage();

        // Extract category from SourceContext
        var category = "Unknown";
        if (logEvent.Properties.TryGetValue("SourceContext", out var ctx))
        {
            var fullName = ctx.ToString().Trim('"');
            category = ExtractCategory(fullName);
        }

        // Render as BBCode
        var color = GetColorForLevel(level);
        output.Write($"[color={color}][b]{level.ToString().ToUpper().PadRight(5)}[/b][/color] ");
        output.Write($"[color=gray]{timestamp}[/color] ");
        output.Write($"[color=cyan]{category}[/color]: ");
        output.WriteLine(message);
    }

    private string GetColorForLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "#666666",      // Dark gray
        LogEventLevel.Debug => "#808080",        // Gray
        LogEventLevel.Information => "#00CED1",  // Cyan
        LogEventLevel.Warning => "#FFD700",      // Gold
        LogEventLevel.Error => "#FF4500",        // OrangeRed
        LogEventLevel.Fatal => "#FF0000",        // Red
        _ => "#FFFFFF"
    };

    private static string ExtractCategory(string sourceContext)
    {
        var parts = sourceContext.Split('.');
        var commandsIndex = Array.IndexOf(parts, "Commands");
        if (commandsIndex >= 0 && commandsIndex + 1 < parts.Length)
            return parts[commandsIndex + 1];
        return "Infrastructure";
    }
}
```

**Update GameStrapper** (add third sink):
```csharp
public override void _Ready()
{
    var enabledCategories = new HashSet<string> { ... };

    // Get RichTextLabel from scene (create minimal debug console first)
    var debugConsole = GetNode<RichTextLabel>("/root/Main/DebugConsole/LogDisplay");
    var godotSink = new GodotRichTextSink(debugConsole, new GodotBBCodeFormatter());

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .Filter.ByIncludingOnly(logEvent => ...)
        .WriteTo.Console(...)
        .WriteTo.File(...)
        .WriteTo.Sink(godotSink)  // ← Add Godot sink
        .CreateLogger();

    // ... rest of setup
}
```

**Test**:
```bash
# Run game manually
# Verify:
# - Logs appear in Godot RichTextLabel with BBCode colors
# - Category filtering affects Godot sink (same as Console/File)
# - Thread-safe (no crashes with concurrent logging)
```

**Commit**: `feat(logging): add Godot rich text sink with BBCode formatting [VS_003 Phase3/4]`

**✅ Phase 3 Complete (2025-09-30 17:15 | ~1h)**:
- Created GodotRichTextSink implementing ILogEventSink for Serilog integration
- Created GodotBBCodeFormatter with 6-color palette (semantic + visual distinction)
- Thread-safe via CallDeferred() marshalling (format on bg thread, UI on main thread)
- Integrated as third sink via .WriteTo.Sink(godotSink)
- Category filter applies to all three sinks (efficient: single filter point)
- Added test logs at Debug/Info/Warning/Error levels for visual validation
- **BBCode colors**: Verbose=#666666, Debug=#808080, Info=#00CED1, Warning=#FFD700, Error=#FF4500, Fatal=#FF0000
- **All 14 Core tests pass** (no regressions)
- **Time**: 1h actual (as estimated)

---

### **Phase 4: Debug UI** (~1h)
**Goal**: Create in-game debug console with category filter UI

**Files to Create**:
```
Nodes/Debug/DebugConsoleNode.cs
Nodes/Debug/LogFilterPanelNode.cs
Scenes/Debug/DebugConsole.tscn
```

**DebugConsoleNode Implementation**:
```csharp
// Nodes/Debug/DebugConsoleNode.cs
public partial class DebugConsoleNode : Control
{
    private RichTextLabel _logDisplay;
    private Button _clearButton;

    public override void _Ready()
    {
        base._Ready();

        _logDisplay = GetNode<RichTextLabel>("VBoxContainer/ScrollContainer/LogDisplay");
        _clearButton = GetNode<Button>("VBoxContainer/HeaderContainer/ClearButton");

        _clearButton.Pressed += () => _logDisplay.Clear();

        // Toggle console with F12
        Visible = false;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.F12)
        {
            Visible = !Visible;
        }
    }

    public RichTextLabel GetLogDisplay() => _logDisplay;
}
```

**LogFilterPanelNode Implementation**:
```csharp
// Nodes/Debug/LogFilterPanelNode.cs
public partial class LogFilterPanelNode : Control
{
    private LoggingService _loggingService;
    private FlowContainer _categoryContainer;

    public override void _Ready()
    {
        base._Ready();

        // Get logging service from DI
        _loggingService = ServiceLocator.Get<LoggingService>();

        _categoryContainer = GetNode<FlowContainer>("CategoryContainer");

        // Auto-discover and create checkboxes
        var categories = _loggingService.GetAvailableCategories();
        foreach (var category in categories)
        {
            var checkbox = new CheckBox
            {
                Text = category,
                ButtonPressed = true  // Enabled by default
            };

            checkbox.Toggled += (isChecked) =>
            {
                if (isChecked)
                    _loggingService.EnableCategory(category);
                else
                    _loggingService.DisableCategory(category);
            };

            _categoryContainer.AddChild(checkbox);
        }
    }
}
```

**Manual Testing Checklist**:
```
1. ✅ Run game
2. ✅ Press F12 → Debug console appears
3. ✅ Logs appear with BBCode colors (different colors per level)
4. ✅ Category checkboxes auto-populated (Combat, Movement, AI, etc.)
5. ✅ Uncheck "Combat" → Combat logs disappear from console
6. ✅ Re-check "Combat" → Combat logs resume immediately
7. ✅ Click "Clear" → Console clears
8. ✅ Verify Console/File show same filtered results
9. ✅ Press F12 again → Console hides
```

**Commit**: `feat(logging): add debug console with category filter UI [VS_003 Phase4/4]`

---

## ✅ Done When (Acceptance Criteria)

- ✅ Core code uses `ILogger<T>` via constructor injection
- ✅ Logs appear in System.Console with ANSI colors
- ✅ Logs appear in Godot in-game console with BBCode formatting
- ✅ Logs persist to `logs/darklands.log` (simple overwrite, no rolling)
- ✅ Can toggle categories on/off at runtime (immediate effect on ALL sinks)
- ✅ Category checkboxes auto-discovered from assembly namespaces
- ✅ Tests verify Core has NO Serilog dependencies (only MS.Ext.Logging.Abstractions)
- ✅ In-game debug console (F12 to toggle)
- ✅ Code committed: `feat: category-based logging system [VS_003]`

---

## 📦 Package Requirements

**Darklands.Core.csproj** (abstractions only):
```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

**Darklands.csproj** (implementations):
```xml
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
```

---

## 🔧 Configuration Notes

### File Logging: Overwrite vs Rolling

**Development** (default):
```csharp
.WriteTo.File(
    "logs/darklands.log",
    shared: true  // Allow concurrent reads (for tail -f)
    // NO rollingInterval = simple overwrite each session
)
```

**Production** (if needed later):
```csharp
.WriteTo.File(
    "logs/darklands-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 7  // Keep last 7 days
)
```

### Formatting Strategy

**Each sink has its own formatter** (different outputs need different rendering):
- **Console**: ANSI color codes (`\x1b[36m`)
- **File**: Plain text (grep-friendly, no escape codes)
- **Godot**: BBCode markup (`[color=cyan]`)

**Same semantic content, different presentation per medium.**

---

## 💡 Future Enhancements (Defer Until Needed)

1. **Hierarchical Categories**: Support `Combat.Attack` vs `Combat.Damage` granularity
2. **Category Presets**: One-click filters ("All", "Errors Only", "Combat Debug")
3. **Save/Load Preferences**: Persist filter state to `user://log_filters.json`
4. **Sink Toggling**: Add if proven need emerges (currently YAGNI)

**YAGNI Principle**: Start with simplest solution, add complexity only when real need emerges.

---

**Depends On**: VS_002 (DI Foundation) ✅ Complete

**Reference Documentation**: [Control Flow Analysis](../03-Reference/Logging-Control-Flow-Analysis.md)

---

### VS_004: Infrastructure - Event Bus System [ARCHITECTURE]
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: S (4-6h)
**Priority**: Critical (Prerequisite for Core → Godot communication)
**Markers**: [ARCHITECTURE] [INFRASTRUCTURE]
**Created**: 2025-09-30

**What**: GodotEventBus to bridge MediatR domain events to Godot nodes
**Why**: Core domain logic needs to notify Godot UI of state changes without coupling

**Scope**:
1. **Infrastructure Layer**:
   - GodotEventBus (subscribes to MediatR INotification)
   - Thread marshalling to main thread (CallDeferred)
   - EventAwareNode base class (subscribe/unsubscribe pattern)
   - Automatic cleanup on node disposal

2. **Tests**:
   - Can publish event from Core
   - Godot node receives event
   - Thread marshalling works correctly
   - Unsubscribe prevents memory leaks

**How** (Implementation Order):
1. **Phase 1: Domain** (~1h)
   - Define event interfaces
   - Simple test event (TestEvent)

2. **Phase 2: Application** (~1h)
   - GodotEventBus implements INotificationHandler<T>
   - Event subscription registry

3. **Phase 3: Infrastructure** (~2h)
   - Thread marshalling implementation
   - EventAwareNode base class
   - Automatic unsubscribe on _ExitTree()

4. **Phase 4: Presentation** (~1h)
   - Simple test scene with EventAwareNode
   - Publish test event from Core
   - Verify node receives event on main thread
   - Manual test: Event updates Godot UI correctly

**Done When**:
- ✅ Can publish MediatR notification from Core
- ✅ Godot nodes receive events via EventBus.Subscribe<T>()
- ✅ Events delivered on main thread (no threading issues)
- ✅ EventAwareNode auto-unsubscribes on disposal
- ✅ Tests verify no memory leaks from subscriptions
- ✅ Simple test scene demonstrates Core → Godot event flow
- ✅ Code committed with message: "feat: event bus system [VS_004]"

**Depends On**: VS_002 (needs DI)

---

### VS_001: Architectural Skeleton - Health System Walking Skeleton [ARCHITECTURE]
**Status**: Proposed
**Owner**: Product Owner → Tech Lead (breakdown)
**Size**: S (4-6h)
**Priority**: Critical (Validates architecture with real feature)
**Markers**: [ARCHITECTURE] [WALKING-SKELETON] [END-TO-END]
**Created**: 2025-09-30
**Updated**: 2025-09-30 (Reduced scope - DI/Logger/EventBus now separate)

**What**: Implement minimal health system to validate complete architecture end-to-end
**Why**: Prove the architecture works with a real feature after infrastructure is in place

**Context**:
- VS_002, VS_003, VS_004 provide foundation (DI, Logger, EventBus)
- This is the FIRST REAL FEATURE using that foundation
- Follow "Walking Skeleton" pattern: thinnest possible slice through all layers
- Validates ADR-001, ADR-002, ADR-003 work together

**Scope** (Minimal Health System):
1. **Domain Layer** (Pure C#):
   - Health value object with Create/Reduce/Increase
   - IHealthComponent interface
   - HealthComponent implementation
   - Use Result<T> for all operations

2. **Application Layer** (CQRS):
   - TakeDamageCommand + Handler (uses ILogger<T>, publishes events)
   - HealthChangedEvent (INotification)
   - Simple in-memory component registry

3. **Infrastructure Layer**:
   - Register health services in GameStrapper
   - ComponentRegistry implementation

4. **Presentation Layer** (Godot):
   - Simple test scene with one actor sprite
   - HealthComponentNode (EventAwareNode, shows health bar)
   - Button to damage actor
   - Verify: Click button → Command → Event → Health bar updates

5. **Tests**:
   - Health value object tests (validation, reduce, increase)
   - TakeDamageCommandHandler tests (with mocked logger)
   - Integration test: Command → Event flow

**NOT in Scope** (defer to later):
- Grid system
- Movement
- Complex combat mechanics
- Multiple actors
- AI
- Turn system
- Fancy UI

**How** (Implementation Order):
1. **Phase 1: Domain** (~1h)
   - Create Health value object with tests
   - Create IHealthComponent + HealthComponent
   - Tests: Health.Create, Reduce, validation with Result<T>

2. **Phase 2: Application** (~2h)
   - TakeDamageCommand + Handler (inject ILogger, IMediator)
   - HealthChangedEvent (INotification)
   - ComponentRegistry service
   - Tests: Handler logic with mocked logger and registry

3. **Phase 3: Infrastructure** (~1h)
   - Register services in GameStrapper
   - ComponentRegistry implementation
   - Verify DI resolution works

4. **Phase 4: Presentation** (~2h)
   - Simple scene (1 sprite + health bar + damage button)
   - HealthComponentNode extends EventAwareNode
   - Subscribe to HealthChangedEvent
   - Wire button click → Send TakeDamageCommand
   - Manual test: Click button → see logs → health bar updates

**Done When**:
- ✅ Build succeeds (dotnet build)
- ✅ Core tests pass (100% pass rate)
- ✅ Godot project loads without errors
- ✅ Can click "Damage" button and health bar updates smoothly
- ✅ Logs appear in debug console showing command execution
- ✅ No Godot references in Darklands.Core project
- ✅ GodotEventBus routes HealthChangedEvent correctly
- ✅ CSharpFunctionalExtensions Result<T> works end-to-end
- ✅ All 3 ADRs validated with working code
- ✅ Code committed with message: "feat: health system walking skeleton [VS_001]"

**Depends On**: VS_002 (DI), VS_003 (Logger), VS_004 (EventBus)

**Product Owner Notes** (2025-09-30):
- This is the FOUNDATION - everything else builds on this
- Keep it MINIMAL - resist adding features
- Validate architecture first, optimize later
- Success = simple but complete end-to-end flow

**Acceptance Test Script**:
```
1. Run: dotnet build src/Darklands.Core/Darklands.Core.csproj
   Expected: Build succeeds, no warnings

2. Run: dotnet test tests/Darklands.Core.Tests/Darklands.Core.Tests.csproj
   Expected: All tests pass

3. Open Godot project
   Expected: No errors in console

4. Run test scene
   Expected: See sprite with health bar above it

5. Click "Damage" button
   Expected: Health bar decreases, animation plays

6. Click repeatedly until health reaches 0
   Expected: Sprite disappears or "Dead" appears
```

---




---

## 📈 Important (Do Next)
*Core features for current milestone, technical debt affecting velocity*



---

## 📋 Quick Reference

**Priority Decision Framework:**
1. **Blocking other work?** → 🔥 Critical
2. **Current milestone?** → 📈 Important  
3. **Everything else** → 💡 Ideas

**Work Item Types:**
- **VS_xxx**: Vertical Slice (new feature) - Product Owner creates
- **BR_xxx**: Bug Report (investigation) - Test Specialist creates, Debugger owns
- **TD_xxx**: Technical Debt (refactoring) - Anyone proposes → Tech Lead approves

*Notes:*
- *Critical bugs are BR items with 🔥 priority*
- *TD items need Tech Lead approval to move from "Proposed" to actionable*



---



---
*Single Source of Truth for all Darklands development work. Simple, maintainable, actually used.*