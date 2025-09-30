using Godot;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Application.Commands;
using Darklands.Core.Features.Health.Application.Events;
using Darklands.Core.Features.Health.Application;
using Darklands.Core.Features.Health.Domain;
using Darklands.Core.Infrastructure.DependencyInjection;
using MediatR;
using Microsoft.Extensions.Logging;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Components;

/// <summary>
/// Godot node that displays actor health using a ProgressBar.
/// Subscribes to HealthChangedEvent to update UI reactively.
///
/// ARCHITECTURE (ADR-002, ADR-004):
/// - Extends EventAwareNode for automatic event subscription/cleanup
/// - Uses ServiceLocator.Get<T>() in _Ready() (Godot constraint)
/// - Terminal subscriber (Rule 3): Only updates UI, no business logic
/// - Receives pre-computed state (IsCritical, IsDead) from handler
/// </summary>
public partial class HealthBarNode : EventAwareNode
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT EDITOR PROPERTIES (set in scene)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    [Export] public ProgressBar? HealthBar { get; set; }
    [Export] public Label? HealthLabel { get; set; }
    [Export] public Button? DamageButton { get; set; }

    [Export] public float DamageAmount { get; set; } = 20f;
    [Export] public Color HealthyColor { get; set; } = new(0, 1, 0);      // Green
    [Export] public Color CriticalColor { get; set; } = new(1, 0.5f, 0);  // Orange
    [Export] public Color DeadColor { get; set; } = new(0.5f, 0.5f, 0.5f); // Gray

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // DEPENDENCIES (resolved via ServiceLocator in _Ready)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private IMediator? _mediator;
    private IHealthComponentRegistry? _registry;
    private ILogger<HealthBarNode>? _logger;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // STATE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private ActorId _actorId;

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GODOT LIFECYCLE
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public override void _Ready()
    {
        // CRITICAL: Call base._Ready() first to initialize EventBus
        base._Ready();

        GD.Print("[HealthBarNode] _Ready() started");

        // GODOT 4 FIX: NodePath exports don't auto-populate C# properties
        // Must manually resolve using GetNode()
        HealthBar = GetNode<ProgressBar>("HealthBar");
        HealthLabel = GetNode<Label>("HealthLabel");
        DamageButton = GetNode<Button>("ButtonContainer/DamageButton");

        GD.Print($"[HealthBarNode] Nodes resolved - HealthBar: {HealthBar != null}, DamageButton: {DamageButton != null}");

        // Resolve dependencies via ServiceLocator (Godot constraint)
        var mediatorResult = ServiceLocator.GetService<IMediator>();
        var registryResult = ServiceLocator.GetService<IHealthComponentRegistry>();
        var loggerResult = ServiceLocator.GetService<ILogger<HealthBarNode>>();

        if (mediatorResult.IsFailure || registryResult.IsFailure || loggerResult.IsFailure)
        {
            GD.PrintErr("[HealthBarNode] Failed to resolve dependencies");
            return;
        }

        _mediator = mediatorResult.Value;
        _registry = registryResult.Value;
        _logger = loggerResult.Value;

        GD.Print("[HealthBarNode] Dependencies resolved successfully");

        // Create test actor with health
        _actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(_actorId, health);
        _registry.RegisterComponent(component);

        _logger.LogInformation("HealthBarNode initialized for actor {ActorId}", _actorId);

        // NOTE: DamageButton signal already connected in scene file (HealthTestScene.tscn line 76)
        // Do NOT wire up here again - causes double button press!
        // REMOVED: DamageButton.Pressed += OnDamageButtonPressed;

        // Initialize UI
        UpdateHealthDisplay(100, 100, isDead: false, isCritical: false);
        GD.Print("[HealthBarNode] _Ready() completed");
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // EVENT SUBSCRIPTION (EventAwareNode pattern)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    protected override void SubscribeToEvents()
    {
        // Subscribe to HealthChangedEvent (published by TakeDamageCommandHandler)
        EventBus?.Subscribe<HealthChangedEvent>(this, OnHealthChanged);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // EVENT HANDLERS (Terminal subscribers - Rule 3)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void OnHealthChanged(HealthChangedEvent evt)
    {
        // Filter: Only react to OUR actor's events
        if (evt.ActorId != _actorId)
            return;

        _logger?.LogDebug(
            "Health changed: {Old} -> {New} (Dead: {IsDead}, Critical: {IsCritical})",
            evt.OldHealth,
            evt.NewHealth,
            evt.IsDead,
            evt.IsCritical);

        // Terminal subscriber: Just update UI, no business logic
        UpdateHealthDisplay(evt.NewHealth, 100, evt.IsDead, evt.IsCritical);
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // BUTTON HANDLERS (Send commands to Core)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private async void OnDamageButtonPressed()
    {
        GD.Print($"[HealthBarNode] 🔥 DamageButton PRESSED! Mediator null? {_mediator == null}");

        if (_mediator == null)
        {
            GD.PrintErr("[HealthBarNode] Cannot send command - mediator is null");
            return;
        }

        GD.Print($"[HealthBarNode] Sending TakeDamageCommand for actor {_actorId}");
        var command = new TakeDamageCommand(_actorId, DamageAmount);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger?.LogWarning("Damage command failed: {Error}", result.Error);
            GD.PrintErr($"Damage failed: {result.Error}");
        }
        else
        {
            GD.Print("[HealthBarNode] ✅ Damage command succeeded!");
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UI UPDATE (Pure presentation logic)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void UpdateHealthDisplay(float current, float maximum, bool isDead, bool isCritical)
    {
        if (HealthBar == null || HealthLabel == null)
            return;

        GD.Print($"[HealthBarNode] Updating display: {current}/{maximum}, Dead: {isDead}, Critical: {isCritical}");

        // Update progress bar
        HealthBar.Value = current;
        HealthBar.MaxValue = maximum;

        // GODOT FIX: Use theme style overrides to color the fill, not Modulate
        // Modulate tints the entire node (including background), making it invisible
        var styleBox = new StyleBoxFlat();

        // Update color and text based on state
        if (isDead)
        {
            styleBox.BgColor = DeadColor;
            HealthLabel.Text = $"DEAD (0/{maximum:F0})";
        }
        else if (isCritical)
        {
            styleBox.BgColor = CriticalColor;
            HealthLabel.Text = $"CRITICAL! {current:F0}/{maximum:F0}";
        }
        else
        {
            styleBox.BgColor = HealthyColor;
            HealthLabel.Text = $"Health: {current:F0}/{maximum:F0}";
        }

        // Apply the styled fill to the progress bar
        HealthBar.AddThemeStyleboxOverride("fill", styleBox);

        GD.Print($"[HealthBarNode] Display updated - Bar value: {HealthBar.Value}/{HealthBar.MaxValue}");
    }
}