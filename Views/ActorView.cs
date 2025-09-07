using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        private readonly Dictionary<Darklands.Core.Domain.Grid.ActorId, ColorRect> _actorNodes = new();
        private const int TileSize = 32;
        private const float MoveDuration = 0.3f; // Seconds for movement animation

        // Actor colors for different types
        private readonly Color PlayerColor = new(0.1f, 0.46f, 0.82f, 1.0f); // Blue #1976D2
        private readonly Color EnemyColor = new(0.96f, 0.26f, 0.21f, 1.0f);  // Red #F44336
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
                GD.Print("ActorView initialized successfully");
            }
            catch (Exception ex)
            {
                GD.PrintErr($"ActorView._Ready error: {ex.Message}");
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
        /// Creates and displays a new actor at the specified position.
        /// </summary>
        public async Task DisplayActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Grid.Position position, ActorType actorType)
        {
            try
            {
                // Remove existing actor node if it exists
                if (_actorNodes.TryGetValue(actorId, out var existingNode))
                {
                    _pendingActorId = actorId;
                    CallDeferred("RemoveActorNodeDeferred");
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

                GD.Print($"Actor {actorId.Value} displayed at position {position} as {actorType}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error displaying actor {actorId.Value}: {ex.Message}");
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
                GD.Print($"ActorView: Actor added at position {_pendingActorNode.Position} with color {_pendingActorNode.Color}");
                _pendingActorNode = null;
            }
        }

        /// <summary>
        /// Helper method to remove actor node on main thread.
        /// </summary>
        private void RemoveActorNodeDeferred()
        {
            if (_actorNodes.TryGetValue(_pendingActorId, out var existingNode))
            {
                existingNode?.QueueFree();
                _actorNodes.Remove(_pendingActorId);
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
                    GD.PrintErr($"Actor {actorId.Value} not found for movement");
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
                GD.PrintErr($"Error moving actor {actorId.Value}: {ex.Message}");
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
                
                // Log current position before movement
                var startPos = actualActorNode.Position;
                GD.Print($"[MOVE DEBUG] Actor {_pendingActorId.Value} starting at pixel position: {startPos}, moving to: {_pendingEndPosition}");
                
                // Capture values for the callback
                var actorId = _pendingActorId;
                var targetPos = _pendingEndPosition;
                
                // CRITICAL: Set position immediately (bypassing tween for now to test basic movement)
                actualActorNode.Position = _pendingEndPosition;
                GD.Print($"[MOVE DEBUG] Position set directly to: {actualActorNode.Position}");
                
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
                
                GD.Print($"Actor {_pendingActorId.Value} movement initiated from grid({_pendingFromPosition}) to grid({_pendingToPosition})");
                _pendingActorNode = null;
            }
            else
            {
                GD.PrintErr($"[MOVE DEBUG] MoveActorNodeDeferred called but actor node not found in dictionary!");
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
                        GD.PrintErr($"Actor {id.Value} not found for update");
                        return;
                    }

                    // Update color based on new type
                    actorNode.Color = GetActorColor(type);
                    
                    // Update position if needed
                    actorNode.Position = new Vector2(pos.X * TileSize, pos.Y * TileSize);

                    GD.Print($"Actor {id.Value} updated at position {pos}");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error updating actor {id.Value}: {ex.Message}");
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
                        GD.Print($"Actor {id.Value} removed from position {pos}");
                    }
                    else
                    {
                        GD.PrintErr($"Actor {id.Value} not found for removal");
                    }

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error removing actor {id.Value}: {ex.Message}");
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
                        GD.PrintErr($"Actor {id.Value} not found for highlighting");
                        return;
                    }

                    // For Phase 4, add a simple border effect
                    // Future: Different highlight types could use different effects
                    var originalColor = actorNode.Color;
                    var highlightColor = originalColor.Lightened(0.3f);
                    
                    // Create border effect by slightly increasing size and changing color temporarily
                    var tween = CreateTween();
                    tween.TweenProperty(actorNode, "Modulate", highlightColor, 0.1f);

                    GD.Print($"Actor {id.Value} highlighted with {type}");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error highlighting actor {id.Value}: {ex.Message}");
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
                        GD.PrintErr($"Actor {id.Value} not found for unhighlighting");
                        return;
                    }

                    // Reset to normal appearance
                    var tween = CreateTween();
                    tween.TweenProperty(actorNode, "Modulate", Colors.White, 0.1f);

                    GD.Print($"Actor {id.Value} unhighlighted");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error unhighlighting actor {id.Value}: {ex.Message}");
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
                    GD.Print($"Actor {id.Value} feedback: {feedbackMessage} ({type})");

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error showing feedback for actor {id.Value}: {ex.Message}");
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
                    GD.Print($"Refreshed display for {_actorNodes.Count} actors");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error refreshing all actors: {ex.Message}");
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