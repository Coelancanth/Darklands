namespace Darklands.Core.Infrastructure.Logging;

/// <summary>
/// Well-defined logging categories for structured logging throughout the Darklands application.
/// These categories help organize log messages by functional area and make debugging more efficient.
/// 
/// Usage: logger.LogInformation("Processing combat action", LogCategory.Combat);
/// </summary>
public static class LogCategory
{
    /// <summary>
    /// Core system startup, shutdown, and configuration
    /// </summary>
    public const string System = "System";
    
    /// <summary>
    /// Dependency injection container operations
    /// </summary>
    public const string DependencyInjection = "DI";
    
    /// <summary>
    /// MediatR command execution and pipeline behaviors
    /// </summary>
    public const string Commands = "Commands";
    
    /// <summary>
    /// MediatR query execution and data retrieval
    /// </summary>
    public const string Queries = "Queries";
    
    /// <summary>
    /// MediatR notification handling and event processing
    /// </summary>
    public const string Events = "Events";
    
    /// <summary>
    /// Combat system operations (time units, actions, damage)
    /// </summary>
    public const string Combat = "Combat";
    
    /// <summary>
    /// Game state management and persistence
    /// </summary>
    public const string GameState = "GameState";
    
    /// <summary>
    /// User interface and presentation layer operations
    /// </summary>
    public const string Presentation = "Presentation";
    
    /// <summary>
    /// Godot engine integration and node operations
    /// </summary>
    public const string Godot = "Godot";
    
    /// <summary>
    /// Performance measurements and profiling
    /// </summary>
    public const string Performance = "Performance";
    
    /// <summary>
    /// Data validation and business rule enforcement
    /// </summary>
    public const string Validation = "Validation";
    
    /// <summary>
    /// External service integrations and API calls
    /// </summary>
    public const string Integration = "Integration";
    
    /// <summary>
    /// Modding system and plugin loading
    /// </summary>
    public const string Modding = "Modding";
    
    /// <summary>
    /// File I/O operations and asset loading
    /// </summary>
    public const string FileSystem = "FileSystem";
    
    /// <summary>
    /// Network operations and multiplayer functionality
    /// </summary>
    public const string Network = "Network";
    
    /// <summary>
    /// Security-related operations and validation
    /// </summary>
    public const string Security = "Security";
    
    /// <summary>
    /// Test execution and debugging support
    /// </summary>
    public const string Testing = "Testing";
}

