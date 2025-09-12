# ADR-007: Unified Logger Architecture

**Status**: Accepted  
**Date**: 2025-09-12  
**Author**: Tech Lead  
**Deciders**: Tech Lead, Dev Engineer  

## Context

Our logging system has evolved into an unmaintainable mess with 4+ parallel implementations:

1. **Serilog ILogger** - Used by 18+ Application layer files
2. **Microsoft.Extensions.Logging ILogger<T>** - Used by 8+ Infrastructure files  
3. **ICategoryLogger** - Two broken implementations (CategoryFilteredLogger, GodotCategoryLogger)
4. **Direct GD.Print()** - Scattered throughout Godot presentation layer

This violates Single Source of Truth (SSOT) and creates several problems:
- Inconsistent log formatting across layers
- Category filtering only works for some loggers
- Debug window controls don't affect all logging
- Log level changes don't propagate to all systems
- Difficult to add new features like file output

### Requirements

1. **Single unified logger** across all architectural layers
2. **Category-based filtering** controlled by DebugConfig (12+ categories)
3. **Multiple simultaneous destinations** (console + file)
4. **Rich console formatting** in Godot (colored output)
5. **Clean Architecture compliance** (no Godot types in Core)
6. **Runtime reconfiguration** via Debug Window
7. **Easy log sharing** (overwrite mode for current session)

## Decision

Implement a **Unified Logger Architecture** with pluggable outputs using the Composite pattern. This provides a single logging interface with multiple configurable destinations while respecting Clean Architecture boundaries.

### Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    Application Layer                     │
│                 Uses: ICategoryLogger                    │
└─────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────┐
│                 Core.Infrastructure                      │
│                                                          │
│  UnifiedLogger : ICategoryLogger                        │
│       │                                                  │
│       ├── IDebugConfiguration (category/level filtering)│
│       │                                                  │
│       └── ILogOutput (abstraction)                      │
│           │                                              │
│           └── CompositeLogOutput : ILogOutput           │
│               ├── FileLogOutput (plain text)            │
│               └── (registered outputs...)               │
└─────────────────────────────────────────────────────────┘
                            │
┌─────────────────────────────────────────────────────────┐
│                     Godot Layer                          │
│                                                          │
│  GodotConsoleOutput : ILogOutput                        │
│  (Rich formatting with GD.PrintRich)                    │
└─────────────────────────────────────────────────────────┘
```

### Core Components

#### 1. ILogOutput Interface (Core.Infrastructure)
```csharp
public interface ILogOutput : IDisposable
{
    void WriteLine(LogLevel level, string category, string message, string formattedMessage);
    void Flush();
    bool IsEnabled { get; set; }
}
```

#### 2. CompositeLogOutput (Core.Infrastructure)
```csharp
public sealed class CompositeLogOutput : ILogOutput
{
    private readonly List<ILogOutput> _outputs = new();
    
    public void AddOutput(ILogOutput output) => _outputs.Add(output);
    public void RemoveOutput(ILogOutput output) => _outputs.Remove(output);
    
    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        foreach (var output in _outputs.Where(o => o.IsEnabled))
        {
            output.WriteLine(level, category, message, formattedMessage);
        }
    }
    
    public void Flush()
    {
        foreach (var output in _outputs)
            output.Flush();
    }
    
    public void Dispose()
    {
        foreach (var output in _outputs)
            output.Dispose();
        _outputs.Clear();
    }
    
    public bool IsEnabled { get; set; } = true;
}
```

#### 3. UnifiedLogger (Core.Infrastructure)
```csharp
public sealed class UnifiedLogger : ICategoryLogger
{
    private readonly ILogOutput _output;
    private readonly IDebugConfiguration _config;
    private readonly string _timestampFormat = "HH:mm:ss.fff";
    
    public UnifiedLogger(ILogOutput output, IDebugConfiguration config)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }
    
    public void Log(LogLevel level, LogCategory category, string template, params object[] args)
    {
        // Apply both level and category filtering
        if (!_config.ShouldLog(level, category))
            return;
            
        try
        {
            var message = FormatTemplate(template, args);
            var timestamp = DateTime.Now.ToString(_timestampFormat);
            var formattedMessage = $"[{timestamp}] [{level:3}] [{category}] {message}";
            
            _output.WriteLine(level, category.ToString(), message, formattedMessage);
        }
        catch (FormatException ex)
        {
            // Fallback for template errors
            var fallback = $"[FORMAT ERROR] {template}";
            _output.WriteLine(LogLevel.Warning, category.ToString(), fallback, fallback);
        }
    }
    
    private string FormatTemplate(string template, object[] args)
    {
        if (args == null || args.Length == 0)
            return template;
            
        // Simple positional template formatting
        // Converts "User {0} logged in at {1}" with args ["Alice", "10:30"]
        // to "User Alice logged in at 10:30"
        return string.Format(template, args);
    }
    
    public void Flush() => _output.Flush();
}
```

#### 4. FileLogOutput (Core.Infrastructure)
```csharp
public sealed class FileLogOutput : ILogOutput
{
    private readonly string _logDirectory;
    private StreamWriter? _writer;
    private readonly object _lock = new();
    
