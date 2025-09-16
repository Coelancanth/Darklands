using System;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Darklands.Application.Infrastructure.DependencyInjection;
using Darklands.Presentation.Presenters;

namespace Darklands.Core.Tests.Architecture
{
    /// <summary>
    /// Tests to verify that presenters can be resolved from the DI container
    /// after the fixes to remove view dependencies from constructors.
    /// </summary>
    public class PresenterRegistrationTests
    {
        [Fact]
        public void Presenters_Should_Be_Resolvable_From_DI_Container()
        {
            // Arrange - Initialize the DI container
            var result = GameStrapper.Initialize();
            result.IsSucc.Should().BeTrue("GameStrapper should initialize successfully");

            var serviceProvider = result.Match(
                Succ: sp => sp,
                Fail: error => throw new InvalidOperationException($"Failed to initialize: {error}")
            );

            // Act & Assert - Verify each presenter can be resolved

            // IGridPresenter should be resolvable
            var gridPresenter = serviceProvider.GetService<IGridPresenter>();
            gridPresenter.Should().NotBeNull("IGridPresenter should be registered");
            gridPresenter.Should().BeOfType<GridPresenter>("Should resolve to concrete GridPresenter");

            // IActorPresenter should be resolvable
            var actorPresenter = serviceProvider.GetService<IActorPresenter>();
            actorPresenter.Should().NotBeNull("IActorPresenter should be registered");
            actorPresenter.Should().BeOfType<ActorPresenter>("Should resolve to concrete ActorPresenter");

            // IAttackPresenter should be resolvable
            var attackPresenter = serviceProvider.GetService<IAttackPresenter>();
            attackPresenter.Should().NotBeNull("IAttackPresenter should be registered");
            attackPresenter.Should().BeOfType<AttackPresenter>("Should resolve to concrete AttackPresenter");

            // Cleanup
            GameStrapper.Dispose();
        }

        [Fact]
        public void Presenters_Should_Not_Require_Views_In_Constructors()
        {
            // This test verifies that presenters can be created without views,
            // which is essential for DI container registration

            // Arrange
            var result = GameStrapper.Initialize();
            result.IsSucc.Should().BeTrue();

            var serviceProvider = result.Match(
                Succ: sp => sp,
                Fail: error => throw new InvalidOperationException($"Failed to initialize: {error}")
            );

            // Act - Create presenters without views
            var gridPresenter = serviceProvider.GetRequiredService<IGridPresenter>() as GridPresenter;
            var actorPresenter = serviceProvider.GetRequiredService<IActorPresenter>() as ActorPresenter;
            var attackPresenter = serviceProvider.GetRequiredService<IAttackPresenter>() as AttackPresenter;

            // Assert - Presenters should be created successfully
            gridPresenter.Should().NotBeNull("GridPresenter should be created without a view");
            actorPresenter.Should().NotBeNull("ActorPresenter should be created without a view");
            attackPresenter.Should().NotBeNull("AttackPresenter should be created without a view");

            // Verify they're in a valid state (not initialized with views yet)
            // AttachView would need to be called later by the views themselves

            // Cleanup
            GameStrapper.Dispose();
        }

        [Fact]
        public void GameStrapper_Should_Load_Presentation_Services_Via_Reflection()
        {
            // This test verifies that the reflection-based loading of
            // presentation services is working correctly

            // Arrange & Act
            var result = GameStrapper.Initialize();

            // Assert
            result.IsSucc.Should().BeTrue("GameStrapper initialization should succeed");

            var serviceProvider = result.Match(
                Succ: sp => sp,
                Fail: error => throw new InvalidOperationException($"Failed: {error}")
            );

            // Check that all three presenter interfaces are registered
            var services = new[]
            {
                typeof(IGridPresenter),
                typeof(IActorPresenter),
                typeof(IAttackPresenter)
            };

            foreach (var serviceType in services)
            {
                var service = serviceProvider.GetService(serviceType);
                service.Should().NotBeNull($"{serviceType.Name} should be registered in DI container");
            }

            // Cleanup
            GameStrapper.Dispose();
        }
    }
}
