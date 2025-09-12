using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
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
using Darklands.Core.Domain.Debug;
using static LanguageExt.Prelude;

namespace Darklands.Core.Tests.Infrastructure.Combat;

/// <summary>
/// Tests for CombatSwitchAdapter to verify TD_043 Strangler Fig routing.
/// Validates that the adapter correctly routes to legacy or tactical implementations
/// based on the feature toggle configuration.
/// </summary>
public class CombatSwitchAdapterTests
{
    [Fact]
    public async Task RouteAttackCommand_WithLegacyConfig_CallsLegacyHandler()
    {
        // Arrange
        var mockLegacyHandler = new Mock<ExecuteAttackCommandHandler>(
            Mock.Of<IGridStateService>(),
            Mock.Of<IActorStateService>(),
            Mock.Of<ICombatSchedulerService>(),
            Mock.Of<MediatR.IMediator>(),
            Mock.Of<ICategoryLogger>(),
            null);

        mockLegacyHandler
            .Setup(h => h.Handle(It.IsAny<ExecuteAttackCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinSucc(Unit.Default));

        var config = new StranglerFigConfiguration { UseTacticalContext = false };
        var adapter = new CombatSwitchAdapter(
            mockLegacyHandler.Object,
            Mock.Of<ProcessNextTurnCommandHandler>(),
            config,
            Mock.Of<ILogger<CombatSwitchAdapter>>(),
            Mock.Of<IServiceProvider>());

        var command = new ExecuteAttackCommand(
            attackerId: Guid.NewGuid(),
            targetId: Guid.NewGuid(),
            combatAction: new Darklands.Core.Domain.Combat.CombatAction("Test", 10, 100));

        // Act
        var result = await adapter.RouteAttackCommand(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSucc);
        mockLegacyHandler.Verify(
            h => h.Handle(It.IsAny<ExecuteAttackCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RouteAttackCommand_WithTacticalConfig_CallsTacticalHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        // Register tactical handler
        var mockTacticalHandler = new Mock<Tactical.Application.Features.Combat.Attack.ExecuteAttackCommandHandler>(
            Mock.Of<Tactical.Application.Features.Combat.Services.IActorRepository>(),
            Mock.Of<MediatR.IMediator>());

        mockTacticalHandler
            .Setup(h => h.Handle(It.IsAny<Tactical.Application.Features.Combat.Attack.ExecuteAttackCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinSucc(new Tactical.Application.Features.Combat.Attack.AttackResult(10, 90, false)));

        services.AddSingleton(mockTacticalHandler.Object);
        var serviceProvider = services.BuildServiceProvider();

        var config = new StranglerFigConfiguration { UseTacticalContext = true };
        var adapter = new CombatSwitchAdapter(
            Mock.Of<ExecuteAttackCommandHandler>(),
            Mock.Of<ProcessNextTurnCommandHandler>(),
            config,
            Mock.Of<ILogger<CombatSwitchAdapter>>(),
            serviceProvider);

        var command = new ExecuteAttackCommand(
            attackerId: Guid.NewGuid(),
            targetId: Guid.NewGuid(),
            combatAction: new Darklands.Core.Domain.Combat.CombatAction("Test", 10, 100));

        // Act
        var result = await adapter.RouteAttackCommand(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSucc);
        mockTacticalHandler.Verify(
            h => h.Handle(It.IsAny<Tactical.Application.Features.Combat.Attack.ExecuteAttackCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RouteAttackCommand_CanSwitchBetweenImplementations()
    {
        // Arrange
        var mockLegacyHandler = new Mock<ExecuteAttackCommandHandler>(
            Mock.Of<IGridStateService>(),
            Mock.Of<IActorStateService>(),
            Mock.Of<ICombatSchedulerService>(),
            Mock.Of<MediatR.IMediator>(),
            Mock.Of<ICategoryLogger>(),
            null);

        mockLegacyHandler
            .Setup(h => h.Handle(It.IsAny<ExecuteAttackCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FinSucc(Unit.Default));

        var config = new StranglerFigConfiguration { UseTacticalContext = false };
        var adapter = new CombatSwitchAdapter(
            mockLegacyHandler.Object,
            Mock.Of<ProcessNextTurnCommandHandler>(),
            config,
            Mock.Of<ILogger<CombatSwitchAdapter>>(),
            Mock.Of<IServiceProvider>());

        var command = new ExecuteAttackCommand(
            attackerId: Guid.NewGuid(),
            targetId: Guid.NewGuid(),
            combatAction: new Darklands.Core.Domain.Combat.CombatAction("Test", 10, 100));

        // Act - First call uses legacy
        var result1 = await adapter.RouteAttackCommand(command, CancellationToken.None);
        Assert.True(result1.IsSucc);

        // Switch to tactical (simulating runtime toggle)
        config.UseTacticalContext = true;

        // Note: In this test, tactical will fail because we don't have it registered
        // but that proves the switch is working
        var result2 = await adapter.RouteAttackCommand(command, CancellationToken.None);
        Assert.True(result2.IsFail); // Expected since tactical handler not in DI

        // Switch back to legacy
        config.UseTacticalContext = false;
        var result3 = await adapter.RouteAttackCommand(command, CancellationToken.None);
        Assert.True(result3.IsSucc);

        // Verify legacy was called twice (first and third calls)
        mockLegacyHandler.Verify(
            h => h.Handle(It.IsAny<ExecuteAttackCommand>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