    public FileLogOutput(string logDirectory = "logs")
    {
        _logDirectory = logDirectory;
        InitializeLogFile();
    }
    
    private void InitializeLogFile()
    {
        Directory.CreateDirectory(_logDirectory);
        
        // Use session-based naming for easy identification
        var fileName = $"darklands-session-{DateTime.Now:yyyyMMdd-HHmmss}.log";
        var filePath = Path.Combine(_logDirectory, fileName);
        
        // Also create a symlink/copy as "darklands-current.log" for easy access
        var currentPath = Path.Combine(_logDirectory, "darklands-current.log");
        
        _writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8)
        {
            AutoFlush = true // Ensure logs are written immediately
        };
        
        // Write session header
        _writer.WriteLine($"═══════════════════════════════════════════════════════");
        _writer.WriteLine($" DARKLANDS SESSION LOG");
        _writer.WriteLine($" Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine($" Version: {GetGameVersion()}");
        _writer.WriteLine($"═══════════════════════════════════════════════════════");
        _writer.WriteLine();
        
        // Update current log symlink/copy
        UpdateCurrentLogLink(filePath, currentPath);
    }
    
    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        lock (_lock)
        {
            _writer?.WriteLine(formattedMessage);
        }
    }
    
    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
        }
    }
    
    public void Dispose()
    {
        lock (_lock)
        {
            if (_writer != null)
            {
                _writer.WriteLine();
                _writer.WriteLine($"═══════════════════════════════════════════════════════");
                _writer.WriteLine($" SESSION ENDED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"═══════════════════════════════════════════════════════");
                _writer.Dispose();
                _writer = null;
            }
        }
    }
    
    public bool IsEnabled { get; set; } = true;
    
    private void UpdateCurrentLogLink(string source, string target)
    {
        try
        {
            // Windows: Copy file; Linux/Mac: Create symlink
            if (File.Exists(target))
                File.Delete(target);
            File.Copy(source, target);
        }
        catch
        {
            // Non-critical if this fails
        }
    }
    
    private string GetGameVersion() => "0.1.0-alpha"; // TODO: Read from assembly
}
```

#### 5. GodotConsoleOutput (Godot Layer)
```csharp
public sealed class GodotConsoleOutput : ILogOutput
{
    private readonly Dictionary<LogLevel, string> _levelColors = new()
    {
        { LogLevel.Debug, "#808080" },     // Gray
        { LogLevel.Information, "#FFFFFF" }, // White
        { LogLevel.Warning, "#FFD700" },    // Gold
        { LogLevel.Error, "#FF4444" }       // Red
    };
    
    private readonly Dictionary<string, string> _categoryColors = new()
    {
        { "Combat", "#FF6B6B" },      // Light Red
        { "Movement", "#4ECDC4" },    // Teal
        { "Vision", "#95E1D3" },      // Mint
        { "AI", "#F38181" },          // Salmon
        { "System", "#AA96DA" },      // Lavender
        { "Network", "#8785A2" },     // Purple Gray
        { "Performance", "#FFD93D" }, // Yellow
        { "Developer", "#6BCB77" }    // Green
    };
    
    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        var levelColor = _levelColors.GetValueOrDefault(level, "#FFFFFF");
        var categoryColor = _categoryColors.GetValueOrDefault(category, "#CCCCCC");
        
        // Rich formatted output for Godot console
        var richText = $"[color={levelColor}]" +
                      $"[{DateTime.Now:HH:mm:ss}] " +
                      $"[{level.ToString().Substring(0, 3).ToUpper()}][/color] " +
                      $"[color={categoryColor}][{category}][/color] " +
                      $"[color={levelColor}]{message}[/color]";
        
