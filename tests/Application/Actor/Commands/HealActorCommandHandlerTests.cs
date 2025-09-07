using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Actor.Commands;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Domain.Grid;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Actor.Commands
{
    [Trait("Category", "Phase2")]
    public class HealActorCommandHandlerTests
    {
        // Test stub for IActorStateService - minimal implementation for testing
        private class TestActorStateService : IActorStateService
        {
            private readonly bool _actorExists;
            private readonly Darklands.Core.Domain.Actor.Actor? _actor;
            private readonly bool _healSucceeds;

            public TestActorStateService(bool actorExists = true, Darklands.Core.Domain.Actor.Actor? actor = null, bool healSucceeds = true)
            {
                _actorExists = actorExists;
                _actor = actor;
                _healSucceeds = healSucceeds;
            }

            public Fin<Unit> AddActor(Darklands.Core.Domain.Actor.Actor actor)
                => FinSucc(unit);

            public Option<Darklands.Core.Domain.Actor.Actor> GetActor(ActorId actorId)
                => _actorExists && _actor != null ? Some(_actor) : None;

            public Fin<Darklands.Core.Domain.Actor.Actor> HealActor(ActorId actorId, int healAmount)
            {
                if (!_healSucceeds)
                    return FinFail<Darklands.Core.Domain.Actor.Actor>(Error.New("HEAL_FAILED: Simulated failure"));

                if (_actor == null)
                    return FinFail<Darklands.Core.Domain.Actor.Actor>(Error.New("ACTOR_NOT_FOUND: Actor not found"));

                var result = _actor.Heal(healAmount);
                return result.Match(
                    Succ: healedActor => FinSucc(healedActor),
                    Fail: error => FinFail<Darklands.Core.Domain.Actor.Actor>(error)
                );
            }

            // Other interface methods - not used in heal tests
            public Fin<LanguageExt.Unit> UpdateActorHealth(ActorId actorId, Darklands.Core.Domain.Actor.Health newHealth) => FinSucc(LanguageExt.Unit.Default);
            public Fin<Darklands.Core.Domain.Actor.Actor> DamageActor(ActorId actorId, int damage) => FinSucc(_actor!);
            public Option<bool> IsActorAlive(ActorId actorId) => Some(true);
            public Fin<LanguageExt.Unit> RemoveDeadActor(ActorId actorId) => FinSucc(LanguageExt.Unit.Default);
        }

        private readonly ActorId _validActorId = ActorId.NewId();

        [Fact]
        public async Task Handle_ValidHealing_ReturnsSuccess()
        {
            // Arrange - Create damaged actor
            var damagedActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Damaged Warrior").Match(
                Succ: a => a.TakeDamage(30).Match(Succ: damaged => damaged, Fail: _ => throw new InvalidOperationException()),
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );
            
            var command = HealActorCommand.Create(_validActorId, 20, "Healing Potion");
            var actorService = new TestActorStateService(actorExists: true, actor: damagedActor, healSucceeds: true);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ReturnsError()
        {
            // Arrange
            var command = HealActorCommand.Create(_validActorId, 25);
            var actorService = new TestActorStateService(actorExists: false);
            var handler = new HealActorCommandHandler(actorService, null!);

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
        public async Task Handle_NegativeHealAmount_ReturnsError()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, -10);
            var actorService = new TestActorStateService(actorExists: true, actor: testActor);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("INVALID_HEAL")
            );
        }

        [Fact]
        public async Task Handle_DeadActor_ReturnsError()
        {
            // Arrange - Create dead actor
            var deadActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Dead Warrior").Match(
                Succ: a => a.SetToDead(),
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, 50, "Healing Attempt");
            var actorService = new TestActorStateService(actorExists: true, actor: deadActor);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("CANNOT_HEAL_DEAD")
            );
        }

        [Fact]
        public async Task Handle_ServiceHealFailure_ReturnsError()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, 25);
            var actorService = new TestActorStateService(actorExists: true, actor: testActor, healSucceeds: false);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("HEAL_FAILED")
            );
        }

        [Fact]
        public async Task Handle_ZeroHealing_ReturnsSuccess()
        {
            // Arrange
            var testActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, 0, "No Healing");
            var actorService = new TestActorStateService(actorExists: true, actor: testActor);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_FullHealthActor_ReturnsSuccess()
        {
            // Arrange - Actor already at full health
            var fullHealthActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Healthy Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, 25, "Overheal Attempt");
            var actorService = new TestActorStateService(actorExists: true, actor: fullHealthActor);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithSource_ProcessesCorrectly()
        {
            // Arrange - Damaged actor
            var damagedActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Injured Warrior").Match(
                Succ: a => a.TakeDamage(40).Match(Succ: damaged => damaged, Fail: _ => throw new InvalidOperationException()),
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, 30, "Divine Blessing");
            var actorService = new TestActorStateService(actorExists: true, actor: damagedActor);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_CriticallyDamagedActor_HealsCorrectly()
        {
            // Arrange - Actor with 1 health remaining
            var criticalActor = Darklands.Core.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Critical Warrior").Match(
                Succ: a => a.TakeDamage(99).Match(Succ: damaged => damaged, Fail: _ => throw new InvalidOperationException()),
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = HealActorCommand.Create(_validActorId, 50, "Major Healing");
            var actorService = new TestActorStateService(actorExists: true, actor: criticalActor);
            var handler = new HealActorCommandHandler(actorService, null!);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }
    }
}