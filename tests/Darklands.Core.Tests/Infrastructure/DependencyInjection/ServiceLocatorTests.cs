using CSharpFunctionalExtensions;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Infrastructure.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.DependencyInjection;

/// <summary>
/// Tests for ServiceLocator - Godot boundary pattern for service resolution.
/// Category: Phase2 for VS_002 implementation gates.
/// </summary>
[Trait("Category", "Phase2")]
public class ServiceLocatorTests
{
    [Fact]
    public void GetService_ShouldReturnFailure_BeforeInitialization()
    {
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
        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();
        var result1 = ServiceLocator.GetService<ITestService>();

        // Act - Reset and re-initialize creates NEW container
        GameStrapper.Reset();
        GameStrapper.Initialize();
        var result2 = ServiceLocator.GetService<ITestService>();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBeSameAs(result2.Value,
            "New container should create new singleton instances");
    }
}

/// <summary>
/// Unregistered service for testing failure paths.
/// </summary>
public interface IUnregisteredService
{
}