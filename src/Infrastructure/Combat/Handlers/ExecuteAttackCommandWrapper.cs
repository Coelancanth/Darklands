using LanguageExt;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Commands;
using Unit = LanguageExt.Unit;

namespace Darklands.Core.Infrastructure.Combat.Handlers;

/// <summary>
/// Thin wrapper handler for ExecuteAttackCommand that delegates to CombatSwitchAdapter.
/// This is the ONLY handler that MediatR will discover for this command type.
/// </summary>
public sealed class ExecuteAttackCommandWrapper : IRequestHandler<ExecuteAttackCommand, Fin<Unit>>
{
    private readonly CombatSwitchAdapter _switchAdapter;

    public ExecuteAttackCommandWrapper(CombatSwitchAdapter switchAdapter)
    {
        _switchAdapter = switchAdapter;
    }

    public async Task<Fin<Unit>> Handle(ExecuteAttackCommand request, CancellationToken cancellationToken)
    {
        // Simply delegate to the switch adapter
        return await _switchAdapter.RouteAttackCommand(request, cancellationToken);
    }
}
