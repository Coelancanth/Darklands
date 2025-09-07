using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Domain.Combat;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Combat.Queries
{
    /// <summary>
    /// Handler for GetSchedulerQuery - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Returns the current turn order without modifying scheduler state.
    /// 
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class GetSchedulerQueryHandler : IRequestHandler<GetSchedulerQuery, Fin<IReadOnlyList<ISchedulable>>>
    {
        private readonly ICombatSchedulerService _combatSchedulerService;
        private readonly ILogger _logger;

        public GetSchedulerQueryHandler(
            ICombatSchedulerService combatSchedulerService,
            ILogger logger)
        {
            _combatSchedulerService = combatSchedulerService;
            _logger = logger;
        }

        public Task<Fin<IReadOnlyList<ISchedulable>>> Handle(GetSchedulerQuery request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing GetSchedulerQuery to retrieve current turn order");

            var result = _combatSchedulerService.GetTurnOrder();

            return result.Match(
                Succ: turnOrder =>
                {
                    _logger?.Debug("Retrieved turn order with {Count} scheduled entities", turnOrder.Count);
                    return Task.FromResult(FinSucc(turnOrder));
                },
                Fail: error =>
                {
                    _logger?.Error("Failed to retrieve turn order: {Error}", error.Message);
                    return Task.FromResult(FinFail<IReadOnlyList<ISchedulable>>(error));
                }
            );
        }
    }
}
