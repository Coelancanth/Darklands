using Darklands.Core.Presentation.Views;
using Darklands.Core.Presentation.Presenters;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Actor;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Darklands.Views
{
    /// <summary>
    /// Concrete Godot implementation of the health view interface.
    /// Manages the visual representation of actor health bars using Godot UI controls.
    /// Provides health bars with progress indication, damage numbers, and visual feedback.
    /// </summary>
    public partial class HealthView : Node2D, IHealthView
    {
        private HealthPresenter? _presenter;
        private ILogger? _logger;
        private readonly Dictionary<ActorId, HealthBarNode> _healthBars = new();
        private const int TileSize = 32;
        private const int HealthBarWidth = 24;
        private const int HealthBarHeight = 4;
        private const int HealthBarOffsetY = -8; // Above the actor

        // Colors for health bar states
        private readonly Color HealthGreenColor = new(0.2f, 0.8f, 0.2f, 1.0f);  // Green for healthy
        private readonly Color HealthYellowColor = new(0.9f, 0.9f, 0.2f, 1.0f); // Yellow for moderate damage
        private readonly Color HealthRedColor = new(0.9f, 0.2f, 0.2f, 1.0f);    // Red for critical/low health
        private readonly Color HealthBarBgColor = new(0.2f, 0.2f, 0.2f, 0.8f);  // Dark background

        // Colors for feedback text
        private readonly Color DamageTextColor = new(1.0f, 0.3f, 0.3f, 1.0f);   // Bright red for damage
        private readonly Color HealingTextColor = new(0.3f, 1.0f, 0.3f, 1.0f);  // Bright green for healing
        private readonly Color CriticalTextColor = new(1.0f, 0.8f, 0.0f, 1.0f); // Orange for critical warnings

        // Temporary storage for deferred method parameters
        private ActorId _pendingActorId;
        private Position _pendingPosition;
        private Position _pendingFromPosition;
        private Position _pendingToPosition;
        private Health _pendingHealth;
        private Health _pendingOldHealth;
        private Health _pendingNewHealth;
        private HealthFeedbackType _pendingFeedbackType;
        private int _pendingAmount;
        private HealthHighlightType _pendingHighlightType;

        /// <summary>
        /// Called when the node is added to the scene tree.
        /// Sets up the health display system.
        /// </summary>
        public override void _Ready()
        {
            try
            {
                // Health view setup will be handled by presenter initialization
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "HealthView._Ready error");
            }
        }

        /// <summary>
        /// Sets the logger for this view.
        /// Called by GameManager during MVP setup.
        /// </summary>
        /// <param name="logger">Logger instance for structured logging</param>
        public void SetLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets the presenter for this view.
        /// Called by GameManager during MVP setup.
        /// </summary>
        /// <param name="presenter">Health presenter instance</param>
        public void SetPresenter(HealthPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
        }

        /// <summary>
        /// Creates and displays a health bar for an actor at the specified position.
        /// </summary>
        public async Task DisplayHealthBarAsync(ActorId actorId, Position position, Health health)
        {
            _pendingActorId = actorId;
            _pendingPosition = position;
            _pendingHealth = health;
            
            await Task.Run(() =>
            {
                CallDeferred("DisplayHealthBarDeferred");
            });
        }

        /// <summary>
        /// Deferred method to create health bar on main thread.
        /// </summary>
        private void DisplayHealthBarDeferred()
        {
            try
            {
                if (_healthBars.ContainsKey(_pendingActorId))
                {
                    _logger?.Warning("Health bar already exists for actor {ActorId}, removing old one", _pendingActorId);
                    // Remove the existing health bar
                    var existingHealthBar = _healthBars[_pendingActorId];
                    existingHealthBar.QueueFree();
                    _healthBars.Remove(_pendingActorId);
                }

                var healthBarNode = CreateHealthBarNode(_pendingActorId, _pendingPosition, _pendingHealth);
                _healthBars[_pendingActorId] = healthBarNode;
                AddChild(healthBarNode);

                _logger?.Debug("Created health bar for actor {ActorId} at {Position} with health {Health}", 
                    _pendingActorId, _pendingPosition, _pendingHealth);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error creating health bar for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Updates an actor's health bar with new health values.
        /// </summary>
        public async Task UpdateHealthAsync(ActorId actorId, Health oldHealth, Health newHealth)
        {
            _pendingActorId = actorId;
            _pendingOldHealth = oldHealth;
            _pendingNewHealth = newHealth;
            
            await Task.Run(() =>
            {
                CallDeferred("UpdateHealthDeferred");
            });
        }

        /// <summary>
        /// Deferred method to update health bar on main thread.
        /// </summary>
        private void UpdateHealthDeferred()
        {
            try
            {
                if (!_healthBars.TryGetValue(_pendingActorId, out var healthBarNode))
                {
                    _logger?.Warning("Health bar not found for actor {ActorId} during update", _pendingActorId);
                    return;
                }

                healthBarNode.UpdateHealth(_pendingNewHealth);

                _logger?.Debug("Updated health bar for actor {ActorId} from {OldHealth} to {NewHealth}",
                    _pendingActorId, _pendingOldHealth, _pendingNewHealth);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error updating health bar for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Moves a health bar to a new position when the actor moves.
        /// </summary>
        public async Task MoveHealthBarAsync(ActorId actorId, Position fromPosition, Position toPosition)
        {
            _pendingActorId = actorId;
            _pendingFromPosition = fromPosition;
            _pendingToPosition = toPosition;
            
            await Task.Run(() =>
            {
                CallDeferred("MoveHealthBarDeferred");
            });
        }

        /// <summary>
        /// Deferred method to move health bar on main thread.
        /// </summary>
        private void MoveHealthBarDeferred()
        {
            try
            {
                if (!_healthBars.TryGetValue(_pendingActorId, out var healthBarNode))
                {
                    _logger?.Warning("Health bar not found for actor {ActorId} during move", _pendingActorId);
                    return;
                }

                var newPixelPosition = GridPositionToPixel(_pendingToPosition);
                // Center the health bar on the tile (add half tile size to X and Y)
                newPixelPosition.X += TileSize / 2;
                newPixelPosition.Y += TileSize / 2;
                healthBarNode.Position = newPixelPosition;

                _logger?.Debug("Moved health bar for actor {ActorId} from {FromPosition} to {ToPosition}",
                    _pendingActorId, _pendingFromPosition, _pendingToPosition);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error moving health bar for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Removes a health bar from the display.
        /// </summary>
        public async Task RemoveHealthBarAsync(ActorId actorId, Position position)
        {
            _pendingActorId = actorId;
            _pendingPosition = position;
            
            await Task.Run(() =>
            {
                CallDeferred("RemoveHealthBarDeferred");
            });
        }

        /// <summary>
        /// Deferred method to remove health bar on main thread.
        /// </summary>
        private void RemoveHealthBarDeferred()
        {
            try
            {
                if (_healthBars.TryGetValue(_pendingActorId, out var healthBarNode))
                {
                    healthBarNode.QueueFree();
                    _healthBars.Remove(_pendingActorId);
                    _logger?.Debug("Removed health bar for actor {ActorId}", _pendingActorId);
                }
                else
                {
                    _logger?.Warning("Health bar not found for removal: actor {ActorId}", _pendingActorId);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error removing health bar for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Shows visual feedback for health changes.
        /// </summary>
        public async Task ShowHealthFeedbackAsync(ActorId actorId, HealthFeedbackType feedbackType, int amount, Position position)
        {
            _pendingActorId = actorId;
            _pendingFeedbackType = feedbackType;
            _pendingAmount = amount;
            _pendingPosition = position;
            
            await Task.Run(() =>
            {
                CallDeferred("ShowHealthFeedbackDeferred");
            });
        }

        /// <summary>
        /// Deferred method to show health feedback on main thread.
        /// </summary>
        private void ShowHealthFeedbackDeferred()
        {
            try
            {
                var pixelPosition = GridPositionToPixel(_pendingPosition);
                // Center the feedback text on the tile
                pixelPosition.X += TileSize / 2;
                pixelPosition.Y += TileSize / 2;
                
                var feedbackLabel = CreateFeedbackLabel(_pendingFeedbackType, _pendingAmount);
                feedbackLabel.Position = pixelPosition + new Vector2(0, HealthBarOffsetY - 10);
                
                AddChild(feedbackLabel);
                
                // Animate the feedback text
                AnimateFeedbackText(feedbackLabel);

                _logger?.Debug("Showed {FeedbackType} feedback for actor {ActorId}: {Amount}",
                    _pendingFeedbackType, _pendingActorId, _pendingAmount);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error showing health feedback for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Highlights a health bar to indicate targeting or special state.
        /// </summary>
        public async Task HighlightHealthBarAsync(ActorId actorId, HealthHighlightType highlightType)
        {
            _pendingActorId = actorId;
            _pendingHighlightType = highlightType;
            
            await Task.Run(() =>
            {
                CallDeferred("HighlightHealthBarDeferred");
            });
        }

        /// <summary>
        /// Deferred method to highlight health bar on main thread.
        /// </summary>
        private void HighlightHealthBarDeferred()
        {
            try
            {
                if (_healthBars.TryGetValue(_pendingActorId, out var healthBarNode))
                {
                    healthBarNode.SetHighlight(_pendingHighlightType);
                    _logger?.Debug("Applied {HighlightType} highlight to health bar for actor {ActorId}",
                        _pendingHighlightType, _pendingActorId);
                }
                else
                {
                    _logger?.Warning("Health bar not found for highlighting: actor {ActorId}", _pendingActorId);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error highlighting health bar for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Removes highlighting from a health bar.
        /// </summary>
        public async Task UnhighlightHealthBarAsync(ActorId actorId)
        {
            _pendingActorId = actorId;
            
            await Task.Run(() =>
            {
                CallDeferred("UnhighlightHealthBarDeferred");
            });
        }

        /// <summary>
        /// Deferred method to unhighlight health bar on main thread.
        /// </summary>
        private void UnhighlightHealthBarDeferred()
        {
            try
            {
                if (_healthBars.TryGetValue(_pendingActorId, out var healthBarNode))
                {
                    healthBarNode.ClearHighlight();
                    _logger?.Debug("Cleared highlight from health bar for actor {ActorId}", _pendingActorId);
                }
                else
                {
                    _logger?.Warning("Health bar not found for unhighlighting: actor {ActorId}", _pendingActorId);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error unhighlighting health bar for actor {ActorId}", _pendingActorId);
            }
        }

        /// <summary>
        /// Refreshes all health bar displays with their current states.
        /// </summary>
        public async Task RefreshAllHealthBarsAsync()
        {
            await Task.Run(() =>
            {
                CallDeferred("RefreshAllHealthBarsDeferred");
            });
        }

        /// <summary>
        /// Deferred method to refresh all health bars on main thread.
        /// </summary>
        private void RefreshAllHealthBarsDeferred()
        {
            try
            {
                foreach (var (actorId, healthBarNode) in _healthBars)
                {
                    healthBarNode.RefreshDisplay();
                }
                
                _logger?.Debug("Refreshed all health bar displays");
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error refreshing all health bars");
            }
        }

        /// <summary>
        /// Creates a health bar node for an actor.
        /// </summary>
        private HealthBarNode CreateHealthBarNode(ActorId actorId, Position position, Health health)
        {
            var pixelPosition = GridPositionToPixel(position);
            // Center the health bar on the tile (add half tile size to X and Y)
            pixelPosition.X += TileSize / 2;
            pixelPosition.Y += TileSize / 2;
            
            return new HealthBarNode(
                actorId, 
                health, 
                pixelPosition,
                HealthBarWidth,
                HealthBarHeight,
                HealthBarOffsetY,
                HealthGreenColor,
                HealthYellowColor,
                HealthRedColor,
                HealthBarBgColor
            );
        }

        /// <summary>
        /// Creates a feedback label for health changes.
        /// </summary>
        private Label CreateFeedbackLabel(HealthFeedbackType feedbackType, int amount)
        {
            var label = new Label();
            
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
        /// Animates feedback text (fade up and out).
        /// </summary>
        private void AnimateFeedbackText(Label label)
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
        /// Converts grid position to pixel coordinates.
        /// </summary>
        private Vector2 GridPositionToPixel(Position gridPosition)
        {
            return new Vector2(gridPosition.X * TileSize, gridPosition.Y * TileSize);
        }
    }

    /// <summary>
    /// Custom Godot node representing a health bar for an actor.
    /// Combines progress bar functionality with highlighting and visual effects.
    /// </summary>
    public partial class HealthBarNode : Node2D
    {
        private readonly ActorId _actorId;
        private Health _health;
        private readonly int _width;
        private readonly int _height;
        private readonly int _offsetY;
        private readonly Color _greenColor;
        private readonly Color _yellowColor;
        private readonly Color _redColor;
        private readonly Color _bgColor;
        
        private ColorRect? _backgroundRect;
        private ColorRect? _healthRect;
        private Label? _healthText;
        private HealthHighlightType? _currentHighlight;
        private bool _initialized = false;

        public HealthBarNode(ActorId actorId, Health health, Vector2 position, int width, int height, int offsetY,
            Color greenColor, Color yellowColor, Color redColor, Color bgColor)
        {
            _actorId = actorId;
            _health = health;
            _width = width;
            _height = height;
            _offsetY = offsetY;
            _greenColor = greenColor;
            _yellowColor = yellowColor;
            _redColor = redColor;
            _bgColor = bgColor;
            
            Position = position;
            // Don't create elements here - wait for _Ready()
        }

        public override void _Ready()
        {
            base._Ready();
            // Create elements when node is actually in the scene tree
            if (!_initialized)
            {
                CreateHealthBarElements();
                _initialized = true;
            }
        }

        /// <summary>
        /// Creates the visual elements of the health bar.
        /// </summary>
        private void CreateHealthBarElements()
        {
            // Background rectangle (dark background for contrast)
            _backgroundRect = new ColorRect();
            _backgroundRect.Size = new Vector2(_width + 2, _height + 2); // Add border
            _backgroundRect.Position = new Vector2(-(_width / 2) - 1, _offsetY - 1);
            _backgroundRect.Color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Almost black with slight transparency
            AddChild(_backgroundRect);

            // Health rectangle (the actual health bar)
            _healthRect = new ColorRect();
            _healthRect.Position = new Vector2(-_width / 2, _offsetY);
            _healthRect.Size = new Vector2(_width, _height); // Will be adjusted by UpdateHealthDisplay
            _healthRect.Color = GetHealthColor(_health.HealthPercentage);
            AddChild(_healthRect);

            // Health text label (shows current/max)
            _healthText = new Label();
            _healthText.Text = $"{_health.Current}/{_health.Maximum}";
            _healthText.AddThemeFontSizeOverride("font_size", 8); // Small font
            _healthText.Position = new Vector2(-_width / 2, _offsetY - 14); // Above the bar
            _healthText.Modulate = Colors.White;
            // Center the text
            _healthText.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
            AddChild(_healthText);

            UpdateHealthDisplay();
        }

        /// <summary>
        /// Updates the health bar display with current health values.
        /// </summary>
        public void UpdateHealth(Health newHealth)
        {
            _health = newHealth;
            UpdateHealthDisplay();
        }

        /// <summary>
        /// Updates the visual display of the health bar.
        /// </summary>
        private void UpdateHealthDisplay()
        {
            if (_healthRect == null) return;

            var healthPercent = _health.HealthPercentage;
            var healthWidth = Math.Max(0, (float)(_width * healthPercent)); // Ensure non-negative
            
            _healthRect.Size = new Vector2(healthWidth, _height);
            _healthRect.Color = GetHealthColor(healthPercent);
            
            // Update the text display
            if (_healthText != null)
            {
                _healthText.Text = $"{_health.Current}/{_health.Maximum}";
                
                // Change text color based on health level
                if (healthPercent <= 0.25)
                {
                    _healthText.Modulate = new Color(1.0f, 0.4f, 0.4f); // Light red for critical
                }
                else if (healthPercent <= 0.6)
                {
                    _healthText.Modulate = new Color(1.0f, 1.0f, 0.6f); // Light yellow for moderate
                }
                else
                {
                    _healthText.Modulate = Colors.White; // White for healthy
                }
            }
        }

        /// <summary>
        /// Gets the appropriate color for the current health percentage.
        /// </summary>
        private Color GetHealthColor(double healthPercent)
        {
            return healthPercent switch
            {
                > 0.6 => _greenColor,
                > 0.25 => _yellowColor,
                _ => _redColor
            };
        }

        /// <summary>
        /// Sets a highlight effect on the health bar.
        /// </summary>
        public void SetHighlight(HealthHighlightType highlightType)
        {
            _currentHighlight = highlightType;
            
            // Add visual highlight effect based on type
            switch (highlightType)
            {
                case HealthHighlightType.Critical:
                    // Pulsing red effect for critical health
                    var criticalTween = CreateTween();
                    criticalTween.SetLoops();
                    criticalTween.TweenProperty(this, "modulate", Colors.Red, 0.5f);
                    criticalTween.TweenProperty(this, "modulate", Colors.White, 0.5f);
                    break;
                    
                case HealthHighlightType.HealTarget:
                    Modulate = Colors.LightGreen;
                    break;
                    
                case HealthHighlightType.DamageTarget:
                    Modulate = Colors.LightCoral;
                    break;
                    
                case HealthHighlightType.Selected:
                    Modulate = Colors.Yellow;
                    break;
            }
        }

        /// <summary>
        /// Clears any highlight effects.
        /// </summary>
        public void ClearHighlight()
        {
            _currentHighlight = null;
            Modulate = Colors.White;
            
            // Stop any running highlight tweens - Godot 4.4 approach
            // Note: In Godot 4.4, tweens are automatically cleaned up when nodes are freed
        }

        /// <summary>
        /// Refreshes the display (useful for initialization or state changes).
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateHealthDisplay();
        }
    }
}