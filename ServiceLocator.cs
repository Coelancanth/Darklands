using System;
using Godot;
using Microsoft.Extensions.Logging;
using Darklands.Core.Domain.Services;

/// <summary>
/// Godot autoload node that provides global access to the IScopeManager.
///
/// Design Rationale:
/// - Godot autoloads are the appropriate way to provide global services
/// - Avoids static dependencies that prevent proper lifecycle management
/// - Provides graceful fallback if autoload registration fails
/// - Thread-safe initialization with proper error handling
///
/// MANDATORY IMPROVEMENT 3: Failure Handling
/// - Graceful degradation if autoload is not properly configured
/// - Fallback to GameStrapper for service resolution
/// - Clear error logging for diagnostic purposes
/// - Never throws exceptions that would crash node initialization
///
/// Usage:
/// 1. Add to Godot project settings as autoload: "/root/ServiceLocator"
/// 2. Initialize from GameStrapper after DI container setup
/// 3. Access via extension methods: node.GetService<T>()
///
/// Autoload Path: /root/ServiceLocator
/// Scene: This file should be added to Godot project settings autoloads
/// </summary>
public partial class ServiceLocator : Node
{
    private static readonly object _initializationLock = new();
    private static volatile bool _isInitialized;

    /// <summary>
    /// The scope manager instance provided by GameStrapper.
    /// Null until Initialize() is called successfully.
    /// </summary>
    public IScopeManager? ScopeManager { get; private set; }

    /// <summary>
    /// Logger for diagnostic information about autoload failures.
    /// </summary>
    private ILogger<ServiceLocator>? _logger;

    /// <summary>
    /// Indicates whether the ServiceLocator was successfully initialized.
    /// Used by extension methods to determine fallback strategy.
    /// </summary>
    public bool IsInitialized => _isInitialized && ScopeManager != null;

    /// <summary>
    /// Called when the autoload node is added to the scene tree.
    /// Logs readiness but doesn't initialize services (GameStrapper does that).
    /// </summary>
    public override void _Ready()
    {
        Name = "ServiceLocator";
        GD.Print($"[ServiceLocator] Autoload ready at {GetPath()}");
    }

    /// <summary>
    /// Initializes the ServiceLocator with the scope manager from GameStrapper.
    ///
    /// MANDATORY IMPROVEMENT 3: Thread-safe initialization with error handling
    /// - Multiple calls are safe (idempotent)
    /// - Provides clear error messages for diagnostic purposes
    /// - Never throws exceptions that would prevent game startup
    ///
    /// Called by GameStrapper after DI container is built.
    /// </summary>
    /// <param name="scopeManager">The scope manager instance to use</param>
    /// <param name="logger">Optional logger for diagnostic messages</param>
    /// <returns>True if initialization succeeded, false otherwise</returns>
    public bool Initialize(IScopeManager scopeManager, ILogger<ServiceLocator>? logger = null)
    {
        if (scopeManager == null)
        {
            GD.PrintErr("[ServiceLocator] Cannot initialize with null scope manager");
            return false;
        }

        lock (_initializationLock)
        {
            try
            {
                if (_isInitialized)
                {
                    logger?.LogDebug("ServiceLocator already initialized, skipping");
                    return true;
                }

                ScopeManager = scopeManager;
                _logger = logger;
                _isInitialized = true;

                _logger?.LogInformation("ServiceLocator initialized successfully at autoload path: {Path}", GetPath());
                GD.Print($"[ServiceLocator] Initialized successfully with scope manager");

                return true;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ServiceLocator] Initialization failed: {ex.Message}");
                _logger?.LogError(ex, "ServiceLocator initialization failed");

                // Reset state on failure
                ScopeManager = null;
                _isInitialized = false;
                return false;
            }
        }
    }

    /// <summary>
    /// Gets diagnostic information about the ServiceLocator state.
    /// Used for debugging autoload configuration issues.
    /// </summary>
    /// <returns>Diagnostic information string</returns>
    public string GetDiagnosticInfo()
    {
        var status = IsInitialized ? "Initialized" : "Not Initialized";
        var path = IsInsideTree() ? GetPath().ToString() : "Not in tree";
        var scopeInfo = ScopeManager != null ? "Available" : "Null";

        return $"ServiceLocator Status: {status}, Path: {path}, ScopeManager: {scopeInfo}";
    }

    /// <summary>
    /// Called when the node is removed from the scene tree.
    /// Cleans up resources and logs disposal.
    /// </summary>
    public override void _ExitTree()
    {
        lock (_initializationLock)
        {
            try
            {
                if (_isInitialized)
                {
                    _logger?.LogInformation("ServiceLocator shutting down");
                    ScopeManager?.Dispose();
                    ScopeManager = null;
                    _isInitialized = false;
                }

                GD.Print("[ServiceLocator] Autoload disposed");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[ServiceLocator] Error during disposal: {ex.Message}");
                _logger?.LogError(ex, "Error during ServiceLocator disposal");
            }
        }
    }

    /// <summary>
    /// Static helper method for getting the ServiceLocator instance.
    /// Handles cases where autoload might not be properly configured.
    ///
    /// MANDATORY IMPROVEMENT 3: Safe autoload access with fallback
    /// - Returns null if autoload is not configured
    /// - Logs diagnostic information for troubleshooting
    /// - Never throws exceptions that would crash calling code
    /// </summary>
    /// <param name="fromNode">The node requesting the ServiceLocator (for context)</param>
    /// <returns>ServiceLocator instance or null if not available</returns>
    public static ServiceLocator? GetInstance(Node fromNode)
    {
        try
        {
            // Try to get the autoload from the scene tree
            var serviceLocator = fromNode.GetNodeOrNull<ServiceLocator>("/root/ServiceLocator");

            if (serviceLocator == null)
            {
                GD.PrintErr("[ServiceLocator] Autoload not found at /root/ServiceLocator. Check project settings.");
                return null;
            }

            if (!serviceLocator.IsInitialized)
            {
                GD.PrintErr("[ServiceLocator] Found but not initialized. GameStrapper.Initialize() may not have been called.");
                return null;
            }

            return serviceLocator;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[ServiceLocator] Error accessing autoload: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Debug method for checking autoload configuration from the editor.
    /// Can be called via Remote Inspector or debug console.
    /// </summary>
    [Signal]
    public delegate void DiagnosticsRequestedEventHandler(string diagnosticInfo);

    /// <summary>
    /// Emits diagnostic information for debugging purposes.
    /// </summary>
    public void EmitDiagnostics()
    {
        var diagnostics = GetDiagnosticInfo();
        EmitSignal(SignalName.DiagnosticsRequested, diagnostics);
        GD.Print($"[ServiceLocator] Diagnostics: {diagnostics}");
    }
}