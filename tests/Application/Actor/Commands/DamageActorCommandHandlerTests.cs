using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Actor.Commands;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Tests.TestUtilities;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Actor.Commands
{
    [Trait("Category", "Phase2")]
    public class DamageActorCommandHandlerTests
    {
        // Test stub for IActorStateService - minimal implementation for testing
        private class TestActorStateService : IActorStateService
        {
            private readonly bool _actorExists;
            private readonly Darklands.Core.Domain.Actor.Actor? _actor;
            private readonly bool _damageSucceeds;

            public TestActorStateService(bool actorExists = true, Darklands.Core.Domain.Actor.Actor? actor = null, bool damageSucceeds = true)
            {
                _actorExists = actorExists;
                _actor = actor;
                _damageSucceeds = damageSucceeds;
            }

            public Fin<Unit> AddActor(Darklands.Core.Domain.Actor.Actor actor)
                => FinSucc(unit);

            public Option<Darklands.Core.Domain.Actor.Actor> GetActor(ActorId actorId)
                => _actorExists && _actor != null ? Some(_actor) : None;

            public Fin<Darklands.Core.Domain.Actor.Actor> DamageActor(ActorId actorId, int damage)
            {
                if (!_damageSucceeds)
                    return FinFail<Darklands.Core.Domain.Actor.Actor>(Error.New("DAMAGE_FAILED: Simulated failure"));

                if (_actor == null)
                    return FinFail<Darklands.Core.Domain.Actor.Actor>(Error.New("ACTOR_NOT_FOUND: Actor not found"));

                var result = _actor.TakeDamage(damage);
                return result.Match(
                    Succ: damagedActor => FinSucc(damagedActor),
                    Fail: error => FinFail<Darklands.Core.Domain.Actor.Actor>(error)
                );
            }

            // Other interface methods - not used in damage tests
            public Fin<LanguageExt.Unit> UpdateActorHealth(ActorId actorId, Darklands.Core.Domain.Actor.Health newHealth) => FinSucc(LanguageExt.Unit.Default);
            public Fin<Darklands.Core.Domain.Actor.Actor> HealActor(ActorId actorId, int healAmount) => FinSucc(_actor!);
            public Option<bool> IsActorAlive(ActorId actorId) => Some(true);
            public Fin<LanguageExt.Unit> RemoveDeadActor(ActorId actorId) => FinSucc(LanguageExt.Unit.Default);
        }

        private readonly ActorId _validActorId = ActorId.NewId(TestIdGenerator.Instance);

        [Fact]
        public async Task Handle_ValidDamage_ReturnsSuccess()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 25, "Sword Attack");
            var actorService = new TestActorStateService(actorExists: true, actor: testActor, damageSucceeds: true);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ReturnsError()
        {
            // Arrange
            var command = DamageActorCommand.Create(_validActorId, 25);
            var actorService = new TestActorStateService(actorExists: false);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("ACTOR_NOT_FOUND")
            );
        }

        [Fact]
        public async Task Handle_NegativeDamage_ReturnsError()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, -10);
            var actorService = new TestActorStateService(actorExists: true, actor: testActor);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("INVALID_DAMAGE")
            );
        }

        [Fact]
        public async Task Handle_ServiceDamageFailure_ReturnsError()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 25);
            var actorService = new TestActorStateService(actorExists: true, actor: testActor, damageSucceeds: false);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("DAMAGE_FAILED")
            );
        }

        [Fact]
        public async Task Handle_ZeroDamage_ReturnsSuccess()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 0, "No Damage");
            var actorService = new TestActorStateService(actorExists: true, actor: testActor);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LethalDamage_ReturnsSuccess()
        {
            // Arrange - Create actor with low health
            var lowHealthActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 10, "Weak Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 15, "Death Blow"); // More than current health
            var actorService = new TestActorStateService(actorExists: true, actor: lowHealthActor);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithSource_ProcessesCorrectly()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 30, "Fire Spell");
            var actorService = new TestActorStateService(actorExists: true, actor: testActor);
            var handler = new DamageActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }
    }
}
