using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Darklands.Views
{
    /// <summary>
    /// Concrete Godot implementation of the actor view interface.
    /// Manages the visual representation of actors (players, enemies, etc.) on the tactical grid.
    /// Uses simple ColorRect nodes for Phase 4 MVP - no sprites or complex visuals yet.
    /// </summary>
    public partial class ActorView : Node2D, IActorView
    {
        private ActorPresenter? _presenter;
        private ILogger? _logger;
        private readonly Dictionary<Darklands.Core.Domain.Grid.ActorId, ColorRect> _actorNodes = new();
        private const int TileSize = 64;
        private const float MoveDuration = 0.3f; // Seconds for movement animation

        // Actor colors for different types
        private readonly Color PlayerColor = new(0.1f, 0.46f, 0.82f, 1.0f); // Blue #1976D2
        private readonly Color EnemyColor = new(0.59f, 0.36f, 0.20f, 1.0f);  // Brown #964D33
        private readonly Color NeutralColor = new(0.61f, 0.61f, 0.61f, 1.0f); // Gray #9E9E9E
        private readonly Color InteractiveColor = new(1.0f, 0.59f, 0.0f, 1.0f); // Orange #FF9800

        // Temporary storage for deferred method parameters
        private ColorRect? _pendingActorNode;
        private Darklands.Core.Domain.Grid.ActorId _pendingActorId;
        private Vector2 _pendingEndPosition;
        private Darklands.Core.Domain.Grid.Position _pendingFromPosition;
        private Darklands.Core.Domain.Grid.Position _pendingToPosition;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Sets up the actor display system.
        /// </summary>
        public override void _Ready()
        {
            try
            {
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "ActorView._Ready error");
            }
        }

        /// <summary>
        /// Sets the presenter that controls this view.
        /// Called during initialization to establish the MVP connection.
        /// </summary>
        public void SetPresenter(ActorPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        /// <summary>
        /// Sets the logger for this view.
        /// Called during initialization to enable proper logging.
        /// </summary>
        public void SetLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates and displays a new actor at the specified position.
        /// </summary>
        public async Task DisplayActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Grid.Position position, ActorType actorType)
        {
            try
            {
                // Remove existing actor node if it exists (synchronously to avoid race conditions)
                if (_actorNodes.TryGetValue(actorId, out var existingNode))
                {
                    existingNode?.QueueFree();
                    _actorNodes.Remove(actorId);
                    _logger?.Debug("Removed existing actor node for {ActorId}", actorId);
                }

                // Create new ColorRect node for the actor
                var actorNode = new ColorRect
                {
                    Size = new Vector2(TileSize, TileSize),
                    Color = GetActorColor(actorType),
                    Position = new Vector2(position.X * TileSize, position.Y * TileSize)
                };

                // Store for deferred call
                _pendingActorNode = actorNode;
                _pendingActorId = actorId;
                CallDeferred("AddActorNodeDeferred");

                _logger?.Information("Actor {ActorId} created at ({X},{Y})", actorId, position.X, position.Y);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error displaying actor {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Helper method to add actor node on main thread.
        /// </summary>
        private void AddActorNodeDeferred()
        {
            if (_pendingActorNode != null)
            {
                AddChild(_pendingActorNode);
                _actorNodes[_pendingActorId] = _pendingActorNode;
                _pendingActorNode = null;
            }
        }


        /// <summary>
        /// Updates an existing actor's position on the grid with smooth animation.
        /// </summary>
        public async Task MoveActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Grid.Position fromPosition, Darklands.Core.Domain.Grid.Position toPosition)
        {
            try
            {
                if (!_actorNodes.TryGetValue(actorId, out var actorNode) || actorNode == null)
                {
                    _logger?.Warning("Actor {ActorId} not found for movement", actorId);
                    return;
                }

                var endPosition = new Vector2(toPosition.X * TileSize, toPosition.Y * TileSize);

                // Store parameters for deferred call
                _pendingActorNode = actorNode;
                _pendingEndPosition = endPosition;
                _pendingActorId = actorId;
                _pendingFromPosition = fromPosition;
                _pendingToPosition = toPosition;

                // Use deferred call for tween operations to ensure main thread execution
                CallDeferred("MoveActorNodeDeferred");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error moving actor {ActorId}", actorId);
            }
        }

        /// <summary>
        /// Helper method to move actor node on main thread.
        /// </summary>
        private void MoveActorNodeDeferred()
        {
            if (_pendingActorNode != null && _actorNodes.ContainsKey(_pendingActorId))
            {
                // Get the ACTUAL actor node from our dictionary (not the pending one which might be stale)
                var actualActorNode = _actorNodes[_pendingActorId];
                
                // Set position immediately
                actualActorNode.Position = _pendingEndPosition;
                _logger?.Information("Actor {ActorId} moved to ({FromX},{FromY}) → ({ToX},{ToY})", 
                    _pendingActorId, 
                    _pendingFromPosition.X, _pendingFromPosition.Y,
                    _pendingToPosition.X, _pendingToPosition.Y);
                
                // TODO: Re-enable tween animation once basic movement is verified working
                // The tween code below should work but may have timing issues with deferred calls
                /*
                // Create and configure tween for smooth movement
                // CRITICAL FIX: Use "Position" (PascalCase) not "position" for C# property
                var tween = CreateTween();
                var tweenResult = tween.TweenProperty(actualActorNode, "Position", _pendingEndPosition, MoveDuration);
                
                // Check if tween was created successfully
                if (tween == null)
                {
                    GD.PrintErr($"[MOVE DEBUG] Failed to create tween!");
                    actualActorNode.Position = _pendingEndPosition; // Fallback: set directly
                }
                else
                {
                    GD.Print($"[MOVE DEBUG] Tween created successfully, animating over {MoveDuration}s");
                    
                    // Add completion callback to verify movement
                    tween.Finished += () => {
                        if (_actorNodes.TryGetValue(actorId, out var node))
                        {
                            GD.Print($"[MOVE DEBUG] Tween completed! Actor {actorId.Value} now at: {node.Position} (target was: {targetPos})");
                        }
                    };
                }
                */
                
                _pendingActorNode = null;
            }
            else
            {
                _logger?.Warning("Actor {ActorId} node not found", _pendingActorId);
            }
        }

        /// <summary>
        /// Updates an actor's visual state or appearance.
        /// </summary>
        public async Task UpdateActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Grid.Position position, ActorType actorType)
        {
            await CallDeferredAsync(actorId, position, actorType);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, Darklands.Core.Domain.Grid.Position pos, ActorType type)
            {
                try
                {
                    if (!_actorNodes.TryGetValue(id, out var actorNode) || actorNode == null)
                    {
                        _logger?.Warning("Actor {ActorId} not found for update", id);
                        return;
                    }

                    // Update color based on new type
                    actorNode.Color = GetActorColor(type);
                    
                    // Update position if needed
                    actorNode.Position = new Vector2(pos.X * TileSize, pos.Y * TileSize);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error updating actor {ActorId}", id);
                }
            }
        }

        /// <summary>
        /// Removes an actor from the visual display.
        /// </summary>
        public async Task RemoveActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Grid.Position position)
        {
            await CallDeferredAsync(actorId, position);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, Darklands.Core.Domain.Grid.Position pos)
            {
                try
                {
                    if (_actorNodes.TryGetValue(id, out var actorNode))
                    {
                        actorNode?.QueueFree();
                        _actorNodes.Remove(id);
                    }
                    else
                    {
                        _logger?.Warning("Actor {ActorId} not found for removal", id);
                    }

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error removing actor {ActorId}", id);
                }
            }
        }

        /// <summary>
        /// Highlights an actor to indicate selection, targeting, or special state.
        /// </summary>
        public async Task HighlightActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, ActorHighlightType highlightType)
        {
            await CallDeferredAsync(actorId, highlightType);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, ActorHighlightType type)
            {
                try
                {
                    if (!_actorNodes.TryGetValue(id, out var actorNode) || actorNode == null)
                    {
                        _logger?.Warning("Actor {ActorId} not found for highlighting", id);
                        return;
                    }

                    // For Phase 4, add a simple border effect
                    // Future: Different highlight types could use different effects
                    var originalColor = actorNode.Color;
                    var highlightColor = originalColor.Lightened(0.3f);
                    
                    // Create border effect by slightly increasing size and changing color temporarily
                    var tween = CreateTween();
                    tween.TweenProperty(actorNode, "Modulate", highlightColor, 0.1f);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error highlighting actor {ActorId}", id);
                }
            }
        }

        /// <summary>
        /// Removes highlighting from an actor.
        /// </summary>
        public async Task UnhighlightActorAsync(Darklands.Core.Domain.Grid.ActorId actorId)
        {
            await CallDeferredAsync(actorId);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id)
            {
                try
                {
                    if (!_actorNodes.TryGetValue(id, out var actorNode) || actorNode == null)
                    {
                        _logger?.Warning("Actor {ActorId} not found for unhighlighting", id);
                        return;
                    }

                    // Reset to normal appearance
                    var tween = CreateTween();
                    tween.TweenProperty(actorNode, "Modulate", Colors.White, 0.1f);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error unhighlighting actor {ActorId}", id);
                }
            }
        }

        /// <summary>
        /// Shows visual feedback related to an actor.
        /// </summary>
        public async Task ShowActorFeedbackAsync(Darklands.Core.Domain.Grid.ActorId actorId, ActorFeedbackType feedbackType, string? message = null)
        {
            await CallDeferredAsync(actorId, feedbackType, message);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, ActorFeedbackType type, string? msg)
            {
                try
                {
                    // For Phase 4, just print to console
                    // Future: Show floating text, particle effects, or screen shake
                    var feedbackMessage = msg ?? type.ToString();

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error showing feedback for actor {ActorId}", id);
                }
            }
        }

        /// <summary>
        /// Refreshes the display of all actors with their current states.
        /// </summary>
        public async Task RefreshAllActorsAsync()
        {
            await CallDeferredAsync();
            
            async Task CallDeferredAsync()
            {
                try
                {
                    // For Phase 4, we don't need to do much since we only have one test actor
                    // Future: Query application layer for all actor states and refresh displays
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Error(ex, "Error refreshing all actors");
                }
            }
        }

        /// <summary>
        /// Gets the appropriate color for an actor type.
        /// </summary>
        private Color GetActorColor(ActorType actorType)
        {
            return actorType switch
            {
                ActorType.Player => PlayerColor,
                ActorType.Enemy => EnemyColor,
                ActorType.Neutral => NeutralColor,
                ActorType.Interactive => InteractiveColor,
                _ => PlayerColor // Default to player color
            };
        }
    }
}