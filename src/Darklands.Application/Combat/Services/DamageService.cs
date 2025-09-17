using LanguageExt;
using LanguageExt.Common;
using Darklands.Domain.Actor;
using Darklands.Domain.Grid;
using Darklands.Domain.Combat.Services;
using Darklands.Application.Actor.Services;
using Darklands.Application.Common;
using static LanguageExt.Prelude;

namespace Darklands.Application.Combat.Services
{
    /// <summary>
    /// Application service implementing IDamageService for combat damage operations.
    /// Consolidates damage logic from ExecuteAttackCommandHandler and DamageActorCommandHandler,
    /// eliminating MediatR anti-pattern while providing rich logging and validation.
    /// </summary>
    public class DamageService : IDamageService
    {
        private readonly IActorStateService _actorStateService;
        private readonly ICategoryLogger _logger;

        public DamageService(
            IActorStateService actorStateService,
            ICategoryLogger logger)
        {
            _actorStateService = actorStateService;
            _logger = logger;
        }

        public Fin<Darklands.Domain.Actor.Actor> ApplyDamage(ActorId actorId, int damage, string source)
        {
            _logger.Log(LogLevel.Debug, LogCategory.Combat, "Applying {Damage} damage to {ActorId} from {Source}",
                damage, actorId, source ?? "Unknown");

            // Get actor current state for before/after logging
            var actorBeforeOption = _actorStateService.GetActor(actorId);
            var hpBefore = actorBeforeOption.Match(
                Some: actor => actor.Health.Current,
                None: () => 0
            );

            // Apply damage using existing actor state service
            var damageResult = _actorStateService.DamageActor(actorId, damage);

            // Enhanced logging with HP transitions and status
            return damageResult.Match(
                Succ: damagedActor =>
                {
                    var hpAfter = damagedActor.Health.Current;
                    var maxHp = damagedActor.Health.Maximum;
                    var actualDamage = hpBefore - hpAfter;

                    if (damagedActor.IsAlive)
                    {
                        _logger.Log(LogLevel.Information, LogCategory.Combat, "{ActorId} health: {HPBefore} → {HPAfter} ({ActualDamage} damage from {Source}, {HPAfter}/{MaxHP} remaining)",
                            actorId, hpBefore, hpAfter, actualDamage, source ?? "Unknown", hpAfter, maxHp);
                    }
                    else
                    {
                        _logger.Log(LogLevel.Information, LogCategory.Combat, "{ActorId} defeated: {HPBefore} → 0 HP ({ActualDamage} damage from {Source}, DEAD)",
                            actorId, hpBefore, actualDamage, source ?? "Unknown");
                    }

                    return FinSucc(damagedActor);
                },
                Fail: error =>
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Combat, "Failed to damage {ActorId} from {Source}: {Error}",
                        actorId, source ?? "Unknown", error.Message);
                    return FinFail<Darklands.Domain.Actor.Actor>(error);
                }
            );
        }
    }
}
