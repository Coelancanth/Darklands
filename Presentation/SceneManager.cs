using System;
using Godot;
using Microsoft.Extensions.Logging;
using Darklands.Core.Domain.Services;
using Darklands.Presentation.Infrastructure;

namespace Darklands.Presentation;

/// <summary>
/// Scene management service that handles scene transitions with automatic DI scope creation.
///
/// Key Features (TD_052 Phase 2):
/// - Automatic scope creation for scene root nodes
/// - Proper scope disposal during scene transitions
/// - Support for overlay scenes with nested scopes
/// - Integration with ServiceLocator autoload pattern
/// - Graceful fallback if scope creation fails
///
/// Usage:
/// ```csharp
/// // Load a new scene with automatic scope
/// var sceneRoot = _sceneManager.LoadScene("res://scenes/Combat.tscn");
///
/// // Load overlay with nested scope
/// var overlay = _sceneManager.LoadOverlay("res://ui/Inventory.tscn", parentNode);
/// ```
///
/// Scope Management:
/// - Scene root nodes get their own scopes
/// - Child nodes automatically inherit parent scopes
/// - Overlay scenes create child scopes for isolation
/// - All scopes are automatically disposed on scene exit
/// </summary>
public sealed class SceneManager
{
    private readonly ILogger<SceneManager>? _logger;

    /// <summary>
    /// Creates a new SceneManager with optional logging.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic information</param>
    public SceneManager(ILogger<SceneManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads a scene with automatic scope creation for the root node.
    ///
    /// Process:
    /// 1. Load the PackedScene from the specified path
    /// 2. Instantiate the scene
    /// 3. Create a DI scope for the root node
    /// 4. Return the scene root for further setup
    ///
    /// The scope will be automatically disposed when the node exits the tree.
    /// </summary>
    /// <param name="scenePath">Path to the .tscn scene file</param>
    /// <returns>The scene root node with scope created, or null if loading failed</returns>
    public Node? LoadScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            _logger?.LogError("LoadScene called with null or empty scene path");
            return null;
        }