        GD.PrintRich(richText);
    }
    
    public void Flush() { } // No-op for console
    public void Dispose() { } // No-op for console
    public bool IsEnabled { get; set; } = true;
}
```

#### 6. TestConsoleOutput (Core.Infrastructure)
```csharp
public sealed class TestConsoleOutput : ILogOutput
{
    private readonly StringBuilder _buffer = new();
    
    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        // Plain text for test environment
        Console.WriteLine(formattedMessage);
        _buffer.AppendLine(formattedMessage);
    }
    
    public string GetBuffer() => _buffer.ToString();
    public void ClearBuffer() => _buffer.Clear();
    
    public void Flush() { }
    public void Dispose() => _buffer.Clear();
    public bool IsEnabled { get; set; } = true;
}
```

### DI Registration

```csharp
// In GameStrapper.cs
public static void ConfigureLogging(this IServiceCollection services, bool isTestEnvironment = false)
{
    // Register composite output as singleton
    services.AddSingleton<ILogOutput>(sp =>
    {
        var composite = new CompositeLogOutput();
        
        if (isTestEnvironment)
        {
            // Test environment: simple console output
            composite.AddOutput(new TestConsoleOutput());
        }
        else
        {
            // Production: Godot console + file
            composite.AddOutput(new GodotConsoleOutput());
            composite.AddOutput(new FileLogOutput("logs"));
        }
        
        return composite;
    });
    
    // Register unified logger
    services.AddSingleton<ICategoryLogger, UnifiedLogger>();
    
    // Register ILogger<T> adapter for compatibility
    services.AddSingleton(typeof(ILogger<>), typeof(CategoryLoggerAdapter<>));
}
```

## Consequences

### Positive

1. ✅ **Single Source of Truth**: One logger implementation for entire application
2. ✅ **Clean Architecture Preserved**: No Godot types in Core layer
3. ✅ **Runtime Reconfiguration**: Changes in Debug Window apply immediately
4. ✅ **Multiple Destinations**: Simultaneous console and file output
5. ✅ **Rich Formatting**: Colored output in Godot console
6. ✅ **Category Filtering**: Fine-grained control via DebugConfig
7. ✅ **Easy Sharing**: Current log always available at known location
8. ✅ **Testable**: Can mock ILogOutput for unit tests
9. ✅ **Extensible**: Easy to add new output destinations
10. ✅ **Performance**: Minimal overhead with direct calls

### Negative

1. ❌ **Migration Effort**: Must update all existing logging calls
2. ❌ **File I/O**: Disk writes could impact performance (mitigated by AutoFlush)
3. ❌ **Memory Usage**: BufferedStream for file output
4. ❌ **Complexity**: More components than single logger

### Neutral

- ➖ File logs use session-based naming (easier to identify)
- ➖ Template formatting is positional, not named (simpler but less flexible)
- ➖ No structured logging to JSON (not needed for our scale)

## Alternatives Considered

### 1. Keep Status Quo (4+ Parallel Loggers)
- ✅ No migration effort
- ❌ Increasing technical debt
- ❌ Inconsistent behavior
- **Rejected**: Unsustainable complexity

### 2. Serilog Everywhere
- ✅ Powerful and mature
- ❌ Heavy dependency for Godot game
- ❌ Complex configuration
- **Rejected**: Over-engineered for our needs

### 3. Event Bus Pattern (like ADR-010)
- ✅ Decoupled architecture
- ❌ Over-complex for logging
- ❌ Performance overhead
- **Rejected**: Wrong pattern for this problem

### 4. Direct GD.Print Only
- ✅ Simplest solution
- ❌ No file output
- ❌ Not testable
- ❌ No category filtering
- **Rejected**: Doesn't meet requirements

## Implementation Plan

### Phase 1: Core Infrastructure (2 hours)
- [x] Create ILogOutput interface
- [x] Implement CompositeLogOutput
- [x] Implement FileLogOutput
- [x] Implement TestConsoleOutput
- [x] Create UnifiedLogger

### Phase 2: Godot Integration (1 hour)
- [ ] Implement GodotConsoleOutput
- [ ] Update GameStrapper registration
- [ ] Connect to DebugConfig

### Phase 3: Migration (2 hours)
- [ ] Replace Serilog usage (18 files)
- [ ] Replace ILogger<T> usage (8 files)
- [ ] Replace GD.Print calls (7 files)
- [ ] Update tests

### Phase 4: Cleanup (0.5 hours)
- [ ] Delete old implementations
- [ ] Remove Serilog packages
- [ ] Update documentation

### Phase 5: Verification (0.5 hours)
- [ ] Test Debug Window integration
- [ ] Verify file output
- [ ] Check performance impact
- [ ] Validate category filtering

**Total Estimate**: 6 hours

## Migration Strategy

```csharp
// BEFORE: Multiple logger types
private readonly ILogger<MyClass> _logger;           // Microsoft
private readonly ILogger _serilogLogger;             // Serilog  
private readonly ICategoryLogger _categoryLogger;    // Custom
GD.Print("Something happened");                      // Direct

// AFTER: Single unified logger
private readonly ICategoryLogger _logger;

