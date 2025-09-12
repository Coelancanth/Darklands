using Xunit;
using Microsoft.Extensions.DependencyInjection;
using LanguageExt;
using System;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Application.Combat.Commands;
using Darklands.Core.Infrastructure.Combat;
using Darklands.Core.Infrastructure.Configuration;
using Darklands.Core.Application.Combat.Services;
using Darklands.Core.Application.Actor.Services;
using Darklands.Core.Application.Grid.Services;
using Darklands.Core.Domain.Actor;
using Darklands.Core.Domain.Grid;
using Darklands.Core.Domain.Combat;
using Darklands.SharedKernel.Domain;
using Darklands.Tactical.Domain.Aggregates.Actors;
using Darklands.Tactical.Domain.ValueObjects;
using Darklands.Tactical.Application.Features.Combat.Services;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Infrastructure.Combat;

/// <summary>
/// TD_047: Test harness for validating combat system equivalence.
/// Creates identical test scenarios in both legacy and tactical systems
/// and validates that combat calculations produce the same results.
/// No production data synchronization required - pure algorithmic validation.
/// </summary>
public class CombatSystemValidationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly StranglerFigConfiguration _config;
    private readonly IActorStateService _legacyActorService;
    private readonly IActorRepository _tacticalActorRepository;
    private readonly IGridStateService _gridStateService;
    private readonly CombatSwitchAdapter _switchAdapter;

    // Test actor IDs
    private readonly Guid _attackerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _targetId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public CombatSystemValidationTests()
    {
        var services = new ServiceCollection();

        // Configure services for testing
        ConfigureTestServices(services);

        _serviceProvider = services.BuildServiceProvider();
        _config = _serviceProvider.GetRequiredService<StranglerFigConfiguration>();
        _legacyActorService = _serviceProvider.GetRequiredService<IActorStateService>();
        _tacticalActorRepository = _serviceProvider.GetRequiredService<IActorRepository>();
        _gridStateService = _serviceProvider.GetRequiredService<IGridStateService>();
        _switchAdapter = _serviceProvider.GetRequiredService<CombatSwitchAdapter>();
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        // Core configuration
        services.AddSingleton<StranglerFigConfiguration>();

        // Legacy services
        services.AddSingleton<IActorStateService, InMemoryActorStateService>();
        services.AddSingleton<IGridStateService, InMemoryGridStateService>();
        services.AddSingleton<ICombatSchedulerService, InMemoryCombatSchedulerService>();

        // Tactical services
        services.AddSingleton<IActorRepository, InMemoryActorRepository>();
        services.AddSingleton<ICombatSchedulerService, Tactical.Infrastructure.Services.InMemoryCombatSchedulerService>();

        // Combat handlers (not as IRequestHandler to avoid MediatR)
        services.AddTransient<Application.Combat.Commands.ExecuteAttackCommandHandler>();
        services.AddTransient<Application.Combat.Commands.ProcessNextTurnCommandHandler>();
        services.AddTransient<Tactical.Application.Features.Combat.Attack.ExecuteAttackCommandHandler>();
        services.AddTransient<Tactical.Application.Features.Combat.Scheduling.ProcessNextTurnCommandHandler>();

        // Switch adapter
        services.AddTransient<CombatSwitchAdapter>();

        // Add other required services with test implementations
        services.AddSingleton<MediatR.IMediator, TestMediator>();
        services.AddSingleton<Core.Domain.Debug.ICategoryLogger, TestLogger>();
    }

    /// <summary>
    /// TD_047 Test Case 1: Basic attack damage calculation equivalence.
    /// Verifies that both systems calculate the same damage for a basic attack.
    /// </summary>
    [Fact]
    public async Task BasicAttack_BothSystems_ProduceSameDamage()
    {
        // Arrange - Set up identical actors in both systems
        const int attackerHealth = 100;
        const int targetHealth = 100;
        const int baseDamage = 10;

        await SetupTestActorsInBothSystems(attackerHealth, targetHealth);

        // Act - Execute attack in legacy system
        _config.UseTacticalContext = false;
        var legacyCommand = new ExecuteAttackCommand(
            attackerId: _attackerId,
            targetId: _targetId,
            combatAction: new CombatAction("Basic Attack", baseDamage, 100));

        var legacyResult = await _switchAdapter.RouteAttackCommand(legacyCommand, CancellationToken.None);
        var legacyTargetHealth = GetLegacyActorHealth(_targetId);

        // Reset target health for tactical test
        await SetupTestActorsInBothSystems(attackerHealth, targetHealth);

        // Act - Execute attack in tactical system
        _config.UseTacticalContext = true;
        var tacticalResult = await _switchAdapter.RouteAttackCommand(legacyCommand, CancellationToken.None);
        var tacticalTargetHealth = await GetTacticalActorHealth(_targetId);

        // Assert - Both systems should calculate same damage
        Assert.True(legacyResult.IsSucc, "Legacy attack should succeed");
        Assert.True(tacticalResult.IsSucc, "Tactical attack should succeed");

        var legacyDamageDealt = targetHealth - legacyTargetHealth;
        var tacticalDamageDealt = targetHealth - tacticalTargetHealth;

        Assert.Equal(legacyDamageDealt, tacticalDamageDealt);
        Assert.Equal(baseDamage, legacyDamageDealt); // Should be exactly base damage with no modifiers
    }

    /// <summary>
    /// TD_047 Test Case 2: Multiple attacks in sequence.
    /// Verifies that both systems handle multiple attacks consistently.
    /// </summary>
    [Fact]
    public async Task MultipleAttacks_BothSystems_ProduceSameSequence()
    {
        // Arrange
        const int initialHealth = 100;
        const int attackDamage = 15;
        const int numberOfAttacks = 3;

        await SetupTestActorsInBothSystems(initialHealth, initialHealth);

        // Act - Execute multiple attacks in legacy
        _config.UseTacticalContext = false;
        for (int i = 0; i < numberOfAttacks; i++)
        {
            var command = new ExecuteAttackCommand(
                _attackerId, _targetId,
                new CombatAction($"Attack {i + 1}", attackDamage, 100));

            var result = await _switchAdapter.RouteAttackCommand(command, CancellationToken.None);
            Assert.True(result.IsSucc, $"Legacy attack {i + 1} should succeed");
        }
        var finalLegacyHealth = GetLegacyActorHealth(_targetId);

        // Reset and test tactical
        await SetupTestActorsInBothSystems(initialHealth, initialHealth);

        _config.UseTacticalContext = true;
        for (int i = 0; i < numberOfAttacks; i++)
        {
            var command = new ExecuteAttackCommand(
                _attackerId, _targetId,
                new CombatAction($"Attack {i + 1}", attackDamage, 100));

            var result = await _switchAdapter.RouteAttackCommand(command, CancellationToken.None);
            Assert.True(result.IsSucc, $"Tactical attack {i + 1} should succeed");
        }
        var finalTacticalHealth = await GetTacticalActorHealth(_targetId);

        // Assert
        Assert.Equal(finalLegacyHealth, finalTacticalHealth);
        Assert.Equal(initialHealth - (attackDamage * numberOfAttacks), finalLegacyHealth);
    }

    /// <summary>
    /// TD_047 Test Case 3: Lethal damage handling.
    /// Verifies that both systems handle killing blows the same way.
    /// </summary>
    [Fact]
    public async Task LethalAttack_BothSystems_HandleDeathIdentically()
    {
        // Arrange - Target with low health
        const int attackerHealth = 100;
        const int targetHealth = 5;
        const int lethalDamage = 20;

        await SetupTestActorsInBothSystems(attackerHealth, targetHealth);

        // Act - Legacy lethal attack
        _config.UseTacticalContext = false;
        var legacyCommand = new ExecuteAttackCommand(
            _attackerId, _targetId,
            new CombatAction("Lethal Strike", lethalDamage, 100));

        var legacyResult = await _switchAdapter.RouteAttackCommand(legacyCommand, CancellationToken.None);
        var legacyTargetAlive = IsLegacyActorAlive(_targetId);

        // Reset and test tactical
        await SetupTestActorsInBothSystems(attackerHealth, targetHealth);

        _config.UseTacticalContext = true;
        var tacticalResult = await _switchAdapter.RouteAttackCommand(legacyCommand, CancellationToken.None);
        var tacticalTargetAlive = await IsTacticalActorAlive(_targetId);

        // Assert - Both should kill the target
        Assert.True(legacyResult.IsSucc);
        Assert.True(tacticalResult.IsSucc);
        Assert.False(legacyTargetAlive, "Legacy target should be dead");
        Assert.False(tacticalTargetAlive, "Tactical target should be dead");
    }

    #region Helper Methods

    private async Task SetupTestActorsInBothSystems(int attackerHealth, int targetHealth)
    {
        // Setup legacy actors
        SetupLegacyActor(_attackerId, attackerHealth, new Position(5, 5));
        SetupLegacyActor(_targetId, targetHealth, new Position(5, 6));

        // Setup tactical actors
        await SetupTacticalActor(_attackerId, attackerHealth, new Position(5, 5));
        await SetupTacticalActor(_targetId, targetHealth, new Position(5, 6));

        // Setup grid (shared by both systems in this test)
        SetupTestGrid();
    }

    private void SetupLegacyActor(Guid id, int health, Position position)
    {
        var actor = ActorAggregate.Create(id, $"TestActor_{id}", ActorType.Player);

        // Set health using the legacy service
        _legacyActorService.CreateActor(id, $"TestActor_{id}", ActorType.Player);
        _legacyActorService.SetActorHealth(id, health);
        _legacyActorService.SetActorPosition(id, position);
    }

    private async Task SetupTacticalActor(Guid id, int health, Position position)
    {
        var actor = Actor.Create(
            new EntityId(id.ToString()),
            $"TestActor_{id}",
            ActorType.Enemy, // Tactical uses different enum
            health,
            10, // speed
            5   // initiative
        );

        await _tacticalActorRepository.AddAsync(actor);
    }

    private void SetupTestGrid()
    {
        // Create a simple 10x10 grid for testing
        var grid = new GridAggregate(10, 10);
        _gridStateService.InitializeGrid(10, 10);

        // Place actors on the grid
        _gridStateService.PlaceActor(_attackerId, new Position(5, 5));
        _gridStateService.PlaceActor(_targetId, new Position(5, 6));
    }

    private int GetLegacyActorHealth(Guid actorId)
    {
        var result = _legacyActorService.GetActorHealth(actorId);
        return result.Match(
            Succ: health => health,
            Fail: _ => -1
        );
    }

    private async Task<int> GetTacticalActorHealth(Guid actorId)
    {
        var result = await _tacticalActorRepository.GetByIdAsync(new EntityId(actorId.ToString()));
        return result.Match(
            Succ: actor => actor.Health.Current,
            Fail: _ => -1
        );
    }

    private bool IsLegacyActorAlive(Guid actorId)
    {
        var health = GetLegacyActorHealth(actorId);
        return health > 0;
    }

    private async Task<bool> IsTacticalActorAlive(Guid actorId)
    {
        var health = await GetTacticalActorHealth(actorId);
        return health > 0;
    }

    #endregion

    #region Test Doubles

    // Minimal test implementations for required services
    private class TestMediator : MediatR.IMediator
    {
        public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, CancellationToken ct = default)
            => Task.FromResult(default(TResponse)!);

        public Task Send<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : MediatR.IRequest
            => Task.CompletedTask;

        public Task Publish(object notification, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : MediatR.INotification
            => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(MediatR.IStreamRequest<TResponse> request, CancellationToken ct = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct = default)
            => AsyncEnumerable.Empty<object?>();
    }

    private class TestLogger : Core.Domain.Debug.ICategoryLogger
    {
        public void Log(Core.Domain.Debug.LogLevel level, Core.Domain.Debug.LogCategory category, string message, params object[] args) { }
    }

    // In-memory implementation of tactical actor repository
    private class InMemoryActorRepository : IActorRepository
    {
        private readonly Dictionary<EntityId, Actor> _actors = new();

        public Task<Fin<Actor>> GetByIdAsync(EntityId id)
        {
            return Task.FromResult(
                _actors.TryGetValue(id, out var actor)
                    ? FinSucc(actor)
                    : FinFail<Actor>(Error.New($"Actor {id} not found"))
            );
        }

        public Task<Fin<Unit>> AddAsync(Actor actor)
        {
            _actors[actor.Id] = actor;
            return Task.FromResult(FinSucc(Unit.Default));
        }

        public Task<Fin<Unit>> UpdateAsync(Actor actor)
        {
            _actors[actor.Id] = actor;
            return Task.FromResult(FinSucc(Unit.Default));
        }

        public Task<Fin<Unit>> DeleteAsync(EntityId id)
        {
            _actors.Remove(id);
            return Task.FromResult(FinSucc(Unit.Default));
        }

        public Task<Fin<Seq<Actor>>> GetAllAsync()
        {
            return Task.FromResult(FinSucc(toSeq(_actors.Values)));
        }
    }

    #endregion

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
