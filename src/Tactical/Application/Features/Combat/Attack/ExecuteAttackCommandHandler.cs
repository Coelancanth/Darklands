using Darklands.Tactical.Application.Features.Combat.Services;
using Darklands.Tactical.Domain.Aggregates.Actors;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
// using Microsoft.Extensions.Logging; // TODO: Re-enable after logging package issues resolved
using static LanguageExt.Prelude;

namespace Darklands.Tactical.Application.Features.Combat.Attack;

/// <summary>
/// Handles the execution of attack commands in the tactical combat system.
/// 
/// TD_043: No longer implements IRequestHandler to avoid MediatR auto-discovery.
/// Called directly by CombatSwitchAdapter when using tactical implementation.
/// </summary>
public sealed class ExecuteAttackCommandHandler
{
    private readonly IActorRepository _actorRepository;
    private readonly IMediator _mediator;
    // private readonly ILogger<ExecuteAttackCommandHandler> _logger; // TODO: Re-enable after logging package issues resolved

    public ExecuteAttackCommandHandler(
        IActorRepository actorRepository,
        IMediator mediator)
        // ILogger<ExecuteAttackCommandHandler> logger) // TODO: Re-enable after logging package issues resolved
    {
        _actorRepository = actorRepository;
        _mediator = mediator;
        // _logger = logger; // TODO: Re-enable after logging package issues resolved
    }

    public async Task<Fin<AttackResult>> Handle(ExecuteAttackCommand command, CancellationToken cancellationToken)
    {
        // _logger.LogDebug("Executing attack command: {AttackerId} -> {TargetId} for {Damage} damage",
        //     command.AttackerId, command.TargetId, command.BaseDamage); // TODO: Re-enable after logging package issues resolved

        // Retrieve actors
        var getAttacker = await _actorRepository.GetByIdAsync(command.AttackerId);
        var getTarget = await _actorRepository.GetByIdAsync(command.TargetId);

        // Validate both actors exist
        var validation = from attacker in getAttacker
                         from target in getTarget
                         select (attacker, target);

        if (validation.IsFail)
        {
            // _logger.LogWarning("Attack validation failed: one or both actors not found"); // TODO: Re-enable after logging package issues resolved
            return validation.Match<Fin<AttackResult>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: err => FinFail<AttackResult>(err)
            );
        }

        var actors = validation.Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException()
        );

        return await ExecuteAttack(actors.attacker, actors.target, command);
    }

    private async Task<Fin<AttackResult>> ExecuteAttack(
        Actor attacker,
        Actor target,
        ExecuteAttackCommand command)
    {
        // Validate attacker can act
        if (!attacker.CanAct)
        {
            // _logger.LogWarning("Attack failed: {AttackerName} cannot act", attacker.Name); // TODO: Re-enable after logging package issues resolved
            return FinFail<AttackResult>(Error.New($"Attacker {attacker.Name} cannot act"));
        }

        // Apply damage to target
        var damageResult = target.TakeDamage(command.BaseDamage, command.OccurredAt);

        if (damageResult.IsFail)
        {
            // _logger.LogWarning("Damage application failed for target {TargetName}", target.Name); // TODO: Re-enable after logging package issues resolved
            return damageResult.Match<Fin<AttackResult>>(
                Succ: _ => throw new InvalidOperationException(),
                Fail: err => FinFail<AttackResult>(err)
            );
        }

        var actualDamage = damageResult.Match(
            Succ: x => x,
            Fail: _ => throw new InvalidOperationException()
        );

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
            // _logger.LogInformation("Actor {ActorName} was killed in combat", target.Name); // TODO: Re-enable after logging package issues resolved
        }
        
        // _logger.LogInformation("Attack executed successfully: {AttackerName} dealt {Damage} damage to {TargetName}",
        //     attacker.Name, actualDamage, target.Name); // TODO: Re-enable after logging package issues resolved

        return FinSucc(new AttackResult(
            actualDamage,
            target.Health,
            target.Health == 0,
            effects
        ));
    }

    private async Task PublishDomainEvent(object domainEvent, CancellationToken cancellationToken)
    {
        // _logger.LogDebug("Publishing domain event: {EventType}", domainEvent.GetType().Name); // TODO: Re-enable after logging package issues resolved
        
        // TODO: In Phase 3, add TacticalContractAdapter to convert domain events to contract events
        await Task.CompletedTask;
    }
}
