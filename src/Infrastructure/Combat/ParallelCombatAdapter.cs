using LanguageExt;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Domain.Combat;
using Darklands.Core.Domain.Grid;
using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Infrastructure.Adapters;
using Unit = LanguageExt.Unit;

namespace Darklands.Core.Infrastructure.Combat;

/// <summary>
/// Parallel operation adapter for TD_043 Strangler Fig validation.
/// Sends commands to BOTH legacy and new Tactical systems simultaneously,
/// allowing comparison of results and behavior.
/// </summary>
public sealed class ParallelCombatAdapter :
    IRequestHandler<ExecuteAttackCommand, Fin<Unit>>,
    IRequestHandler<ProcessNextTurnCommand, Fin<Unit>>
{
    private readonly IRequestHandler<ExecuteAttackCommand, Fin<Unit>> _legacyAttackHandler;
    private readonly IRequestHandler<ProcessNextTurnCommand, Fin<Unit>> _legacyTurnHandler;
    private readonly TacticalContractAdapter _tacticalAdapter;
    private readonly ILogger<ParallelCombatAdapter> _logger;
    private readonly bool _validateResults;

    public ParallelCombatAdapter(
        IRequestHandler<ExecuteAttackCommand, Fin<Unit>> legacyAttackHandler,
        IRequestHandler<ProcessNextTurnCommand, Fin<Unit>> legacyTurnHandler,
        TacticalContractAdapter tacticalAdapter,
        ILogger<ParallelCombatAdapter> logger,
        bool validateResults = true)
    {
        _legacyAttackHandler = legacyAttackHandler;
        _legacyTurnHandler = legacyTurnHandler;
        _tacticalAdapter = tacticalAdapter;
        _logger = logger;
        _validateResults = validateResults;
    }

    public async Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PARALLEL] Starting parallel attack execution");

        // Execute legacy system (primary - controls actual game state)
        var legacyTask = _legacyAttackHandler.Handle(request, cancellationToken);

        // Execute new Tactical system (shadow - for validation only)
        var tacticalTask = ExecuteTacticalAttackAsync(request, cancellationToken);

        // Wait for both to complete
        var results = await Task.WhenAll(legacyTask, tacticalTask);

        var legacyResult = results[0];
        var tacticalResult = results[1];

        // Compare results if validation enabled
        if (_validateResults)
        {
            ValidateAttackResults(legacyResult, tacticalResult, request);
        }

        // Return legacy result (it's the source of truth for now)
        return legacyResult;
    }

    public async Task<Fin<Unit>> Handle(ProcessNextTurnCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[PARALLEL] Starting parallel turn processing");

        // Execute legacy system (primary)
        var legacyTask = _legacyTurnHandler.Handle(request, cancellationToken);

        // Execute new Tactical system (shadow)
        var tacticalTask = ExecuteTacticalTurnAsync(request, cancellationToken);

        // Wait for both
        var results = await Task.WhenAll(legacyTask, tacticalTask);

        var legacyResult = results[0];
        var tacticalResult = results[1];

        // Compare results if validation enabled
        if (_validateResults)
        {
            ValidateTurnResults(legacyResult, tacticalResult, request);
        }

        // Return legacy result
        return legacyResult;
    }

    private async Task<Fin<Unit>> ExecuteTacticalAttackAsync(
        ExecuteAttackCommand legacyCommand,
        CancellationToken cancellationToken)
    {
        try
        {
            // Map legacy command to Tactical command
            var tacticalCommand = new Tactical.Application.Features.Combat.Attack.ExecuteAttackCommand(
                attackerId: new EntityId(legacyCommand.AttackerId.Value),
                targetId: new EntityId(legacyCommand.TargetId.Value),
                baseDamage: legacyCommand.CombatAction.BaseDamage,
                occurredAt: Tactical.Domain.ValueObjects.TimeUnit.Create(1000).Match(
                    Succ: t => t,
                    Fail: _ => Tactical.Domain.ValueObjects.TimeUnit.Zero
                ),
                attackType: legacyCommand.CombatAction.Name
            );

            // Execute through adapter (which publishes contract events)
            var result = await _tacticalAdapter.ExecuteAttackWithContractAsync(
                tacticalCommand,
                cancellationToken);

            // Map result back to Unit
            return result.Match<Fin<Unit>>(
                Succ: _ => Unit.Default,
                Fail: err => err
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PARALLEL] Tactical attack execution failed");
            // Don't fail the operation - this is shadow mode
            return Unit.Default;
        }
    }

    private async Task<Fin<Unit>> ExecuteTacticalTurnAsync(
        ProcessNextTurnCommand legacyCommand,
        CancellationToken cancellationToken)
    {
        try
        {
            // Map legacy command to Tactical command
            var tacticalCommand = new Tactical.Application.Features.Combat.Scheduling.ProcessNextTurnCommand(
                currentTime: Tactical.Domain.ValueObjects.TimeUnit.Create(legacyCommand.CurrentTime).Match(
                    Succ: t => t,
                    Fail: _ => Tactical.Domain.ValueObjects.TimeUnit.Zero
                ),
                autoExecuteAI: false
            );

            // Execute through adapter
            var result = await _tacticalAdapter.ProcessTurnWithContractAsync(
                tacticalCommand,
                cancellationToken);

            // Map result back to Unit
            return result.Match<Fin<Unit>>(
                Succ: _ => Unit.Default,
                Fail: err => err
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PARALLEL] Tactical turn processing failed");
            return Unit.Default;
        }
    }

    private void ValidateAttackResults(
        Fin<Unit> legacyResult,
        Fin<Unit> tacticalResult,
        ExecuteAttackCommand command)
    {
        var legacySuccess = legacyResult.IsSucc;
        var tacticalSuccess = tacticalResult.IsSucc;

        if (legacySuccess != tacticalSuccess)
        {
            _logger.LogWarning(
                "[VALIDATION] Attack result mismatch! Legacy: {LegacySuccess}, Tactical: {TacticalSuccess}, Command: {AttackerId}->{TargetId}",
                legacySuccess, tacticalSuccess, command.AttackerId, command.TargetId);
        }
        else
        {
            _logger.LogDebug(
                "[VALIDATION] Attack results match. Both succeeded: {Success}",
                legacySuccess);
        }
    }

    private void ValidateTurnResults(
        Fin<Unit> legacyResult,
        Fin<Unit> tacticalResult,
        ProcessNextTurnCommand command)
    {
        var legacySuccess = legacyResult.IsSucc;
        var tacticalSuccess = tacticalResult.IsSucc;

        if (legacySuccess != tacticalSuccess)
        {
            _logger.LogWarning(
                "[VALIDATION] Turn result mismatch! Legacy: {LegacySuccess}, Tactical: {TacticalSuccess}, Time: {CurrentTime}",
                legacySuccess, tacticalSuccess, command.CurrentTime);
        }
        else
        {
            _logger.LogDebug(
                "[VALIDATION] Turn results match. Both succeeded: {Success}",
                legacySuccess);
        }
    }
}