        try
        {
            _logger?.LogDebug("Loading scene: {ScenePath}", scenePath);

            // Load the PackedScene resource
            var packedScene = GD.Load<PackedScene>(scenePath);
            if (packedScene == null)
            {
                _logger?.LogError("Failed to load PackedScene from path: {ScenePath}", scenePath);
                return null;
            }

            // Instantiate the scene
            var sceneInstance = packedScene.Instantiate();
            if (sceneInstance == null)
            {
                _logger?.LogError("Failed to instantiate scene from: {ScenePath}", scenePath);
                return null;
            }

            _logger?.LogDebug("Scene instantiated successfully: {NodeName} from {ScenePath}",
                             sceneInstance.Name, scenePath);

            // Create scope for the new scene root
            var scopeCreated = sceneInstance.CreateScope();
            if (scopeCreated)
            {
                _logger?.LogInformation("Loaded scene {ScenePath} with DI scope for node {NodeName}",
                                       scenePath, sceneInstance.Name);
            }
            else
            {
                _logger?.LogWarning("Loaded scene {ScenePath} but scope creation failed for node {NodeName}. " +
                                   "Node will fall back to GameStrapper pattern.",
                                   scenePath, sceneInstance.Name);
            }

            return sceneInstance;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading scene {ScenePath}", scenePath);
            return null;
        }
    }

    /// <summary>
    /// Loads an overlay scene with nested scope creation.
    ///
    /// Overlays are scenes that appear on top of existing scenes (like modals, dialogs, HUD elements).
    /// They create child scopes that inherit from the parent node's scope.
    ///
    /// Process:
    /// 1. Load and instantiate the overlay scene
    /// 2. Add it as a child to the specified parent
    /// 3. Create a child scope for isolation
    /// 4. Return the overlay root for further configuration
    /// </summary>
    /// <param name="overlayPath">Path to the overlay .tscn scene file</param>
    /// <param name="parent">Parent node to add the overlay to</param>
    /// <returns>The overlay root node with scope created, or null if loading failed</returns>
    public Node? LoadOverlay(string overlayPath, Node parent)
    {
        if (string.IsNullOrEmpty(overlayPath))
        {
            _logger?.LogError("LoadOverlay called with null or empty overlay path");
            return null;
        }

        if (parent == null)
        {
            _logger?.LogError("LoadOverlay called with null parent node");
            return null;
        }

        try
        {
            _logger?.LogDebug("Loading overlay: {OverlayPath} for parent {ParentName}",
                             overlayPath, parent.Name);

            // Load the overlay using the same logic as LoadScene
            var overlayInstance = LoadScene(overlayPath);
            if (overlayInstance == null)
            {
                _logger?.LogError("Failed to load overlay scene: {OverlayPath}", overlayPath);
                return null;
            }

            // Add as child to the specified parent
            parent.AddChild(overlayInstance);

            _logger?.LogInformation("Loaded overlay {OverlayPath} as child of {ParentName}",
                                   overlayPath, parent.Name);

            return overlayInstance;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading overlay {OverlayPath} for parent {ParentName}",
                             overlayPath, parent.Name);
            return null;
        }
    }

    /// <summary>
    /// Replaces the current scene with a new scene, ensuring proper cleanup.
    ///
    /// This method handles the complete scene transition:
    /// 1. Load the new scene with scope
    /// 2. Get the current scene tree
    /// 3. Replace the current scene (which will dispose old scopes)
    /// 4. Set the new scene as current
    ///
    /// The old scene's scopes are automatically disposed via TreeExiting signals.
    /// </summary>
    /// <param name="newScenePath">Path to the new scene to load</param>
    /// <returns>True if scene transition succeeded, false otherwise</returns>
    public bool ChangeScene(string newScenePath)
    {
        if (string.IsNullOrEmpty(newScenePath))
        {
            _logger?.LogError("ChangeScene called with null or empty scene path");
            return false;
        }

        try
        {
            _logger?.LogInformation("Changing scene to: {NewScenePath}", newScenePath);

            // Load the new scene with scope
            var newSceneRoot = LoadScene(newScenePath);
            if (newSceneRoot == null)
            {
                _logger?.LogError("Failed to load new scene for transition: {NewScenePath}", newScenePath);
                return false;
            }

            // Get the scene tree and change to the new scene
            // This will automatically dispose the old scene's scopes via TreeExiting
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree == null)
            {
                _logger?.LogError("Could not get SceneTree for scene transition");
                newSceneRoot.QueueFree(); // Clean up since we can't use it
                return false;
            }

            // Get current scene and replace it
            var currentScene = sceneTree.CurrentScene;
            if (currentScene != null)
            {
                currentScene.GetParent()?.RemoveChild(currentScene);
                currentScene.QueueFree();
            }

            sceneTree.Root.AddChild(newSceneRoot);
            sceneTree.CurrentScene = newSceneRoot;

            _logger?.LogInformation("Scene transition completed successfully to: {NewScenePath}", newScenePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during scene transition to {NewScenePath}", newScenePath);
            return false;
        }
    }

    /// <summary>
    /// Gets diagnostic information about scene management and scopes.
    /// Useful for debugging scene transition and scope lifecycle issues.
    /// </summary>
    /// <returns>Diagnostic information string</returns>
    public string GetDiagnosticInfo()
    {
        try
        {
            var sceneTree = Engine.GetMainLoop() as SceneTree;
            if (sceneTree?.CurrentScene == null)
            {
                return "SceneManager: No current scene";
            }

            var currentScene = sceneTree.CurrentScene;
            var scopeDiagnostics = currentScene.GetScopeDiagnostics();

            return $"SceneManager: Current scene '{currentScene.Name}' at {currentScene.GetPath()}. " +
                   $"Scope info: {scopeDiagnostics}";
        }
        catch (Exception ex)
        {
            return $"SceneManager diagnostic error: {ex.Message}";
        }
    }
}