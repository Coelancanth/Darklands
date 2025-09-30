using CSharpFunctionalExtensions;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Infrastructure.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.DependencyInjection;

/// <summary>
/// Tests for ServiceLocator - Godot boundary pattern for service resolution.
/// Category: Phase2 for VS_002 implementation gates.
/// Collection: GameStrapperCollection prevents parallel execution (shared static state)
/// </summary>
[Trait("Category", "Phase2")]
[Collection("GameStrapperCollection")]
public class ServiceLocatorTests
{
    [Fact]
    public void GetService_ShouldReturnFailure_BeforeInitialization()
    {
        // WHY: Godot nodes may call GetService() during uncertain initialization phases
        // Result<T> pattern allows graceful error handling instead of crashes

        // Arrange
        GameStrapper.Reset();

        // Act
        var result = ServiceLocator.GetService<ITestService>();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not initialized");
    }

    [Fact]
    public void GetService_ShouldReturnSuccess_AfterInitialization()
    {
        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act
        var result = ServiceLocator.GetService<ITestService>();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void GetService_ShouldReturnFailure_WhenServiceNotRegistered()
    {
        // WHY: Developer errors (typos, forgot to register service) should return descriptive
        // Result.Failure with helpful message instead of null reference exceptions

        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act
        var result = ServiceLocator.GetService<IUnregisteredService>();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not registered");
    }

    [Fact]
    public void Get_ShouldReturnService_AfterInitialization()
    {
        // WHY: Get<T>() is fail-fast API for post-bootstrap code (Godot _Ready() methods)
        // Returns service directly or throws - simpler than GetService<T>() Result<T> checking

        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act
        var service = ServiceLocator.Get<ITestService>();

        // Assert
        service.Should().NotBeNull();
        service.GetTestMessage().Should().Be("DI is working!");
    }

    [Fact]
    public void Get_ShouldThrow_WhenServiceNotAvailable()
    {
        // WHY: Get<T>() is fail-fast - missing services indicate misconfiguration
        // Better to crash immediately with clear exception than silently continue with null

        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act - Try to get unregistered service
        Action act = () => ServiceLocator.Get<IUnregisteredService>();

        // Assert
        act.Should().Throw<InvalidOperationException>("Get() should throw when service not registered");
    }

    [Fact]
    public void GetService_ShouldReturnSingletonInstances_WithinSameContainer()
    {
        // WHY: ServiceLocator delegates to GameStrapper's container - must preserve
        // singleton lifetime guarantees for stateful services across multiple resolutions

        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act - resolve same service twice from same container
        var result1 = ServiceLocator.GetService<ITestService>();
        var result2 = ServiceLocator.GetService<ITestService>();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().BeSameAs(result2.Value,
            "Singleton services should return same instance within same container");
    }

    [Fact]
    public void GetService_ShouldReturnNewInstances_AfterContainerReset()
    {
        // WHY: Test isolation requires fresh container with new instances
        // Reset() disposes old container, Initialize() creates new one with new singletons
        // EDGE CASE: Holding references across Reset() will cause stale state bugs

        // Arrange - First container
        GameStrapper.Reset();
        var initResult1 = GameStrapper.Initialize();
        initResult1.IsSuccess.Should().BeTrue("First Initialize should succeed");

        var result1 = ServiceLocator.GetService<ITestService>();
        result1.IsSuccess.Should().BeTrue("First container should resolve service");
        var service1 = result1.Value;

        // Act - Reset and re-initialize creates NEW container
        GameStrapper.Reset();
        var initResult2 = GameStrapper.Initialize();
        initResult2.IsSuccess.Should().BeTrue("Second Initialize should succeed");

        var result2 = ServiceLocator.GetService<ITestService>();
        result2.IsSuccess.Should().BeTrue("Second container should resolve service");
        var service2 = result2.Value;

        // Assert
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service1.Should().NotBeSameAs(service2,
            "New container should create new singleton instances");
    }
}

/// <summary>
/// Unregistered service for testing failure paths.
/// </summary>
public interface IUnregisteredService
{
}
