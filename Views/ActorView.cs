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
        private const int TileSize = 16;
        private const float MoveDuration = 0.3f; // Seconds for movement animation

        // Actor colors for different types
        private readonly Color PlayerColor = new(0.1f, 0.46f, 0.82f, 1.0f); // Blue #1976D2
        private readonly Color EnemyColor = new(0.96f, 0.26f, 0.21f, 1.0f);  // Red #F44336
        private readonly Color NeutralColor = new(0.61f, 0.61f, 0.61f, 1.0f); // Gray #9E9E9E
        private readonly Color InteractiveColor = new(1.0f, 0.59f, 0.0f, 1.0f); // Orange #FF9800

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
            await CallDeferredAsync(actorId, position, actorType);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, Darklands.Core.Domain.Grid.Position pos, ActorType type)
            {
                try
                {
                    // Remove existing actor node if it exists
                    if (_actorNodes.TryGetValue(id, out var existingNode))
                    {
                        existingNode?.QueueFree();
                        _actorNodes.Remove(id);
                    }

                    // Create new ColorRect node for the actor
                    var actorNode = new ColorRect
                    {
                        Size = new Vector2(TileSize, TileSize),
                        Color = GetActorColor(type),
                        Position = new Vector2(pos.X * TileSize, pos.Y * TileSize)
                    };

                    // Add to scene tree
                    AddChild(actorNode);
                    _actorNodes[id] = actorNode;

                    GD.Print($"Actor {id.Value} displayed at position {pos} as {type}");
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error displaying actor {id.Value}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Updates an existing actor's position on the grid with smooth animation.
        /// </summary>
        public async Task MoveActorAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Grid.Position fromPosition, Darklands.Core.Domain.Grid.Position toPosition)
        {
            await CallDeferredAsync(actorId, fromPosition, toPosition);
            
            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, Darklands.Core.Domain.Grid.Position from, Darklands.Core.Domain.Grid.Position to)
            {
                try
                {
                    if (!_actorNodes.TryGetValue(id, out var actorNode) || actorNode == null)
                    {
                        GD.PrintErr($"Actor {id.Value} not found for movement");
                        return;
                    }

                    var startPosition = new Vector2(from.X * TileSize, from.Y * TileSize);
                    var endPosition = new Vector2(to.X * TileSize, to.Y * TileSize);

                    // Create and configure tween for smooth movement
                    var tween = CreateTween();
                    tween.TweenProperty(actorNode, "position", endPosition, MoveDuration);
                    
                    // Wait for animation to complete
                    await tween.ToSignal(tween, Tween.SignalName.Finished);
                    
                    GD.Print($"Actor {id.Value} moved from {from} to {to}");
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"Error moving actor {id.Value}: {ex.Message}");
                }
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
                    tween.TweenProperty(actorNode, "modulate", highlightColor, 0.1f);

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
                    tween.TweenProperty(actorNode, "modulate", Colors.White, 0.1f);

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