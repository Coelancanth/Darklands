# Logging System Control Flow Analysis

**Date**: 2025-09-30
**Context**: VS_003 Technical Analysis - Sink-Based vs Category-Based Filtering
**Author**: Tech Lead

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Scenario Setup](#scenario-setup)
3. [Original Design: Sink-Based Toggling](#original-design-sink-based-toggling)
4. [Proposed Design: Category-Based Filtering](#proposed-design-category-based-filtering)
5. [Runtime Control Mechanism](#runtime-control-mechanism)
6. [Performance Comparison](#performance-comparison)
7. [Godot UI Integration](#godot-ui-integration)
8. [Recommendation](#recommendation)

---

## Executive Summary

This document compares two architectural approaches for runtime logging control:

- **Original Design**: Toggle individual sinks (Console, File, Godot) at runtime
- **Proposed Design**: Filter by domain categories (Combat, Movement, AI) before sinks

**Key Insight**: Both use the same **closure-over-mutable-state** pattern for runtime control. The difference is **what** gets filtered (sinks vs categories) and **where** filtering happens (per-sink vs before-sinks).

---

## Scenario Setup

### Core Code Logs a Message

```csharp
// Core/Application/Commands/Combat/ExecuteAttackCommandHandler.cs
public class ExecuteAttackCommandHandler
{
    private readonly ILogger<ExecuteAttackCommandHandler> _logger;

    public async Task<Result> Handle(ExecuteAttackCommand cmd)
    {
        // This line triggers the entire logging pipeline
        _logger.LogInformation("Attack executed: {Damage}", cmd.Damage);
    }
}
```

**Question**: How does this message reach Console, File, and Godot UI? How can we control it at runtime?

---

## Original Design: Sink-Based Toggling

### Architecture Overview

```mermaid
graph TB
    subgraph "Core Layer"
        Handler[ExecuteAttackCommandHandler]
        ILogger[ILogger&lt;T&gt;]
        Handler -->|Uses| ILogger
    end

    subgraph "Serilog Pipeline"
        Logger[Log.Logger]
        LevelCheck{Level >= Minimum?}
        ILogger -->|Bridge| Logger
        Logger --> LevelCheck
    end

    subgraph "Conditional Sinks"
        LevelCheck -->|Pass| ConsoleCond[ConditionalConsoleSink]
        LevelCheck -->|Pass| FileCond[ConditionalFileSink]
        LevelCheck -->|Pass| GodotCond[ConditionalGodotSink]

        ConsoleCond -->|Check Toggle| ConsoleEnabled{IsEnabled?}
        FileCond -->|Check Toggle| FileEnabled{IsEnabled?}
        GodotCond -->|Check Toggle| GodotEnabled{IsEnabled?}

        ConsoleEnabled -->|Yes| ConsoleOutput[Console.WriteLine]
        ConsoleEnabled -->|No| ConsoleDrop[Drop]

        FileEnabled -->|Yes| FileOutput[File.AppendText]
        FileEnabled -->|No| FileDrop[Drop]

        GodotEnabled -->|Yes| GodotOutput[RichTextLabel.AppendText]
        GodotEnabled -->|No| GodotDrop[Drop]
    end

    subgraph "Shared State"
        ToggleDict[Dictionary&lt;string, SinkToggle&gt;]
        ConsoleEnabled -.->|Reads| ToggleDict
        FileEnabled -.->|Reads| ToggleDict
        GodotEnabled -.->|Reads| ToggleDict
    end

    subgraph "Godot UI"
        Checkbox[CheckBox: Console]
        Service[ILoggingService]
        Checkbox -->|Toggled| Service
        Service -->|Mutates| ToggleDict
    end

    style Handler fill:#e1f5ff
    style Logger fill:#fff4e1
    style ConsoleCond fill:#ffe1e1
    style FileCond fill:#ffe1e1
    style GodotCond fill:#ffe1e1
    style ToggleDict fill:#e1ffe1
```

### Initialization Flow (GameStrapper)

```mermaid
sequenceDiagram
    participant GS as GameStrapper
    participant Serilog as Serilog Config
    participant DI as DI Container
    participant Toggle as SinkToggle Objects

    Note over GS: _Ready() at app startup

    GS->>Toggle: Create SinkToggle objects
    activate Toggle
    Note right of Toggle: { "Console": IsEnabled=true,<br/>"File": IsEnabled=true,<br/>"Godot": IsEnabled=true }

    GS->>Serilog: Create ConditionalConsoleSink
    Note right of Serilog: Closure captures () => toggles["Console"].IsEnabled

    GS->>Serilog: Create ConditionalFileSink
    Note right of Serilog: Closure captures () => toggles["File"].IsEnabled

    GS->>Serilog: Create ConditionalGodotSink
    Note right of Serilog: Closure captures () => toggles["Godot"].IsEnabled

    GS->>Serilog: Configure Log.Logger
    Note right of Serilog: .WriteTo.Sink(conditionalConsoleSink)<br/>.WriteTo.Sink(conditionalFileSink)<br/>.WriteTo.Sink(conditionalGodotSink)

    GS->>DI: Register ILoggingService
    Note right of DI: new LoggingService(toggles)
    deactivate Toggle
```

### Runtime Execution Flow

```mermaid
sequenceDiagram
    participant Core as Core Handler
    participant Serilog as Log.Logger
    participant ConsoleSink as ConditionalConsoleSink
    participant FileSink as ConditionalFileSink
    participant GodotSink as ConditionalGodotSink
    participant Toggle as SinkToggle Dict
    participant Console as System.Console
    participant File as File System
    participant Godot as RichTextLabel

    Core->>Serilog: LogInformation("Attack executed")

    Serilog->>Serilog: Check minimum level
    Note right of Serilog: Level >= Debug? ✅

    par Parallel Sink Processing
        Serilog->>ConsoleSink: Emit(logEvent)
        ConsoleSink->>Toggle: Check toggles["Console"].IsEnabled
        Toggle-->>ConsoleSink: true
        ConsoleSink->>Console: WriteLine("[INFO] Attack executed")

    and
        Serilog->>FileSink: Emit(logEvent)
        FileSink->>Toggle: Check toggles["File"].IsEnabled
        Toggle-->>FileSink: true
        FileSink->>File: AppendText("2025-09-30 [INFO] Attack executed")

    and
        Serilog->>GodotSink: Emit(logEvent)
        GodotSink->>Toggle: Check toggles["Godot"].IsEnabled
        Toggle-->>GodotSink: true
        GodotSink->>Godot: CallDeferred(AppendText, "[color=cyan]INFO[/color]...")
    end
```

### Runtime Toggle Flow

```mermaid
sequenceDiagram
    participant User as User
    participant Checkbox as CheckBox UI
    participant Node as LogSettingsPanelNode
    participant Service as ILoggingService
    participant Toggle as SinkToggle Dict
    participant NextLog as Next Log Event
    participant Sink as ConditionalConsoleSink

    User->>Checkbox: Click "Console" (uncheck)
    Checkbox->>Node: Toggled signal (isChecked=false)
    Node->>Service: DisableSink("Console")
    Service->>Toggle: toggles["Console"].IsEnabled = false
    Note right of Toggle: Shared state mutated

    Note over NextLog: Some time later...
    NextLog->>Sink: Emit(logEvent)
    Sink->>Toggle: Check toggles["Console"].IsEnabled
    Toggle-->>Sink: false ❌
    Sink->>Sink: Drop event (no output)

    Note over User: User re-checks "Console"
    User->>Checkbox: Click "Console" (check)
    Checkbox->>Node: Toggled signal (isChecked=true)
    Node->>Service: EnableSink("Console")
    Service->>Toggle: toggles["Console"].IsEnabled = true

    Note over NextLog: Next log...
    NextLog->>Sink: Emit(logEvent)
    Sink->>Toggle: Check toggles["Console"].IsEnabled
    Toggle-->>Sink: true ✅
    Sink->>Sink: Emit to Console
```

### Code Structure

```csharp
// Infrastructure/Logging/SinkToggle.cs
public class SinkToggle
{
    public bool IsEnabled { get; set; } = true;
}

// Infrastructure/Logging/ConditionalSink.cs
public class ConditionalSink : ILogEventSink
{
    private readonly ILogEventSink _innerSink;
    private readonly Func<bool> _isEnabled;  // Closure captures toggle reference

    public ConditionalSink(ILogEventSink innerSink, Func<bool> isEnabled)
    {
        _innerSink = innerSink;
        _isEnabled = isEnabled;
    }

    public void Emit(LogEvent logEvent)
    {
        if (_isEnabled())  // Check shared state (~1ns)
            _innerSink.Emit(logEvent);
    }
}

// Infrastructure/GameStrapper.cs
public override void _Ready()
{
    // Create shared state
    var sinkToggles = new Dictionary<string, SinkToggle>
    {
        ["Console"] = new SinkToggle { IsEnabled = true },
        ["File"] = new SinkToggle { IsEnabled = true },
        ["Godot"] = new SinkToggle { IsEnabled = true }
    };

    // Configure Serilog with conditional sinks
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Sink(new ConditionalSink(
            new ConsoleSink(),
            () => sinkToggles["Console"].IsEnabled))  // Closure!
        .WriteTo.Sink(new ConditionalSink(
            new FileSink("logs/darklands-.log"),
            () => sinkToggles["File"].IsEnabled))
        .WriteTo.Sink(new ConditionalSink(
            new GodotRichTextSink(GetNode<RichTextLabel>(...)),
            () => sinkToggles["Godot"].IsEnabled))
        .CreateLogger();

    // Register control service
    services.AddSingleton<ILoggingService>(
        new LoggingService(sinkToggles));
}

// Infrastructure/Logging/LoggingService.cs
public class LoggingService : ILoggingService
{
    private readonly Dictionary<string, SinkToggle> _sinkToggles;

    public void DisableSink(string sinkName)
    {
        if (_sinkToggles.TryGetValue(sinkName, out var toggle))
            toggle.IsEnabled = false;  // Mutate shared state
    }

    public void EnableSink(string sinkName)
    {
        if (_sinkToggles.TryGetValue(sinkName, out var toggle))
            toggle.IsEnabled = true;
    }
}
```

---

## Proposed Design: Category-Based Filtering

### Architecture Overview

```mermaid
graph TB
    subgraph "Core Layer"
        Handler[ExecuteAttackCommandHandler]
        ILogger[ILogger&lt;T&gt;]
        Handler -->|Uses| ILogger

        Note1[Note: Namespace = ...Commands.Combat.ExecuteAttackCommandHandler]
        Handler -.->|Auto-enriched| Note1
    end

    subgraph "Serilog Pipeline"
        Logger[Log.Logger]
        LevelCheck{Level >= Minimum?}
        CategoryFilter{Category Enabled?}

        ILogger -->|Bridge| Logger
        Logger --> LevelCheck
        LevelCheck -->|Pass| CategoryFilter

        CategoryFilter -->|Extract SourceContext| Extract[Extract: 'Combat']
        Extract -->|Check HashSet| EnabledSet{Contains 'Combat'?}
        EnabledSet -->|Yes| PassThrough[Pass to Sinks]
        EnabledSet -->|No| Drop[Drop Event]
    end

    subgraph "Simple Sinks (No Wrappers)"
        PassThrough --> Console[ConsoleSink]
        PassThrough --> File[FileSink]
        PassThrough --> Godot[GodotRichTextSink]

        Console --> ConsoleOut[Console.WriteLine]
        File --> FileOut[File.AppendText]
        Godot --> GodotOut[RichTextLabel.AppendText]
    end

    subgraph "Shared State"
        Categories[HashSet&lt;string&gt; enabledCategories]
        EnabledSet -.->|Reads| Categories
    end

    subgraph "Godot UI"
        Checkbox[CheckBox: Combat]
        Service[LoggingService]
        Checkbox -->|Toggled| Service
        Service -->|Mutates| Categories
    end

    style Handler fill:#e1f5ff
    style Logger fill:#fff4e1
    style CategoryFilter fill:#ffe1f5
    style PassThrough fill:#e1ffe1
    style Drop fill:#ffcccc
    style Categories fill:#e1ffe1
```

### Initialization Flow (GameStrapper)

```mermaid
sequenceDiagram
    participant GS as GameStrapper
    participant Serilog as Serilog Config
    participant DI as DI Container
    participant Categories as HashSet<string>

    Note over GS: _Ready() at app startup

    GS->>Categories: Create HashSet
    activate Categories
    Note right of Categories: { "Combat", "Movement",<br/>"AI", "Infrastructure", "Network" }

    GS->>Serilog: Configure Filter.ByIncludingOnly
    Note right of Serilog: Closure captures reference:<br/>() => categories.Contains(ExtractCategory(event))

    GS->>Serilog: Add simple sinks (NO wrappers)
    Note right of Serilog: .WriteTo.Console()<br/>.WriteTo.File()<br/>.WriteTo.Sink(godotSink)

    GS->>DI: Register LoggingService
    Note right of DI: new LoggingService(categories)
    deactivate Categories
```

### Runtime Execution Flow

```mermaid
sequenceDiagram
    participant Core as Core Handler
    participant Serilog as Log.Logger
    participant Filter as Category Filter
    participant Categories as HashSet
    participant Console as ConsoleSink
    participant File as FileSink
    participant Godot as GodotRichTextSink

    Core->>Serilog: LogInformation("Attack executed")
    Note right of Core: SourceContext = "...Commands.Combat.ExecuteAttackCommandHandler"

    Serilog->>Serilog: Check minimum level
    Note right of Serilog: Level >= Debug? ✅

    Serilog->>Filter: Apply Filter.ByIncludingOnly
    Filter->>Filter: Extract category from SourceContext
    Note right of Filter: "...Commands.Combat.ExecuteAttackCommandHandler"<br/>→ "Combat"

    Filter->>Categories: Contains("Combat")?
    Categories-->>Filter: true ✅

    Note over Filter: Event PASSES filter

    par Parallel Sink Delivery
        Filter->>Console: Emit(logEvent)
        Console->>Console: WriteLine("[INFO] Combat: Attack executed")

    and
        Filter->>File: Emit(logEvent)
        File->>File: AppendText("2025-09-30 [INFO] Combat: Attack executed")

    and
        Filter->>Godot: Emit(logEvent)
        Godot->>Godot: CallDeferred(AppendText, "[color=cyan]INFO[/color]...")
    end
```

### Runtime Toggle Flow

```mermaid
sequenceDiagram
    participant User as User
    participant Checkbox as CheckBox UI
    participant Node as LogFilterPanelNode
    participant Service as LoggingService
    participant Categories as HashSet
    participant NextLog as Next Log Event
    participant Filter as Category Filter

    User->>Checkbox: Click "Combat" (uncheck)
    Checkbox->>Node: Toggled signal (isChecked=false)
    Node->>Service: DisableCategory("Combat")
    Service->>Categories: Remove("Combat")
    Note right of Categories: Shared state mutated

    Note over NextLog: Some time later...
    NextLog->>Filter: LogInformation("Attack executed")
    Filter->>Filter: Extract category → "Combat"
    Filter->>Categories: Contains("Combat")?
    Categories-->>Filter: false ❌
    Filter->>Filter: DROP event
    Note right of Filter: Sinks NEVER see this event

    Note over User: User re-checks "Combat"
    User->>Checkbox: Click "Combat" (check)
    Checkbox->>Node: Toggled signal (isChecked=true)
    Node->>Service: EnableCategory("Combat")
    Service->>Categories: Add("Combat")

    Note over NextLog: Next log...
    NextLog->>Filter: LogInformation("Another attack")
    Filter->>Filter: Extract category → "Combat"
    Filter->>Categories: Contains("Combat")?
    Categories-->>Filter: true ✅
    Filter->>Filter: Pass to all sinks
```

### Code Structure

```csharp
// Infrastructure/GameStrapper.cs
public override void _Ready()
{
    // Create shared state
    var enabledCategories = new HashSet<string>
    {
        "Combat", "Movement", "AI", "Infrastructure", "Network"
    };

    // Configure Serilog with category filter BEFORE sinks
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()

        // Filter stage - closure captures HashSet reference
        .Filter.ByIncludingOnly(logEvent =>
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var ctx))
            {
                var fullName = ctx.ToString().Trim('"');
                var category = ExtractCategory(fullName);
                return enabledCategories.Contains(category);  // Check shared state
            }
            return false;
        })

        // Simple sinks (NO wrappers needed)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .WriteTo.File("logs/darklands-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.Sink(new GodotRichTextSink(GetNode<RichTextLabel>(...)))
        .CreateLogger();

    // Register control service (NO interface in Core)
    services.AddSingleton(new LoggingService(enabledCategories));
}

private static string ExtractCategory(string sourceContext)
{
    // Input: "Darklands.Core.Application.Commands.Combat.ExecuteAttackCommandHandler"
    // Output: "Combat"

    var parts = sourceContext.Split('.');
    var commandsIndex = Array.IndexOf(parts, "Commands");
    if (commandsIndex >= 0 && commandsIndex + 1 < parts.Length)
        return parts[commandsIndex + 1];

    return "Unknown";
}

// Infrastructure/Logging/LoggingService.cs (NO interface in Core!)
public class LoggingService
{
    private readonly HashSet<string> _enabledCategories;

    public void DisableCategory(string category)
    {
        _enabledCategories.Remove(category);  // Mutate shared state
    }

    public void EnableCategory(string category)
    {
        _enabledCategories.Add(category);
    }

    // Auto-discover categories from assembly
    public IReadOnlyList<string> GetAvailableCategories()
    {
        return typeof(ExecuteAttackCommandHandler).Assembly
            .GetTypes()
            .Where(t => t.Namespace?.Contains("Commands") == true ||
                       t.Namespace?.Contains("Queries") == true)
            .Select(t => ExtractCategory(t.Namespace))
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }
}
```

---

## Runtime Control Mechanism

### The Closure Pattern (Both Approaches)

Both designs use **C# closures over mutable state** for runtime control without logger recreation.

```mermaid
graph LR
    subgraph "At Startup"
        Create[Create Mutable Object]
        Closure[Create Closure]
        Create -->|Reference| Closure
    end

    subgraph "Closure Capture"
        Closure -->|Captures| Ref[Reference to Object]
        Note1[Not a copy - a pointer!]
        Ref -.-> Note1
    end

    subgraph "At Runtime"
        Mutate[Mutate Object]
        NextCall[Next Closure Invocation]
        Mutate -->|Changes| Ref
        NextCall -->|Reads via| Ref
        NextCall -->|Sees| NewValue[New Value]
    end

    style Create fill:#e1f5ff
    style Closure fill:#fff4e1
    style Ref fill:#ffe1e1
    style Mutate fill:#e1ffe1
```

### Original Design: Closure Over SinkToggle

```csharp
// At startup
var toggle = new SinkToggle { IsEnabled = true };

// Closure captures REFERENCE to toggle object
var conditionalSink = new ConditionalSink(
    innerSink,
    () => toggle.IsEnabled  // Closure reads from 'toggle' reference
    //    ^^^^^
    //    Captured reference (not value!)
);

// Later at runtime
toggle.IsEnabled = false;  // Mutate the SAME object

// Next log event
conditionalSink.Emit(event);  // Closure re-evaluates () => toggle.IsEnabled
                               // Sees new value: false
```

### Proposed Design: Closure Over HashSet

```csharp
// At startup
var categories = new HashSet<string> { "Combat", "Movement" };

// Closure captures REFERENCE to HashSet
.Filter.ByIncludingOnly(e =>
    categories.Contains(ExtractCategory(e))  // Closure reads from 'categories' reference
    //         ^^^^^
    //         Captured reference (not snapshot!)
)

// Later at runtime
categories.Remove("Combat");  // Mutate the SAME HashSet

// Next log event
// Filter re-evaluates: categories.Contains("Combat")
// Sees new state: false (not in set anymore)
```

### Why This Works Without Logger Recreation

```mermaid
sequenceDiagram
    participant Startup as Startup Code
    participant Obj as Mutable Object
    participant Closure as Closure
    participant Heap as Heap Memory

    Note over Startup: Create object
    Startup->>Heap: Allocate SinkToggle/HashSet
    Heap-->>Obj: Reference (memory address)

    Note over Startup: Create closure
    Startup->>Closure: Create lambda
    Closure->>Heap: Capture reference to object
    Note right of Closure: Closure holds POINTER,<br/>not VALUE

    Note over Startup: Time passes...

    Note over Startup: Runtime mutation
    Startup->>Heap: Mutate object state
    Note right of Heap: Same memory address,<br/>new contents

    Note over Startup: Next log event
    activate Closure
    Closure->>Heap: Dereference pointer
    Heap-->>Closure: Current state (mutated!)
    Closure->>Closure: Evaluate condition with new state
    deactivate Closure
```

**Key Insight**: Closures capture **references** (pointers), not **values** (copies). When you mutate the referenced object, all closures reading from it see the new state immediately. This is standard C# closure behavior.

---

## Performance Comparison

### Checks Per Log Event

```mermaid
graph LR
    subgraph "Original Design"
        O1[Core logs] --> O2[Level check]
        O2 --> O3[ConditionalConsoleSink check]
        O2 --> O4[ConditionalFileSink check]
        O2 --> O5[ConditionalGodotSink check]

        O3 -->|If enabled| O6[Console output]
        O4 -->|If enabled| O7[File output]
        O5 -->|If enabled| O8[Godot output]
    end

    subgraph "Proposed Design"
        P1[Core logs] --> P2[Level check]
        P2 --> P3[Category check]
        P3 -->|If enabled| P4[ALL sinks receive]

        P4 --> P5[Console output]
        P4 --> P6[File output]
        P4 --> P7[Godot output]
    end

    style O2 fill:#ffe1e1
    style O3 fill:#ffe1e1
    style O4 fill:#ffe1e1
    style O5 fill:#ffe1e1
    style P2 fill:#e1ffe1
    style P3 fill:#e1ffe1
```

### Complexity Analysis

| Aspect | Original Design | Proposed Design | Winner |
|--------|----------------|-----------------|--------|
| **Checks Per Log** | 1 level + N sinks | 1 level + 1 category | Proposed (O(1) vs O(N)) |
| **Dropped Events** | Distributed to all ConditionalSinks | Dropped before distribution | Proposed (fewer function calls) |
| **Formatting Work** | Each sink formats independently | Only if filter passes | Same |
| **Memory Allocations** | SinkToggle objects (3) | HashSet entries (5-10) | Similar |
| **Boolean Checks** | 3 per log (if 3 sinks) | 1 HashSet lookup per log | Proposed (faster) |

### Benchmark Estimate

Assuming 1000 log events per second:

**Original Design**:
- 1000 level checks
- 3000 sink toggle checks (1000 × 3 sinks)
- 3000 function calls to ConditionalSink.Emit()
- **Total: ~4000 conditional checks**

**Proposed Design**:
- 1000 level checks
- 1000 category HashSet lookups
- If passed: 3000 function calls to sinks
- **Total: ~2000 conditional checks** (50% reduction)

**When logs are filtered out**:
- Original: Still calls all 3 ConditionalSink.Emit() (waste)
- Proposed: Drops before sink distribution (efficient)

---

## Godot UI Integration

### UI Structure Comparison

```mermaid
graph TB
    subgraph "Original Design UI"
        O_Panel[DebugConsoleNode]
        O_Panel --> O_Logs[LogsTab: RichTextLabel]
        O_Panel --> O_Settings[SettingsTab]

        O_Settings --> O_Level[LevelDropdown]
        O_Settings --> O_Sinks[Sink Toggles]

        O_Sinks --> O_Console[CheckBox: Console]
        O_Sinks --> O_File[CheckBox: File]
        O_Sinks --> O_Godot[CheckBox: Godot]
    end

    subgraph "Proposed Design UI"
        P_Panel[DebugConsoleNode]
        P_Panel --> P_Logs[LogsTab: RichTextLabel]
        P_Panel --> P_Settings[SettingsTab]

        P_Settings --> P_Level[LevelDropdown]
        P_Settings --> P_Categories[Category Filters]
        P_Settings --> P_Presets[Preset Dropdown]

        P_Categories --> P_Combat[CheckBox: Combat]
        P_Categories --> P_Movement[CheckBox: Movement]
        P_Categories --> P_AI[CheckBox: AI]
        P_Categories --> P_Infra[CheckBox: Infrastructure]
        P_Categories --> P_Network[CheckBox: Network]
        P_Categories --> P_More[...auto-discovered]
    end

    style O_Sinks fill:#ffe1e1
    style P_Categories fill:#e1ffe1
```

### Godot Node Implementation

**Original Design**:
```csharp
// Nodes/Debug/LogSettingsPanelNode.cs
public partial class LogSettingsPanelNode : Control
{
    private ILoggingService _loggingService;

    public override void _Ready()
    {
        base._Ready();

        // Get service from DI container
        _loggingService = ServiceLocator.Get<ILoggingService>();

        // Wire up FIXED checkboxes (hardcoded sinks)
        var consoleCheckbox = GetNode<CheckBox>("SinkToggles/ConsoleCheckbox");
        consoleCheckbox.Toggled += (isChecked) =>
        {
            if (isChecked)
                _loggingService.EnableSink("Console");
            else
                _loggingService.DisableSink("Console");
        };

        // Repeat for File, Godot...
    }
}
```

**Proposed Design**:
```csharp
// Nodes/Debug/LogFilterPanelNode.cs
public partial class LogFilterPanelNode : Control
{
    private LoggingService _loggingService;

    public override void _Ready()
    {
        base._Ready();

        _loggingService = ServiceLocator.Get<LoggingService>();

        // Auto-discover categories from assembly
        var categories = _loggingService.GetAvailableCategories();
        // → ["AI", "Combat", "Infrastructure", "Movement", "Network"]

        // Dynamically create checkboxes
        var container = GetNode<FlowContainer>("CategoryFilters");
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

            container.AddChild(checkbox);
        }
    }
}
```

### ServiceLocator Pattern at Godot Boundary

```mermaid
sequenceDiagram
    participant Godot as Godot Scene Loader
    participant Node as LogSettingsPanelNode
    participant SL as ServiceLocator
    participant DI as DI Container
    participant Service as LoggingService

    Note over Godot: Load scene file (.tscn)
    Godot->>Node: Instantiate() via reflection
    Note right of Godot: Godot uses new Node()<br/>(not DI constructor)

    Godot->>Node: _Ready()
    activate Node

    Node->>SL: Get<LoggingService>()
    SL->>DI: Resolve<LoggingService>()
    DI-->>SL: Return singleton instance
    SL-->>Node: Return service reference

    Node->>Node: Store reference in field
    Node->>Service: Wire up UI signals

    deactivate Node

    Note over Node: Later: User clicks checkbox
    Node->>Service: DisableCategory("Combat")
    Service->>Service: Mutate HashSet
```

**Why ServiceLocator Here?**

Godot instantiates nodes via scene loading, not DI. We **cannot** use constructor injection. ServiceLocator bridges Godot's instantiation model to our DI container. This is acceptable at the **framework boundary**—it's isolated to `_Ready()` methods in presentation layer.

---

## Recommendation

### Accept Dev Engineer's Category-Based Proposal

**Reasons**:

1. ✅ **Solves Real User Problem**: Filter by domain (Combat, Movement) matches debugging mental model
2. ✅ **Better Performance**: O(1) category check vs O(N) sink checks
3. ✅ **Simpler Implementation**: ~60 lines vs ~300 lines core logic
4. ✅ **Faster Delivery**: 3-4 hours vs 6-8 hours
5. ✅ **Automatic Categorization**: Extracts from namespaces (zero manual tagging)
6. ✅ **Extensible**: Can add sink toggling later if proven need emerges
7. ✅ **Leverages Serilog**: Uses built-in `Filter.ByIncludingOnly` (battle-tested)

### Optional Enhancements

**Hierarchical Categories** (+30min):
```csharp
EnableCategory("Combat.Attack");  // Fine-grained
EnableCategory("Combat");          // All Combat.*
```

**Presets** (+15min):
```csharp
ApplyPreset("All");            // Everything
ApplyPreset("ErrorsOnly");     // Production debugging
ApplyPreset("CombatDebug");    // Combat + related systems
```

**Save/Load Preferences** (+30min):
```csharp
SaveFilterPreferences();   // Persist to user://log_filters.json
LoadFilterPreferences();   // Restore on launch
```

**Total Time**: ~5 hours (vs 6-8 for original)

### Decision Rationale

**Original design is technically sound but over-engineered.**

- Builds flexibility for scenarios that don't exist ("toggle sinks at runtime")
- Doesn't match developer mental model (infrastructure vs domain)
- Reinvents Serilog's built-in filtering
- Higher complexity (300 lines vs 60 lines)
- Longer implementation time (6-8h vs 3-4h)

**Category-based design solves the actual problem**:
- Matches real debugging workflows ("hide Combat spam")
- Leverages Serilog's native capabilities
- Simpler, faster, more maintainable
- Can add sink toggling later if needed (YAGNI principle)

---

## Appendix: Event Flow Timeline

### Original Design Timeline

```
[15:23:45.123] Core logs "Attack executed"
      ↓
[15:23:45.124] Check level (Information >= Debug?) ✅
      ↓
[15:23:45.125] ConditionalConsoleSink.Emit()
      ├─ Check toggles["Console"].IsEnabled → true ✅
      └─ Console.WriteLine("[INFO] Attack executed")
      ↓
[15:23:45.126] ConditionalFileSink.Emit()
      ├─ Check toggles["File"].IsEnabled → true ✅
      └─ File.AppendText("2025-09-30 [INFO] Attack executed")
      ↓
[15:23:45.127] ConditionalGodotSink.Emit()
      ├─ Check toggles["Godot"].IsEnabled → true ✅
      └─ RichTextLabel.CallDeferred(AppendText, ...)

Total: 1 level check + 3 sink checks = 4 conditionals
```

### Proposed Design Timeline

```
[15:23:45.123] Core logs "Attack executed"
      ↓
[15:23:45.124] Check level (Information >= Debug?) ✅
      ↓
[15:23:45.125] Filter.ByIncludingOnly
      ├─ Extract SourceContext → "...Commands.Combat.ExecuteAttackCommandHandler"
      ├─ Parse category → "Combat"
      └─ Check enabledCategories.Contains("Combat") → true ✅
      ↓
[15:23:45.126] Pass to all sinks in parallel
      ├─ ConsoleSink.Emit() → Console.WriteLine(...)
      ├─ FileSink.Emit() → File.AppendText(...)
      └─ GodotSink.Emit() → RichTextLabel.CallDeferred(...)

Total: 1 level check + 1 category check = 2 conditionals
```

---

**End of Document**