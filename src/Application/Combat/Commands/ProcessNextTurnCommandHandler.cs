using System;
using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Services;
using static LanguageExt.Prelude;

namespace Darklands.Core.Application.Combat.Commands
{
    /// <summary>
    /// Handler for ProcessNextTurnCommand - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Processes the next turn by removing and returning the next scheduled actor.
    /// 
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class ProcessNextTurnCommandHandler : IRequestHandler<ProcessNextTurnCommand, Fin<Option<Guid>>>
    {
        private readonly ICombatSchedulerService _combatSchedulerService;
        private readonly ILogger _logger;

        public ProcessNextTurnCommandHandler(
            ICombatSchedulerService combatSchedulerService,
            ILogger logger)
        {
            _combatSchedulerService = combatSchedulerService;
            _logger = logger;
        }

        public Task<Fin<Option<Guid>>> Handle(ProcessNextTurnCommand request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing ProcessNextTurnCommand to get next scheduled actor");

            var result = _combatSchedulerService.ProcessNextTurn();

            return result.Match(
                Succ: actorOption =>
                {
                    actorOption.Match(
                        Some: actorId => _logger?.Debug("Next turn processed: Actor {ActorId} is up", actorId),
                        None: () => _logger?.Debug("Next turn processed: No actors scheduled")
                    );
                    return Task.FromResult(FinSucc(actorOption));
                },
                Fail: error =>
                {
                    _logger?.Error("Failed to process next turn: {Error}", error.Message);
                    return Task.FromResult(FinFail<Option<Guid>>(error));
                }
            );
        }
    }
}
