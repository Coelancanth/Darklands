using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Darklands.Core.Domain.Debug;
using Godot;
using System;
using System.Collections.Concurrent;
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
        private ICategoryLogger? _logger;
        // FIXED BR_007: Use ConcurrentDictionary for thread-safe access from async operations
        private readonly ConcurrentDictionary<Darklands.Core.Domain.Grid.ActorId, ColorRect> _actorNodes = new();
        private readonly ConcurrentDictionary<Darklands.Core.Domain.Grid.ActorId, ProgressBar> _healthBars = new();
        private const int TileSize = 64;
        private const float MoveDuration = 0.3f; // Seconds for movement animation

        // Actor colors for different types
        private readonly Color PlayerColor = new(0.1f, 0.46f, 0.82f, 1.0f); // Blue #1976D2
        private readonly Color EnemyColor = new(0.59f, 0.36f, 0.20f, 1.0f);  // Brown #964D33
        private readonly Color NeutralColor = new(0.61f, 0.61f, 0.61f, 1.0f); // Gray #9E9E9E
        private readonly Color InteractiveColor = new(1.0f, 0.59f, 0.0f, 1.0f); // Orange #FF9800

        // Queue-based storage for deferred operations - fixes race condition (TD_011)
        private readonly Queue<ActorCreationData> _pendingActorCreations = new();
        private readonly Queue<ActorMoveData> _pendingActorMoves = new();
        private readonly Queue<ActorVisibilityData> _pendingVisibilityUpdates = new();

        // Data structures to hold operation parameters
        private record ActorCreationData(ColorRect ActorNode, Darklands.Core.Domain.Grid.ActorId ActorId);
        private record ActorMoveData(ColorRect ActorNode, Vector2 EndPosition, Darklands.Core.Domain.Grid.ActorId ActorId,
            Darklands.Core.Domain.Grid.Position FromPosition, Darklands.Core.Domain.Grid.Position ToPosition);
        private record ActorVisibilityData(Darklands.Core.Domain.Grid.ActorId ActorId, bool IsVisible);

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
                _logger?.Log(LogLevel.Error, LogCategory.System, "ActorView._Ready error: {Error}", ex.Message);
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
        public void SetLogger(ICategoryLogger logger)
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
                // Remove existing actor node if it exists (thread-safe with ConcurrentDictionary)
                if (_actorNodes.TryRemove(actorId, out var existingNode))
                {
                    existingNode?.QueueFree();
                    _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Removed existing actor node for {ActorId}", actorId);
                }

                // Create new ColorRect node for the actor
                var actorNode = new ColorRect
                {
                    Size = new Vector2(TileSize, TileSize),
                    Color = GetActorColor(actorType),
                    Position = new Vector2(position.X * TileSize, position.Y * TileSize)
                };

                // Create health bar as child of actor node
                if (actorType != ActorType.Neutral) // Only add health bars to actors that need them
                {
                    var healthBar = CreateHealthBar(actorId);
                    actorNode.AddChild(healthBar);
                }

                // Queue for deferred call - prevents race condition
                lock (_pendingActorCreations)
                {
                    _pendingActorCreations.Enqueue(new ActorCreationData(actorNode, actorId));
                }
                CallDeferred("ProcessPendingActorCreations");

                // Log using Gameplay category for fine-grained filtering
                if (DebugSystem.Instance?.Logger != null)
                {
                    DebugSystem.Instance.Logger.Log(LogLevel.Information, LogCategory.Gameplay,
                        "{0} created at ({1},{2})", actorId, position.X, position.Y);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error displaying actor {ActorId}: {Error}", actorId, ex.Message);
            }
        }

        /// <summary>
        /// Helper method to process queued actor creations on main thread.
        /// Fixes race condition by processing all queued operations sequentially.
        /// </summary>
        private void ProcessPendingActorCreations()
        {
            lock (_pendingActorCreations)
            {
                while (_pendingActorCreations.Count > 0)
                {
                    var creationData = _pendingActorCreations.Dequeue();
                    AddChild(creationData.ActorNode);
                    // BR_007 FIX: Use thread-safe AddOrUpdate to handle concurrent access
                    _actorNodes.AddOrUpdate(creationData.ActorId, creationData.ActorNode, (key, oldValue) => creationData.ActorNode);
                    _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Processed actor creation for {ActorId}", creationData.ActorId);
                }
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
                    _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found for movement", actorId);
                    return;
                }

                var endPosition = new Vector2(toPosition.X * TileSize, toPosition.Y * TileSize);

                // Queue parameters for deferred call - prevents race condition
                lock (_pendingActorMoves)
                {
                    _pendingActorMoves.Enqueue(new ActorMoveData(actorNode, endPosition, actorId, fromPosition, toPosition));
                }

                // Use deferred call for tween operations to ensure main thread execution
                CallDeferred("ProcessPendingActorMoves");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error moving actor {ActorId}: {Error}", actorId, ex.Message);
            }
        }

        /// <summary>
        /// Helper method to process queued actor moves on main thread.
        /// Fixes race condition by processing all queued move operations sequentially.
        /// </summary>
        private void ProcessPendingActorMoves()
        {
            lock (_pendingActorMoves)
            {
                while (_pendingActorMoves.Count > 0)
                {
                    var moveData = _pendingActorMoves.Dequeue();

                    if (_actorNodes.ContainsKey(moveData.ActorId))
                    {
                        // Get the ACTUAL actor node from our dictionary
                        var actualActorNode = _actorNodes[moveData.ActorId];

                        // Set position immediately
                        actualActorNode.Position = moveData.EndPosition;
                        // Log using Gameplay category for fine-grained filtering
                        if (DebugSystem.Instance?.Logger != null)
                        {
                            DebugSystem.Instance.Logger.Log(LogLevel.Information, LogCategory.Gameplay,
                                "{0} moved from ({1},{2}) to ({3},{4})",
                                moveData.ActorId,
                                moveData.FromPosition.X, moveData.FromPosition.Y,
                                moveData.ToPosition.X, moveData.ToPosition.Y);
                        }

                        // TODO: Re-enable tween animation once basic movement is verified working
                    }
                    else
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Cannot move actor {ActorId} - not found in actor nodes", moveData.ActorId);
                    }
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
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found for update", id);
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
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error updating actor {ActorId}: {Error}", id, ex.Message);
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
                    if (_actorNodes.TryRemove(id, out var actorNode))
                    {
                        actorNode?.QueueFree();
                        _healthBars.TryRemove(id, out _); // Also remove health bar reference (thread-safe)
                    }
                    else
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found for removal", id);
                    }

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error removing actor {ActorId}: {Error}", id, ex.Message);
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
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found for highlighting", id);
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
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error highlighting actor {ActorId}: {Error}", id, ex.Message);
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
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found for unhighlighting", id);
                        return;
                    }

                    // Reset to normal appearance
                    var tween = CreateTween();
                    tween.TweenProperty(actorNode, "Modulate", Colors.White, 0.1f);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error unhighlighting actor {ActorId}: {Error}", id, ex.Message);
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
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error showing feedback for actor {ActorId}: {Error}", id, ex.Message);
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
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error refreshing all actors: {Error}", ex.Message);
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

        /// <summary>
        /// Creates a health bar as a child node of an actor.
        /// The health bar will automatically move with its parent actor.
        /// </summary>
        private ProgressBar CreateHealthBar(Darklands.Core.Domain.Grid.ActorId actorId)
        {
            var healthBar = new ProgressBar
            {
                Size = new Vector2(TileSize - 8, 4), // Thinner health bar
                Position = new Vector2(4, -12), // Positioned above the actor
                MinValue = 0,
                MaxValue = 100,
                Value = 100, // Default to full health
                ShowPercentage = false // We'll use a label instead
            };

            // Add a label to show HP numbers
            var hpLabel = new Label
            {
                Text = "100/100",
                Position = new Vector2(0, -4), // Above the health bar
                Size = new Vector2(TileSize - 8, 12),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Set font size using method
            hpLabel.AddThemeFontSizeOverride("font_size", 10);

            // Style the label for visibility
            hpLabel.AddThemeColorOverride("font_color", Colors.White);
            hpLabel.AddThemeColorOverride("font_shadow_color", Colors.Black);
            hpLabel.AddThemeConstantOverride("shadow_offset_x", 1);
            hpLabel.AddThemeConstantOverride("shadow_offset_y", 1);

            healthBar.AddChild(hpLabel);
            healthBar.SetMeta("hp_label", hpLabel); // Store reference for updates

            // Style the health bar
            var styleBoxFilled = new StyleBoxFlat();
            styleBoxFilled.BgColor = new Color(0.0f, 0.8f, 0.0f); // Green for health
            styleBoxFilled.BorderWidthTop = 1;
            styleBoxFilled.BorderWidthBottom = 1;
            styleBoxFilled.BorderWidthLeft = 1;
            styleBoxFilled.BorderWidthRight = 1;
            styleBoxFilled.BorderColor = new Color(0.2f, 0.2f, 0.2f);

            var styleBoxBackground = new StyleBoxFlat();
            styleBoxBackground.BgColor = new Color(0.3f, 0.1f, 0.1f); // Dark red background
            styleBoxBackground.BorderWidthTop = 1;
            styleBoxBackground.BorderWidthBottom = 1;
            styleBoxBackground.BorderWidthLeft = 1;
            styleBoxBackground.BorderWidthRight = 1;
            styleBoxBackground.BorderColor = new Color(0.2f, 0.2f, 0.2f);

            healthBar.AddThemeStyleboxOverride("fill", styleBoxFilled);
            healthBar.AddThemeStyleboxOverride("background", styleBoxBackground);

            // Store reference for later updates (thread-safe)
            _healthBars.AddOrUpdate(actorId, healthBar, (key, oldValue) => healthBar);

            return healthBar;
        }

        /// <summary>
        /// Updates the health value of an actor's health bar.
        /// </summary>
        public void UpdateActorHealth(Darklands.Core.Domain.Grid.ActorId actorId, int currentHealth, int maxHealth)
        {
            if (_healthBars.TryGetValue(actorId, out var healthBar))
            {
                healthBar.MaxValue = maxHealth;
                healthBar.Value = currentHealth;

                // Update the HP label
                if (healthBar.HasMeta("hp_label"))
                {
                    var labelVariant = healthBar.GetMeta("hp_label");
                    if (labelVariant.AsGodotObject() is Label label)
                    {
                        label.Text = $"{currentHealth}/{maxHealth}";
                    }
                }

                // Change color based on health percentage
                var healthPercent = (float)currentHealth / maxHealth;
                var styleBoxFilled = new StyleBoxFlat();

                if (healthPercent > 0.5f)
                    styleBoxFilled.BgColor = new Color(0.0f, 0.8f, 0.0f); // Green
                else if (healthPercent > 0.25f)
                    styleBoxFilled.BgColor = new Color(0.8f, 0.8f, 0.0f); // Yellow
                else
                    styleBoxFilled.BgColor = new Color(0.8f, 0.0f, 0.0f); // Red

                styleBoxFilled.BorderWidthTop = 1;
                styleBoxFilled.BorderWidthBottom = 1;
                styleBoxFilled.BorderWidthLeft = 1;
                styleBoxFilled.BorderWidthRight = 1;
                styleBoxFilled.BorderColor = new Color(0.2f, 0.2f, 0.2f);

                healthBar.AddThemeStyleboxOverride("fill", styleBoxFilled);
            }
        }

        /// <summary>
        /// Updates an actor's health bar with new health values.
        /// Provides smooth transitions for health changes using the existing child health bar.
        /// </summary>
        public async Task UpdateActorHealthAsync(Darklands.Core.Domain.Grid.ActorId actorId, Darklands.Core.Domain.Actor.Health oldHealth, Darklands.Core.Domain.Actor.Health newHealth)
        {
            await CallDeferredAsync(actorId, oldHealth, newHealth);

            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, Darklands.Core.Domain.Actor.Health old, Darklands.Core.Domain.Actor.Health current)
            {
                try
                {
                    // Use existing UpdateActorHealth method with integer values
                    UpdateActorHealth(id, current.Current, current.Maximum);

                    _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Updated health for actor {ActorId} from {OldHealth} to {NewHealth}",
                        id, old, current);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error updating actor health for {ActorId}: {Error}", id, ex.Message);
                }
            }
        }

        /// <summary>
        /// Shows visual feedback for health changes (damage numbers, healing effects).
        /// Creates floating text above the actor using their existing position.
        /// </summary>
        public async Task ShowHealthFeedbackAsync(Darklands.Core.Domain.Grid.ActorId actorId, HealthFeedbackType feedbackType, int amount, Darklands.Core.Domain.Grid.Position position)
        {
            await CallDeferredAsync(actorId, feedbackType, amount, position);

            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, HealthFeedbackType type, int amt, Darklands.Core.Domain.Grid.Position pos)
            {
                try
                {
                    var pixelPosition = new Vector2(pos.X * TileSize, pos.Y * TileSize);
                    pixelPosition.X += TileSize / 2; // Center on tile
                    pixelPosition.Y += TileSize / 2;

                    var feedbackLabel = CreateHealthFeedbackLabel(type, amt);
                    feedbackLabel.Position = pixelPosition + new Vector2(0, -30); // Above actor

                    AddChild(feedbackLabel);
                    AnimateHealthFeedbackText(feedbackLabel);

                    _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Showed {FeedbackType} feedback for actor {ActorId}: {Amount}",
                        type, id, amt);

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error showing health feedback for actor {ActorId}: {Error}", id, ex.Message);
                }
            }
        }

        /// <summary>
        /// Highlights an actor's health bar to indicate targeting or special state.
        /// Uses the existing health bar infrastructure.
        /// </summary>
        public async Task HighlightActorHealthBarAsync(Darklands.Core.Domain.Grid.ActorId actorId, HealthHighlightType highlightType)
        {
            await CallDeferredAsync(actorId, highlightType);

            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id, HealthHighlightType type)
            {
                try
                {
                    if (_healthBars.TryGetValue(id, out var healthBar) && healthBar != null)
                    {
                        ApplyHealthBarHighlight(healthBar, type);
                        _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Applied {HighlightType} highlight to health bar for actor {ActorId}",
                            type, id);
                    }
                    else
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Health bar not found for highlighting: actor {ActorId}", id);
                    }

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error highlighting health bar for actor {ActorId}: {Error}", id, ex.Message);
                }
            }
        }

        /// <summary>
        /// Removes highlighting from an actor's health bar.
        /// </summary>
        public async Task UnhighlightActorHealthBarAsync(Darklands.Core.Domain.Grid.ActorId actorId)
        {
            await CallDeferredAsync(actorId);

            async Task CallDeferredAsync(Darklands.Core.Domain.Grid.ActorId id)
            {
                try
                {
                    if (_healthBars.TryGetValue(id, out var healthBar) && healthBar != null)
                    {
                        ClearHealthBarHighlight(healthBar);
                        _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Cleared highlight from health bar for actor {ActorId}", id);
                    }
                    else
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Health bar not found for unhighlighting: actor {ActorId}", id);
                    }

                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error unhighlighting health bar for actor {ActorId}: {Error}", id, ex.Message);
                }
            }
        }

        /// <summary>
        /// Sets the visibility of an actor based on player vision.
        /// Used by the fog of war system to show/hide actors dynamically.
        /// </summary>
        public async Task SetActorVisibilityAsync(Darklands.Core.Domain.Grid.ActorId actorId, bool isVisible)
        {
            try
            {
                // Queue for deferred call - ensures thread safety with Godot nodes
                lock (_pendingVisibilityUpdates)
                {
                    _pendingVisibilityUpdates.Enqueue(new ActorVisibilityData(actorId, isVisible));
                }
                CallDeferred("ProcessPendingVisibilityUpdates");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger?.Log(LogLevel.Error, LogCategory.Gameplay, "Error queueing actor visibility update for {ActorId}: {Error}", actorId, ex.Message);
            }
        }

        /// <summary>
        /// Helper method to process queued visibility updates on the main thread.
        /// Ensures thread-safe updates to Godot node visibility properties.
        /// </summary>
        private void ProcessPendingVisibilityUpdates()
        {
            lock (_pendingVisibilityUpdates)
            {
                while (_pendingVisibilityUpdates.Count > 0)
                {
                    var visibilityData = _pendingVisibilityUpdates.Dequeue();

                    if (_actorNodes.TryGetValue(visibilityData.ActorId, out var actorNode) && actorNode != null)
                    {
                        actorNode.Visible = visibilityData.IsVisible;
                        _logger?.Log(LogLevel.Debug, LogCategory.Gameplay, "Set actor {ActorId} visibility to {Visible}",
                            visibilityData.ActorId.Value.ToString()[..8],
                            visibilityData.IsVisible ? "VISIBLE" : "HIDDEN");
                    }
                    else
                    {
                        _logger?.Log(LogLevel.Warning, LogCategory.Gameplay, "Actor {ActorId} not found for visibility update", visibilityData.ActorId);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a feedback label for health changes.
        /// Extracted from HealthView for consistency.
        /// </summary>
        private Label CreateHealthFeedbackLabel(HealthFeedbackType feedbackType, int amount)
        {
            var label = new Label();

            // Colors for feedback text
            var DamageTextColor = new Color(1.0f, 0.3f, 0.3f, 1.0f);   // Bright red for damage
            var HealingTextColor = new Color(0.3f, 1.0f, 0.3f, 1.0f);  // Bright green for healing
            var CriticalTextColor = new Color(1.0f, 0.8f, 0.0f, 1.0f); // Orange for critical warnings

            switch (feedbackType)
            {
                case HealthFeedbackType.Damage:
                    label.Text = $"-{amount}";
                    label.Modulate = DamageTextColor;
                    break;
                case HealthFeedbackType.Healing:
                    label.Text = $"+{amount}";
                    label.Modulate = HealingTextColor;
                    break;
                case HealthFeedbackType.Death:
                    label.Text = "DEAD";
                    label.Modulate = DamageTextColor;
                    break;
                case HealthFeedbackType.CriticalHealth:
                    label.Text = "CRITICAL!";
                    label.Modulate = CriticalTextColor;
                    break;
                case HealthFeedbackType.FullRestore:
                    label.Text = "RESTORED";
                    label.Modulate = HealingTextColor;
                    break;
            }

            // Make text bold and visible
            label.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
            return label;
        }

        /// <summary>
        /// Animates health feedback text (fade up and out).
        /// Extracted from HealthView for consistency.
        /// </summary>
        private void AnimateHealthFeedbackText(Label label)
        {
            var tween = CreateTween();
            tween.SetParallel(true);

            // Move up and fade out
            var startPos = label.Position;
            var endPos = startPos + new Vector2(0, -30);

            tween.TweenProperty(label, "position", endPos, 1.0f);
            tween.TweenProperty(label, "modulate:a", 0.0f, 1.0f);

            // Remove after animation
            tween.TweenCallback(Callable.From(() => label.QueueFree())).SetDelay(1.0f);
        }

        /// <summary>
        /// Applies highlighting effects to a health bar.
        /// Uses existing ProgressBar infrastructure for consistent look.
        /// </summary>
        private void ApplyHealthBarHighlight(ProgressBar healthBar, HealthHighlightType highlightType)
        {
            switch (highlightType)
            {
                case HealthHighlightType.Critical:
                    // Pulsing red effect for critical health
                    var criticalTween = CreateTween();
                    criticalTween.SetLoops();
                    criticalTween.TweenProperty(healthBar, "modulate", Colors.Red, 0.5f);
                    criticalTween.TweenProperty(healthBar, "modulate", Colors.White, 0.5f);
                    break;

                case HealthHighlightType.HealTarget:
                    healthBar.Modulate = Colors.LightGreen;
                    break;

                case HealthHighlightType.DamageTarget:
                    healthBar.Modulate = Colors.LightCoral;
                    break;

                case HealthHighlightType.Selected:
                    healthBar.Modulate = Colors.Yellow;
                    break;
            }
        }

        /// <summary>
        /// Clears highlighting effects from a health bar.
        /// </summary>
        private void ClearHealthBarHighlight(ProgressBar healthBar)
        {
            healthBar.Modulate = Colors.White;
        }
    }
}
