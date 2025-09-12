using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Infrastructure.Configuration;
using Darklands.SharedKernel.Domain;
using Unit = LanguageExt.Unit;
using static LanguageExt.Prelude;

namespace Darklands.Core.Infrastructure.Combat;

/// <summary>
/// Switch adapter for TD_043 Strangler Fig migration.
/// Routes commands to EITHER legacy OR new Tactical system based on feature toggle.
/// This avoids ambiguous handler registration while allowing controlled migration.
/// </summary>
public sealed class CombatSwitchAdapter :
    IRequestHandler<ExecuteAttackCommand, Fin<Unit>>,
    IRequestHandler<ProcessNextTurnCommand, Fin<Option<Guid>>>
{
    private readonly IRequestHandler<ExecuteAttackCommand, Fin<Unit>> _legacyAttackHandler;
    private readonly IRequestHandler<ProcessNextTurnCommand, Fin<Option<Guid>>> _legacyTurnHandler;
    private readonly StranglerFigConfiguration _config;
    private readonly ILogger<CombatSwitchAdapter> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CombatSwitchAdapter(
        Application.Combat.Commands.ExecuteAttackCommandHandler legacyAttackHandler,
        Application.Combat.Commands.ProcessNextTurnCommandHandler legacyTurnHandler,
        StranglerFigConfiguration config,
        ILogger<CombatSwitchAdapter> logger,
        IServiceProvider serviceProvider)
    {
        _legacyAttackHandler = legacyAttackHandler;
        _legacyTurnHandler = legacyTurnHandler;
        _config = config;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken cancellationToken)
    {
        if (_config.UseTacticalContext)
        {
            _logger.LogInformation("[SWITCH] Routing attack to NEW Tactical system");
            return await ExecuteTacticalAttackAsync(request, cancellationToken);
        }
        else
        {
            _logger.LogInformation("[SWITCH] Routing attack to LEGACY system");
            return await _legacyAttackHandler.Handle(request, cancellationToken);
        }
    }

    public async Task<Fin<Option<Guid>>> Handle(ProcessNextTurnCommand request, CancellationToken cancellationToken)
    {
        if (_config.UseTacticalContext)
        {
            _logger.LogInformation("[SWITCH] Routing turn processing to NEW Tactical system");
            return await ExecuteTacticalTurnAsync(request, cancellationToken);
        }
        else
        {
            _logger.LogInformation("[SWITCH] Routing turn processing to LEGACY system");
            return await _legacyTurnHandler.Handle(request, cancellationToken);
        }
    }

    private async Task<Fin<Unit>> ExecuteTacticalAttackAsync(
        ExecuteAttackCommand legacyCommand,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the Tactical handler from DI
            var tacticalHandler = _serviceProvider.GetService(typeof(Tactical.Application.Features.Combat.Attack.ExecuteAttackCommandHandler))
                as Tactical.Application.Features.Combat.Attack.ExecuteAttackCommandHandler;

            if (tacticalHandler == null)
            {
                _logger.LogError("[SWITCH] Tactical attack handler not found in DI");
                return Error.New("Tactical attack handler not registered");
            }

            // Map legacy command to Tactical command
            var tacticalCommand = new Tactical.Application.Features.Combat.Attack.ExecuteAttackCommand(
                attackerId: new EntityId(legacyCommand.AttackerId.Value),
                targetId: new EntityId(legacyCommand.TargetId.Value),
                baseDamage: legacyCommand.CombatAction.BaseDamage,
                occurredAt: new Tactical.Domain.ValueObjects.TimeUnit(1000),
                attackType: legacyCommand.CombatAction.Name
            );

            // Execute using Tactical handler
            var result = await tacticalHandler.Handle(tacticalCommand, cancellationToken);

            // Map result back to Unit
            return result.Match<Fin<Unit>>(
                Succ: _ => Unit.Default,
                Fail: err => err
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SWITCH] Tactical attack execution failed");
            return Error.New($"Tactical attack failed: {ex.Message}");
        }
    }

    private async Task<Fin<Option<Guid>>> ExecuteTacticalTurnAsync(
        ProcessNextTurnCommand legacyCommand,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the Tactical handler from DI
            var tacticalHandler = _serviceProvider.GetService(typeof(Tactical.Application.Features.Combat.Scheduling.ProcessNextTurnCommandHandler))
                as Tactical.Application.Features.Combat.Scheduling.ProcessNextTurnCommandHandler;

            if (tacticalHandler == null)
            {
                _logger.LogError("[SWITCH] Tactical turn handler not found in DI");
                return Error.New("Tactical turn handler not registered");
            }

            // Map legacy command to Tactical command
            var tacticalCommand = new Tactical.Application.Features.Combat.Scheduling.ProcessNextTurnCommand(
                currentTime: new Tactical.Domain.ValueObjects.TimeUnit(1000),
                autoExecuteAI: false
            );

            // Execute using Tactical handler
            var result = await tacticalHandler.Handle(tacticalCommand, cancellationToken);

            // Map TurnResult to Option<Guid>
            return result.Match<Fin<Option<Guid>>>(
                Succ: turnResult => Option<Guid>.Some(Guid.NewGuid()), // TODO: Map actual actor ID
                Fail: err => err
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SWITCH] Tactical turn processing failed");
            return Error.New($"Tactical turn processing failed: {ex.Message}");
        }
    }
}
