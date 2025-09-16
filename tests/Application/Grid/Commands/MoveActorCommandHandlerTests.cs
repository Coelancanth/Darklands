using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Application.Grid.Commands;
using Darklands.Application.Grid.Services;
using Darklands.Domain.Grid;
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
    public class MoveActorCommandHandlerTests
    {
        // Test stub for IGridStateService - minimal implementation for testing
        private class TestGridStateService : IGridStateService
        {
            private readonly bool _actorExists;
            private readonly bool _validMove;

            public TestGridStateService(bool actorExists = true, bool validMove = true)
            {
                _actorExists = actorExists;
                _validMove = validMove;
            }

            public Option<Position> GetActorPosition(ActorId actorId)
                => _actorExists ? Some(new Position(3, 3)) : None;

            public Fin<Unit> ValidateMove(Position fromPosition, Position toPosition)
                => _validMove ? FinSucc(Unit.Default) : FinFail<Unit>(Error.New("INVALID_MOVE: Invalid move"));

            public Fin<Unit> MoveActor(ActorId actorId, Position toPosition)
                => FinSucc(Unit.Default);

            public Fin<Darklands.Domain.Grid.Grid> GetCurrentGrid() => throw new NotImplementedException();
            public bool IsValidPosition(Position position) => true;
            public bool IsPositionEmpty(Position position) => true;

            // TD_009: New interface methods for SSOT architecture
            public Fin<Unit> AddActorToGrid(ActorId actorId, Position position) => FinSucc(Unit.Default);
            public Fin<Unit> RemoveActorFromGrid(ActorId actorId) => FinSucc(Unit.Default);
            public IReadOnlyDictionary<ActorId, Position> GetAllActorPositions() =>
                new Dictionary<ActorId, Position>();
        }

        [Fact]
        public async Task Handle_ValidMove_ReturnsSuccess()
        {
            // Arrange
            var actorId = ActorId.FromGuid(Guid.NewGuid());
            var toPosition = new Position(5, 5);
            var command = MoveActorCommand.Create(actorId, toPosition);

            // Use test stub that simulates successful scenario
            var gridStateService = new TestGridStateService(actorExists: true, validMove: true);
            var handler = new MoveActorCommandHandler(gridStateService, new NullCategoryLogger());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSucc.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ActorNotFound_ReturnsError()
        {
            // Arrange
            var actorId = ActorId.FromGuid(Guid.NewGuid());
            var toPosition = new Position(5, 5);
            var command = MoveActorCommand.Create(actorId, toPosition);

            // Use test stub that simulates actor not found
            var gridStateService = new TestGridStateService(actorExists: false);
            var handler = new MoveActorCommandHandler(gridStateService, new NullCategoryLogger());

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsFail.Should().BeTrue();
            result.Match(
                Succ: _ => throw new InvalidOperationException("Expected failure"),
                Fail: error => error.Message.Should().Contain("ACTOR")
            );
        }
    }
}
