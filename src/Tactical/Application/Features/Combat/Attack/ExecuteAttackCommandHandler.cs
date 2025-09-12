using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.Aggregates.Actors;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Application.Features.Combat.Attack;

/// <summary>
/// Handles the execution of attack commands in the tactical combat system.
/// </summary>
public sealed class ExecuteAttackCommandHandler : IRequestHandler<ExecuteAttackCommand, Fin<AttackResult>>
{
    private readonly IActorRepository _actorRepository;
    private readonly ILogger<ExecuteAttackCommandHandler> _logger;
    private readonly IMediator _mediator;

    public ExecuteAttackCommandHandler(
        IActorRepository actorRepository,
        ILogger<ExecuteAttackCommandHandler> logger,
        IMediator mediator)
    {
        _actorRepository = actorRepository;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Fin<AttackResult>> Handle(ExecuteAttackCommand command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing attack from {AttackerId} to {TargetId} with base damage {BaseDamage}",
            command.AttackerId, command.TargetId, command.BaseDamage);

        // Retrieve actors
        var getAttacker = await _actorRepository.GetByIdAsync(command.AttackerId);
        var getTarget = await _actorRepository.GetByIdAsync(command.TargetId);

        // Validate both actors exist
        var validation = from attacker in getAttacker
                         from target in getTarget
                         select (attacker, target);

        return await validation.Match(
            Succ: async actors => await ExecuteAttack(actors.attacker, actors.target, command),
            Fail: error => FinFail<AttackResult>(error)
        );
    }

    private async Task<Fin<AttackResult>> ExecuteAttack(
        Actor attacker,
        Actor target,
        ExecuteAttackCommand command)
    {
        // Validate attacker can act
        if (!attacker.CanAct)
        {
            return FinFail<AttackResult>(Error.New($"Attacker {attacker.Name} cannot act"));
        }

        // Apply damage to target
        var damageResult = target.TakeDamage(command.BaseDamage, command.OccurredAt);

        return await damageResult.Match(
            Succ: async actualDamage =>
            {
                // Save the updated target state
                await _actorRepository.UpdateAsync(target);

                // Publish domain events as integration events
                foreach (var domainEvent in target.DomainEvents)
                {
                    await PublishDomainEvent(domainEvent, cancellationToken: default);
                }

                // Clear domain events after publishing
                target.ClearDomainEvents();

                var effects = new List<string>();
                if (target.Health == 0)
                {
                    effects.Add($"{target.Name} was killed!");
                }

                _logger.LogInformation(
                    "Attack executed: {AttackerName} dealt {Damage} damage to {TargetName} (HP: {RemainingHealth}/{MaxHealth})",
                    attacker.Name, actualDamage, target.Name, target.Health, target.MaxHealth);

                return FinSucc(new AttackResult(
                    actualDamage,
                    target.Health,
                    target.Health == 0,
                    effects
                ));
            },
            Fail: error =>
            {
                _logger.LogWarning("Attack failed: {Error}", error.Message);
                return FinFail<AttackResult>(error);
            }
        );
    }

    private async Task PublishDomainEvent(object domainEvent, CancellationToken cancellationToken)
    {
        // This will be enhanced in Phase 3 to publish contract events
        // For now, just log the domain event
        _logger.LogDebug("Domain event occurred: {EventType}", domainEvent.GetType().Name);

        // TODO: In Phase 3, add TacticalContractAdapter to convert domain events to contract events
        await Task.CompletedTask;
    }
}
