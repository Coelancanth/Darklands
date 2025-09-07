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
    /// Handler for ScheduleActorCommand - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Schedules an actor to act at a specific time in the combat timeline.
    /// 
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class ScheduleActorCommandHandler : IRequestHandler<ScheduleActorCommand, Fin<LanguageExt.Unit>>
    {
        private readonly ICombatSchedulerService _combatSchedulerService;
        private readonly ILogger _logger;

        public ScheduleActorCommandHandler(
            ICombatSchedulerService combatSchedulerService,
            ILogger logger)
        {
            _combatSchedulerService = combatSchedulerService;
            _logger = logger;
        }

        public Task<Fin<LanguageExt.Unit>> Handle(ScheduleActorCommand request, CancellationToken cancellationToken)
        {
            _logger?.Debug("Processing ScheduleActorCommand for ActorId: {ActorId} at Position: {Position} NextTurn: {NextTurn}",
                request.ActorId, request.Position, request.NextTurn);

            // Schedule the actor using the combat scheduler service
            var result = _combatSchedulerService.ScheduleActor(request.ActorId, request.Position, request.NextTurn);

            return result.Match(
                Succ: _ =>
                {
                    _logger?.Debug("Successfully scheduled actor {ActorId} for NextTurn: {NextTurn}",
                        request.ActorId, request.NextTurn);
                    return Task.FromResult(FinSucc(LanguageExt.Unit.Default));
                },
                Fail: error =>
                {
                    _logger?.Warning("Failed to schedule actor {ActorId}: {Error}",
                        request.ActorId, error.Message);
                    return Task.FromResult(FinFail<LanguageExt.Unit>(error));
                }
            );
        }
    }
}
