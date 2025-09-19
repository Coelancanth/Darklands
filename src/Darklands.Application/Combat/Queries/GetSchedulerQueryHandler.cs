using System.Collections.Generic;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Application.Combat.Services;
using Darklands.Domain.Combat;
using static LanguageExt.Prelude;

namespace Darklands.Application.Combat.Queries
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
        private readonly ICategoryLogger _logger;

        public GetSchedulerQueryHandler(
            ICombatSchedulerService combatSchedulerService,
            ICategoryLogger logger)
        {
            _combatSchedulerService = combatSchedulerService;
            _logger = logger;
        }

        public Task<Fin<IReadOnlyList<ISchedulable>>> Handle(GetSchedulerQuery request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Combat, "Processing GetSchedulerQuery to retrieve current turn order");

            var result = _combatSchedulerService.GetTurnOrder();

            return result.Match(
                Succ: turnOrder =>
                {
                    _logger.Log(LogLevel.Debug, LogCategory.Combat, "Retrieved turn order with {Count} scheduled entities", turnOrder.Count);
                    return Task.FromResult(FinSucc(turnOrder));
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Error, LogCategory.Combat, "Failed to retrieve turn order: {Error}", error.Message);
                    return Task.FromResult(FinFail<IReadOnlyList<ISchedulable>>(error));
                }
            );
        }
    }
}