// Usage is consistent everywhere:
_logger.Log(LogLevel.Information, LogCategory.System, "Operation {0} completed", operationName);
```

## Success Metrics

1. **All tests pass** with new logger
2. **Single logger type** throughout codebase
3. **Debug Window controls** affect all logging
4. **File output** works for sharing
5. **Performance impact** <1ms per log
6. **Zero format errors** in output

## Related Documents

- TD_038: Complete Logger System Rewrite
- BR_005: Debug Log Level Filtering Issue  
- ADR-006: Selective Abstraction Strategy
- DebugConfig.cs: Runtime configuration
- DebugWindow.cs: UI controls

## Example Usage

```csharp
// In any class needing logging
public sealed class CombatService
{
    private readonly ICategoryLogger _logger;
    
    public CombatService(ICategoryLogger logger)
    {
        _logger = logger;
    }
    
    public void ExecuteAttack(Actor attacker, Actor target)
    {
        _logger.Log(LogLevel.Debug, LogCategory.Combat, 
            "Attack initiated: {0} -> {1}", attacker.Name, target.Name);
        
        var damage = CalculateDamage(attacker, target);
        
        _logger.Log(LogLevel.Information, LogCategory.Combat,
            "Damage dealt: {0} points", damage);
    }
}
```

## Appendix: Design Decisions

### Why Composite Pattern?
Allows runtime addition/removal of outputs without changing logger code. New destinations (network, UI overlay) can be added without modification.

### Why Session-Based File Names?
Each game session gets a unique log file with timestamp. The "current.log" symlink/copy provides consistent access point for reading.

### Why Not Structured Logging?
For a tactical game of Battle Brothers scale, plain text logs are sufficient. Structured logging adds complexity without proportional value.

### Why Category Enum Instead of Strings?
Type safety and compile-time checking. With 12+ categories, typos in strings would cause silent failures.

### Why Positional Templates?
Simpler than named templates, sufficient for our needs, and performs better than regex-based parsing.

## Future Considerations: Game Analytics vs Debug Logging

This ADR explicitly separates **debug logging** (for development) from **game analytics** (for features and balancing). They serve different purposes and should remain separate systems:

### Debug Logging (This ADR)
- **Purpose**: Help developers find bugs and understand program flow
- **Audience**: Developers only
- **Format**: Human-readable text with categories
- **Storage**: Console output and rotating log files
- **Lifetime**: Current session only
- **Performance**: Must be minimal overhead

### Future Game Systems (NOT This ADR)

When these features are needed, implement them as **separate systems**:

#### 1. Life-Review/Obituary System
```csharp
public interface IGameHistorian
{
    void RecordDeath(Actor actor, CombatContext context);
    void RecordBattle(BattleResult result);
    void RecordMilestone(string achievement);
    PlayerHistory GetPlayerHistory();
}
```
- **Purpose**: Create narrative for player's journey
- **Storage**: SQLite in save game or JSON
- **Example**: Battle Brothers obituary, Dwarf Fortress legends

#### 2. Economy Balancing System
```csharp
public interface IEconomyTracker
{
    void RecordTransaction(Transaction transaction);
    void RecordLoot(LootResult loot);
    EconomyMetrics GetBalanceMetrics();
}
```
- **Purpose**: Balance item prices and gold flow
- **Storage**: Aggregated analytics database
- **Example**: Average gold per battle, item purchase patterns

#### 3. Player Analytics System
```csharp
public interface IPlayerAnalytics
{
    void TrackPlayerAction(PlayerAction action);
    void TrackProgression(ProgressionEvent progress);
    PlayerMetrics GetPlayerMetrics();
}
```
- **Purpose**: Understand player behavior
- **Storage**: Structured events for analysis
- **Example**: Death heatmaps, difficulty spikes

### Architecture Separation

```
Game Code
    ├── Debug Logger (ICategoryLogger) → Console/File
    ├── Game Historian (IGameHistorian) → SaveGame.db
    ├── Economy Tracker (IEconomyTracker) → Analytics.db
    └── Player Analytics (IPlayerAnalytics) → Events.json
```

### Key Principles

1. **Don't Pollute Debug Logs**: Debug logs should remain text for developers
2. **Don't Over-Engineer**: Build analytics when features require them
3. **Separate Concerns**: Each system has its own interface and storage
4. **Coexistence**: Systems can instrument the same code without conflict

### Example: Same Event, Different Systems

```csharp
public void OnActorDeath(Actor actor, DamageSource source)
{
    // Debug logging (this ADR)
    _logger.Log(LogLevel.Info, LogCategory.Combat, 
        "Actor {0} died from {1}", actor.Name, source);
    
    // Game historian (future feature)
    _historian?.RecordDeath(actor, source);
    
    // Economy tracker (future balancing)
    _economy?.RecordGoldLost(actor.CarriedGold);
    
    // Each system captures what it needs
}
```

This separation ensures our debug logger remains simple and focused while allowing future growth for game features that need structured data.