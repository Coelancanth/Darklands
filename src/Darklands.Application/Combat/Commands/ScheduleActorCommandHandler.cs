using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Application.Combat.Services;
using static LanguageExt.Prelude;

namespace Darklands.Application.Combat.Commands
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
        private readonly ICategoryLogger _logger;

        public ScheduleActorCommandHandler(
            ICombatSchedulerService combatSchedulerService,
            ICategoryLogger logger)
        {
            _combatSchedulerService = combatSchedulerService;
            _logger = logger;
        }

        public Task<Fin<LanguageExt.Unit>> Handle(ScheduleActorCommand request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Combat, "Processing ScheduleActorCommand for ActorId: {ActorId} at Position: {Position} NextTurn: {NextTurn}",
                request.ActorId, request.Position, request.NextTurn);

            // Schedule the actor using the combat scheduler service
            var result = _combatSchedulerService.ScheduleActor(request.ActorId, request.Position, request.NextTurn);

            return result.Match(
                Succ: _ =>
                {
                    _logger.Log(LogLevel.Debug, LogCategory.Combat, "Successfully scheduled actor {ActorId} for NextTurn: {NextTurn}",
                        request.ActorId, request.NextTurn);
                    return Task.FromResult(FinSucc(LanguageExt.Unit.Default));
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Combat, "Failed to schedule actor {ActorId}: {Error}",
                        request.ActorId, error.Message);
                    return Task.FromResult(FinFail<LanguageExt.Unit>(error));
                }
            );
        }
    }
}
