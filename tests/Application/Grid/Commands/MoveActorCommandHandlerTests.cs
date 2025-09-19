using Xunit;
using FluentAssertions;
using LanguageExt;
using LanguageExt.Common;
using Darklands.Application.Grid.Commands;
using Darklands.Application.Grid.Services;
using Darklands.Application.Actor.Services;
using Darklands.Application.Grid.Queries;
using Darklands.Application.Events;
using Darklands.Domain.Grid;
using Darklands.Domain.Actor;
using Darklands.Core.Tests.TestUtilities;
using MediatR;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using LangExtUnit = LanguageExt.Unit;

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

            public Fin<LangExtUnit> ValidateMove(Position fromPosition, Position toPosition)
                => _validMove ? FinSucc(LangExtUnit.Default) : FinFail<LangExtUnit>(Error.New("INVALID_MOVE: Invalid move"));

            public Fin<LangExtUnit> MoveActor(ActorId actorId, Position toPosition)
                => FinSucc(LangExtUnit.Default);

            public Fin<Darklands.Domain.Grid.Grid> GetCurrentGrid() => throw new NotImplementedException();
            public bool IsValidPosition(Position position) => true;
            public bool IsPositionEmpty(Position position) => true;

            // TD_009: New interface methods for SSOT architecture
            public Fin<LangExtUnit> AddActorToGrid(ActorId actorId, Position position) => FinSucc(LangExtUnit.Default);
            public Fin<LangExtUnit> RemoveActorFromGrid(ActorId actorId) => FinSucc(LangExtUnit.Default);
            public IReadOnlyDictionary<ActorId, Position> GetAllActorPositions() =>
                new Dictionary<ActorId, Position>();

            // VS_014: New pathfinding methods
            public System.Collections.Immutable.ImmutableHashSet<Position> GetObstacles() =>
                System.Collections.Immutable.ImmutableHashSet<Position>.Empty;

            public bool IsWalkable(Position position) => true;
        }

        // Test stub for IActorStateService - minimal implementation for testing
        private class TestActorStateService : IActorStateService
        {
            private readonly bool _actorExists;
            private readonly Darklands.Domain.Actor.Actor? _testActor;

            public TestActorStateService(bool actorExists = true)
            {
                _actorExists = actorExists;
                if (actorExists)
                {
                    var actorId = ActorId.FromGuid(Guid.NewGuid());
                    var health = Darklands.Domain.Actor.Health.Create(100, 100)
                        .IfFail(error => throw new InvalidOperationException($"Failed to create health: {error.Message}"));
                    _testActor = new Darklands.Domain.Actor.Actor(
                        actorId,
                        health,
                        "TestActor",
                        ImmutableDictionary<string, string>.Empty
                    );
                }
            }

            public Option<Darklands.Domain.Actor.Actor> GetActor(ActorId actorId)
                => _actorExists ? Some(_testActor!) : None;

            public Fin<LangExtUnit> AddActor(Darklands.Domain.Actor.Actor actor) => FinSucc(LangExtUnit.Default);
            public IReadOnlyDictionary<ActorId, Darklands.Domain.Actor.Actor> GetAllActors() =>
                new Dictionary<ActorId, Darklands.Domain.Actor.Actor>();
            public Fin<LangExtUnit> RemoveActor(ActorId actorId) => FinSucc(LangExtUnit.Default);

            // Additional interface methods - stubs for testing
            public Fin<LangExtUnit> UpdateActorHealth(ActorId actorId, Darklands.Domain.Actor.Health health) => FinSucc(LangExtUnit.Default);
            public Fin<Darklands.Domain.Actor.Actor> DamageActor(ActorId actorId, int damage) =>
                _actorExists ? FinSucc(_testActor!) : FinFail<Darklands.Domain.Actor.Actor>(Error.New("Actor not found"));
            public Fin<Darklands.Domain.Actor.Actor> HealActor(ActorId actorId, int healing) =>
                _actorExists ? FinSucc(_testActor!) : FinFail<Darklands.Domain.Actor.Actor>(Error.New("Actor not found"));
            public Option<bool> IsActorAlive(ActorId actorId) => _actorExists ? Some(true) : None;
            public Fin<LangExtUnit> RemoveDeadActor(ActorId actorId) => FinSucc(LangExtUnit.Default);
        }

        // Test stub for IMediator - minimal implementation for testing
        private class TestMediator : IMediator
        {
            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                // For CalculatePathQuery, return a simple path
                if (request is CalculatePathQuery pathQuery)
                {
                    var simplePath = Seq(
                        pathQuery.FromPosition,
                        pathQuery.ToPosition
                    );
                    return Task.FromResult((TResponse)(object)FinSucc(simplePath));
                }
                throw new NotImplementedException($"TestMediator doesn't handle {typeof(TResponse).Name}");
            }

            public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
                where TRequest : IRequest
                => Task.CompletedTask;

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public Task Publish(object notification, CancellationToken cancellationToken = default)
                => Task.CompletedTask; // Just succeed for events

            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
                where TNotification : INotification
                => Task.CompletedTask; // Just succeed for events

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();

            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
                => throw new NotImplementedException();
        }

        [Fact]
        public async Task Handle_ValidMove_ReturnsSuccess()
        {
            // Arrange
            var actorId = ActorId.FromGuid(Guid.NewGuid());
            var toPosition = new Position(5, 5);
            var command = MoveActorCommand.Create(actorId, toPosition);

            // Use test stubs that simulate successful scenario
            var gridStateService = new TestGridStateService(actorExists: true, validMove: true);
            var actorStateService = new TestActorStateService(actorExists: true);
            var mediator = new TestMediator();
            var handler = new MoveActorCommandHandler(gridStateService, actorStateService, mediator, new NullCategoryLogger());

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

            // Use test stubs that simulate actor not found
            var gridStateService = new TestGridStateService(actorExists: false);
            var actorStateService = new TestActorStateService(actorExists: false);
            var mediator = new TestMediator();
            var handler = new MoveActorCommandHandler(gridStateService, actorStateService, mediator, new NullCategoryLogger());

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
