using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Core.Application.Grid.Commands;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Tests.TestUtilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Application.Grid.Commands
{
    [Trait("Category", "Phase2")]
    public class SpawnDummyCommandHandlerTests
    {
        // Test stub for IGridStateService - configurable for different test scenarios
        private class TestGridStateService : IGridStateService
        {
            private readonly bool _positionEmpty;
            private readonly bool _addToGridSucceeds;

            public TestGridStateService(bool positionEmpty = true, bool addToGridSucceeds = true)
            {
                _positionEmpty = positionEmpty;
                _addToGridSucceeds = addToGridSucceeds;
            }

            public bool IsPositionEmpty(Position position) => _positionEmpty;

            public Fin<Unit> AddActorToGrid(ActorId actorId, Position position)
                => _addToGridSucceeds ? FinSucc(Unit.Default) : FinFail<Unit>(Error.New("GRID_ERROR: Failed to add actor to grid"));

            // Required interface methods - minimal implementations for testing
            public Option<Position> GetActorPosition(ActorId actorId) => None;
            public Fin<Unit> ValidateMove(Position fromPosition, Position toPosition) => FinSucc(Unit.Default);
            public Fin<Unit> MoveActor(ActorId actorId, Position toPosition) => FinSucc(Unit.Default);
            public Fin<Darklands.Core.Domain.Grid.Grid> GetCurrentGrid() => throw new NotImplementedException();
            public bool IsValidPosition(Position position) => true;
            public Fin<Unit> RemoveActorFromGrid(ActorId actorId) => FinSucc(Unit.Default);
            public IReadOnlyDictionary<ActorId, Position> GetAllActorPositions() =>
                new Dictionary<ActorId, Position>();
        }

        // Test stub for IActorStateService - configurable for different test scenarios
        private class TestActorStateService : IActorStateService
        {
            private readonly bool _addActorSucceeds;

            public TestActorStateService(bool addActorSucceeds = true)
            {
                _addActorSucceeds = addActorSucceeds;
            }

            public Fin<Unit> AddActor(Darklands.Core.Domain.Actor.Actor actor)
                => _addActorSucceeds ? FinSucc(Unit.Default) : FinFail<Unit>(Error.New("ACTOR_ERROR: Failed to add actor"));

            // Required interface methods - minimal implementations for testing
            public Option<Darklands.Core.Domain.Actor.Actor> GetActor(ActorId actorId) => None;
            public Fin<Unit> UpdateActorHealth(ActorId actorId, Health newHealth) => FinSucc(Unit.Default);
            public Fin<Darklands.Core.Domain.Actor.Actor> DamageActor(ActorId actorId, int damage) => FinFail<Darklands.Core.Domain.Actor.Actor>(Error.New("Not implemented"));
            public Fin<Darklands.Core.Domain.Actor.Actor> HealActor(ActorId actorId, int healAmount) => FinFail<Darklands.Core.Domain.Actor.Actor>(Error.New("Not implemented"));
            public Option<bool> IsActorAlive(ActorId actorId) => None;
            public Fin<Unit> RemoveDeadActor(ActorId actorId) => FinSucc(Unit.Default);
        }

        private readonly Position _validPosition = new Position(5, 5);

        [Fact]
        public async Task Handle_ValidSpawn_ReturnsSuccess()
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, "Test Dummy");
            var gridStateService = new TestGridStateService(positionEmpty: true, addToGridSucceeds: true);
            var actorStateService = new TestActorStateService(addActorSucceeds: true);
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_PositionOccupied_ReturnsError()
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, "Test Dummy");
            var gridStateService = new TestGridStateService(positionEmpty: false);
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("POSITION_OCCUPIED")
            );
        }

        [Fact]
        public async Task Handle_InvalidDummyParameters_ReturnsError()
        {
            // Arrange - Invalid max health (negative)
            var command = SpawnDummyCommand.Create(_validPosition, -10, "Invalid Dummy");
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("health must be greater than 0")
            );
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task Handle_InvalidDummyName_ReturnsError(string? invalidName)
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, invalidName!);
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Dummy name cannot be empty")
            );
        }

        [Fact]
        public async Task Handle_ActorStateServiceFails_ReturnsError()
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, "Test Dummy");
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService(addActorSucceeds: false);
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("ACTOR_ERROR")
            );
        }

        [Fact]
        public async Task Handle_GridServiceFails_ReturnsError()
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, "Test Dummy");
            var gridStateService = new TestGridStateService(positionEmpty: true, addToGridSucceeds: false);
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("GRID_ERROR")
            );
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        public async Task Handle_ValidMaxHealthValues_ReturnsSuccess(int maxHealth)
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, maxHealth, "Health Test Dummy");
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task Handle_InvalidMaxHealthValues_ReturnsError(int invalidMaxHealth)
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, invalidMaxHealth, "Invalid Health Dummy");
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("Maximum health must be greater than 0")
            );
        }

        [Fact]
        public async Task Handle_CancellationRequested_StillCompletes()
        {
            // Arrange
            var command = SpawnDummyCommand.Create(_validPosition, 50, "Test Dummy");
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);
            var cancellationToken = new CancellationToken(canceled: true);

            // Act & Assert - Should complete despite cancellation request
            var result = await handler.Handle(command, cancellationToken);
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_CreateFactoryMethod_ProducesValidCommand()
        {
            // Arrange - Test the factory method creates valid command
            var command = SpawnDummyCommand.CreateCombatDummy(_validPosition);
            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_MultipleSequentialCommands_EachSucceeds()
        {
            // Arrange
            var position1 = new Position(1, 1);
            var position2 = new Position(2, 2);

            var command1 = SpawnDummyCommand.CreateCombatDummy(position1);
            var command2 = SpawnDummyCommand.CreateTrainingDummy(position2);

            var gridStateService = new TestGridStateService();
            var actorStateService = new TestActorStateService();
            var handler = new SpawnDummyCommandHandler(gridStateService, actorStateService, null!, TestIdGenerator.Instance);

            // Act
            var result1 = await handler.Handle(command1, CancellationToken.None);
            var result2 = await handler.Handle(command2, CancellationToken.None);

            // Assert
            result1.IsSucc.Should().BeTrue();
            result2.IsSucc.Should().BeTrue();
        }
    }
}
