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

    [Export] private ProgressBar? _healthBar;
    [Export] private Label? _healthLabel;
    [Export] private Button? _damageButton;
    [Export] private Button? _healButton;

    [Export] private float DamageAmount { get; set; } = 20f;
    [Export] private Color HealthyColor { get; set; } = new(0, 1, 0);      // Green
    [Export] private Color CriticalColor { get; set; } = new(1, 0.5f, 0);  // Orange
    [Export] private Color DeadColor { get; set; } = new(0.5f, 0.5f, 0.5f); // Gray

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

        // Create test actor with health
        _actorId = ActorId.NewId();
        var health = HealthValue.Create(100, 100).Value;
        var component = new HealthComponent(_actorId, health);
        _registry.RegisterComponent(component);

        _logger.LogInformation("HealthBarNode initialized for actor {ActorId}", _actorId);

        // Wire up buttons
        if (_damageButton != null)
            _damageButton.Pressed += OnDamageButtonPressed;

        if (_healButton != null)
            _healButton.Pressed += OnHealButtonPressed;

        // Initialize UI
        UpdateHealthDisplay(100, 100, isDead: false, isCritical: false);
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
        if (_mediator == null)
            return;

        var command = new TakeDamageCommand(_actorId, DamageAmount);
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            _logger?.LogWarning("Damage command failed: {Error}", result.Error);
            GD.PrintErr($"Damage failed: {result.Error}");
        }
    }

    private void OnHealButtonPressed()
    {
        if (_mediator == null || _registry == null)
            return;

        // Direct heal for demo (no HealCommand yet - out of scope for Phase 4)
        var componentResult = _registry.GetComponent(_actorId);
        if (componentResult.HasNoValue)
            return;

        var healResult = componentResult.Value.Heal(DamageAmount);
        if (healResult.IsSuccess)
        {
            // Manually update UI (no event for heal yet)
            var newHealth = healResult.Value;
            UpdateHealthDisplay(newHealth.Current, newHealth.Maximum, false, newHealth.Percentage < 0.25f);
            _logger?.LogInformation("Healed {Amount} HP", DamageAmount);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UI UPDATE (Pure presentation logic)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void UpdateHealthDisplay(float current, float maximum, bool isDead, bool isCritical)
    {
        if (_healthBar == null || _healthLabel == null)
            return;

        // Update progress bar
        _healthBar.Value = current;
        _healthBar.MaxValue = maximum;

        // Update color based on state
        if (isDead)
        {
            _healthBar.Modulate = DeadColor;
            _healthLabel.Text = $"DEAD (0/{maximum:F0})";
        }
        else if (isCritical)
        {
            _healthBar.Modulate = CriticalColor;
            _healthLabel.Text = $"CRITICAL! {current:F0}/{maximum:F0}";
        }
        else
        {
            _healthBar.Modulate = HealthyColor;
            _healthLabel.Text = $"Health: {current:F0}/{maximum:F0}";
        }
    }
}