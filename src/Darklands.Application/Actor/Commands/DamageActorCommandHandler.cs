using LanguageExt;
using LanguageExt.Common;
using MediatR;
using Darklands.Application.Common;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Domain.Combat.Services;
using static LanguageExt.Prelude;

namespace Darklands.Application.Actor.Commands
{
    /// <summary>
    /// Handler for DamageActorCommand - Implements functional CQRS pattern with Fin&lt;T&gt; monads.
    /// Refactored to use IDamageService to eliminate MediatR anti-pattern.
    /// Following TDD+VSA Comprehensive Development Workflow.
    /// </summary>
    public class DamageActorCommandHandler : IRequestHandler<DamageActorCommand, Fin<LanguageExt.Unit>>
    {
        private readonly IDamageService _damageService;
        private readonly ICategoryLogger _logger;

        public DamageActorCommandHandler(
            IDamageService damageService,
            ICategoryLogger logger)
        {
            _damageService = damageService;
            _logger = logger;
        }

        public Task<Fin<LanguageExt.Unit>> Handle(DamageActorCommand request, CancellationToken cancellationToken)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Command, "Processing DamageActorCommand for ActorId: {ActorId}, Damage: {Damage}, Source: {Source}",
                request.ActorId, request.Damage, request.Source ?? "Unknown");

            // Apply damage using the domain service (all validation and logging is handled there)
            var damageResult = _damageService.ApplyDamage(request.ActorId, request.Damage, request.Source ?? "Unknown");

            return Task.FromResult(damageResult.Match(
                Succ: _ => FinSucc(LanguageExt.Unit.Default),
                Fail: error => FinFail<LanguageExt.Unit>(error)
            ));
        }
    }
}
