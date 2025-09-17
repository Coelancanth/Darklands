using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Application.Actor.Commands;
using Darklands.Application.Actor.Services;
using Darklands.Domain.Combat.Services;
using Darklands.Domain.Grid;
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
        // Test stub for IDamageService - minimal implementation for testing
        private class TestDamageService : IDamageService
        {
            private readonly Darklands.Domain.Actor.Actor? _actor;
            private readonly bool _damageSucceeds;

            public TestDamageService(Darklands.Domain.Actor.Actor? actor = null, bool damageSucceeds = true)
            {
                _actor = actor;
                _damageSucceeds = damageSucceeds;
            }

            public Fin<Darklands.Domain.Actor.Actor> ApplyDamage(ActorId actorId, int damage, string source)
            {
                if (!_damageSucceeds)
                    return FinFail<Darklands.Domain.Actor.Actor>(Error.New("DAMAGE_FAILED: Simulated failure"));

                if (_actor == null)
                    return FinFail<Darklands.Domain.Actor.Actor>(Error.New("ACTOR_NOT_FOUND: Actor not found"));

                if (damage < 0)
                    return FinFail<Darklands.Domain.Actor.Actor>(Error.New("INVALID_DAMAGE: Damage amount cannot be negative"));

                var result = _actor.TakeDamage(damage);
                return result.Match(
                    Succ: damagedActor => FinSucc(damagedActor),
                    Fail: error => FinFail<Darklands.Domain.Actor.Actor>(error)
                );
            }
        }

        private readonly ActorId _validActorId = ActorId.NewId(TestIdGenerator.Instance);

        [Fact]
        public async Task Handle_ValidDamage_ReturnsSuccess()
        {
            // Arrange
            var testActor = Darklands.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 25, "Sword Attack");
            var damageService = new TestDamageService(actor: testActor, damageSucceeds: true);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

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
            var damageService = new TestDamageService(actor: null);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

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
            var testActor = Darklands.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, -10);
            var damageService = new TestDamageService(actor: testActor);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

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
            var testActor = Darklands.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 25);
            var damageService = new TestDamageService(actor: testActor, damageSucceeds: false);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

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
            var testActor = Darklands.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 0, "No Damage");
            var damageService = new TestDamageService(actor: testActor);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_LethalDamage_ReturnsSuccess()
        {
            // Arrange - Create actor with low health
            var lowHealthActor = Darklands.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 10, "Weak Actor").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 15, "Death Blow"); // More than current health
            var damageService = new TestDamageService(actor: lowHealthActor);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithSource_ProcessesCorrectly()
        {
            // Arrange
            var testActor = Darklands.Domain.Actor.Actor.CreateAtFullHealth(_validActorId, 100, "Test Warrior").Match(
                Succ: a => a,
                Fail: _ => throw new InvalidOperationException("Test setup failed")
            );

            var command = DamageActorCommand.Create(_validActorId, 30, "Fire Spell");
            var damageService = new TestDamageService(actor: testActor);
            var handler = new DamageActorCommandHandler(damageService, new NullCategoryLogger());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }
    }
}
