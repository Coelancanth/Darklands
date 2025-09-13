using LanguageExt;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Commands;
using static LanguageExt.Prelude;

namespace Darklands.Core.Infrastructure.Combat.Handlers;

/// <summary>
/// Thin wrapper handler for ProcessNextTurnCommand that delegates to CombatSwitchAdapter.
/// This is the ONLY handler that MediatR will discover for this command type.
/// </summary>
public sealed class ProcessNextTurnCommandWrapper : IRequestHandler<ProcessNextTurnCommand, Fin<Option<Guid>>>
{
    private readonly CombatSwitchAdapter _switchAdapter;

    public ProcessNextTurnCommandWrapper(CombatSwitchAdapter switchAdapter)
    {
        _switchAdapter = switchAdapter;
    }

    public async Task<Fin<Option<Guid>>> Handle(ProcessNextTurnCommand request, CancellationToken cancellationToken)
    {
        // Simply delegate to the switch adapter
        return await _switchAdapter.RouteTurnCommand(request, cancellationToken);
    }
}
