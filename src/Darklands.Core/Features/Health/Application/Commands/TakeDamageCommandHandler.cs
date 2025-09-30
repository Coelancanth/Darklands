using CSharpFunctionalExtensions;
using Darklands.Core.Domain.Common;
using Darklands.Core.Features.Health.Application.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using HealthValue = Darklands.Core.Domain.Common.Health;

namespace Darklands.Core.Features.Health.Application.Commands;

/// <summary>
/// Handler for TakeDamageCommand.
/// Orchestrates damage application and publishes HealthChangedEvent.
/// Follows ADR-004 Rule 1: Commands orchestrate, events notify.
/// </summary>
public sealed class TakeDamageCommandHandler
    : IRequestHandler<TakeDamageCommand, Result<DamageResult>>
{
    private readonly IHealthComponentRegistry _registry;
    private readonly IMediator _mediator;
    private readonly ILogger<TakeDamageCommandHandler> _logger;

    public TakeDamageCommandHandler(
        IHealthComponentRegistry registry,
        IMediator mediator,
        ILogger<TakeDamageCommandHandler> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<Result<DamageResult>> Handle(
        TakeDamageCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Applying {Amount} damage to actor {ActorId}",
            command.Amount,
            command.TargetId);

        // Railway-oriented composition: Get component → Apply damage → Create result → Publish event
        var result = _registry.GetComponent(command.TargetId)
            .ToResult($"Actor {command.TargetId} not found")
            .Bind(component => ApplyDamageToComponent(component, command.Amount))
            .Tap(r => PublishHealthChangedEvent(r, cancellationToken));

        return Task.FromResult(result);
    }

    private Result<DamageResult> ApplyDamageToComponent(
        Domain.IHealthComponent component,
        float amount)
    {
        var oldHealth = component.CurrentHealth;

        return component.TakeDamage(amount)
            .Map(newHealth => CreateDamageResult(
                component.OwnerId,
                oldHealth,
                newHealth,
                amount));
    }

    private static DamageResult CreateDamageResult(
        ActorId actorId,
        HealthValue oldHealth,
        HealthValue newHealth,
        float damageApplied)
    {
        return new DamageResult(
            actorId,
            oldHealth.Current,
            newHealth.Current,
            damageApplied,
            isDead: newHealth.IsDepleted,
            isCritical: newHealth.Percentage < 0.25f);
    }

    private void PublishHealthChangedEvent(
        DamageResult result,
        CancellationToken cancellationToken)
    {
        // ADR-004 Rule 1: Publish event at END with ALL information
        // Subscribers are terminal (Rule 3) - they just display/log, no cascading
        var healthChangedEvent = new HealthChangedEvent(
            result.ActorId,
            result.OldHealth,
            result.NewHealth,
            result.IsDead,
            result.IsCritical);

        _logger.LogDebug(
            "Publishing HealthChangedEvent for actor {ActorId}: {Old} -> {New}",
            result.ActorId,
            result.OldHealth,
            result.NewHealth);

        // Fire and forget - don't await
        // Event handlers are terminal subscribers (UI, logs, sound)
        _ = _mediator.Publish(healthChangedEvent, cancellationToken);
    }
}
