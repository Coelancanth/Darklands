using CSharpFunctionalExtensions;
using Darklands.Core.Application.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace Darklands.Core.Tests.Application.Infrastructure;

/// <summary>
/// Tests for GameStrapper - DI container bootstrapper.
/// Category: Phase1 for VS_002 implementation gates.
/// </summary>
[Trait("Category", "Phase1")]
public class GameStrapperTests
{
    [Fact]
    public void Initialize_ShouldSucceed_OnFirstCall()
    {
        // Arrange
        GameStrapper.Reset(); // Clean state for test

        // Act
        var result = GameStrapper.Initialize();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Initialize_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act
        var result = GameStrapper.Initialize();

        // Assert
        result.IsSuccess.Should().BeTrue("Initialize should be safe to call multiple times");
    }

    [Fact]
    public void GetServices_ShouldReturnFailure_BeforeInitialization()
    {
        // Arrange
        GameStrapper.Reset();

        // Act
        var result = GameStrapper.GetServices();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not initialized");
    }

    [Fact]
    public void GetServices_ShouldReturnServiceProvider_AfterInitialization()
    {
        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act
        var result = GameStrapper.GetServices();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Initialize_ShouldRegisterTestService()
    {
        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();

        // Act
        var providerResult = GameStrapper.GetServices();
        var testService = providerResult.Value.GetService<ITestService>();

        // Assert
        testService.Should().NotBeNull("ITestService should be registered for validation");
        testService!.GetTestMessage().Should().Be("DI is working!");
    }

    [Fact]
    public void Initialize_ShouldRegisterSingletonServices_WithSingletonLifetime()
    {
        // Arrange
        GameStrapper.Reset();
        GameStrapper.Initialize();
        var provider = GameStrapper.GetServices().Value;

        // Act
        var service1 = provider.GetService<ITestService>();
        var service2 = provider.GetService<ITestService>();

        // Assert
        service1.Should().NotBeNull();
        service2.Should().NotBeNull();
        service1.Should().BeSameAs(service2, "Singleton services should return same instance");
    }
}
