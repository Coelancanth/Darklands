using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using LanguageExt;
using Darklands.Core.Infrastructure.DependencyInjection;
using Darklands.Core.Domain.Services;
using Darklands.Core.Infrastructure.Services;

namespace Darklands.Core.Tests.Infrastructure.Services;

/// <summary>
/// Integration tests for TD_022 Core Abstraction Services.
/// Verifies that services are properly registered in the DI container and can be resolved.
/// Tests the integration between domain interfaces and infrastructure implementations.
/// </summary>
[Trait("Category", "Integration")]
[Collection("GameStrapper")]
public class CoreAbstractionServicesIntegrationTests
{
    [Fact]
    public void GameStrapper_ShouldRegisterAllCoreAbstractionServices()
    {
        // Act
        var result = GameStrapper.Initialize();

        // Assert
        result.IsSucc.Should().BeTrue();

        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        // Verify all three core abstraction services are registered
        // NOTE: In Core project, these resolve to Mock implementations for testing
        var audioService = serviceProvider.GetService<IAudioService>();
        audioService.Should().NotBeNull();
        audioService.Should().BeOfType<MockAudioService>();

        var inputService = serviceProvider.GetService<IInputService>();
        inputService.Should().NotBeNull();
        inputService.Should().BeOfType<MockInputService>();

        var settingsService = serviceProvider.GetService<ISettingsService>();
        settingsService.Should().NotBeNull();
        settingsService.Should().BeOfType<MockSettingsService>();
    }

    [Fact]
    public void AudioService_FromDI_ShouldHaveCorrectLifetime()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        // Act - Request the service twice
        var audioService1 = serviceProvider.GetRequiredService<IAudioService>();
        var audioService2 = serviceProvider.GetRequiredService<IAudioService>();

        // Assert - Should be the same instance (Singleton)
        audioService1.Should().BeSameAs(audioService2);
    }

    [Fact]
    public void InputService_FromDI_ShouldHaveCorrectLifetime()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        // Act - Request the service twice
        var inputService1 = serviceProvider.GetRequiredService<IInputService>();
        var inputService2 = serviceProvider.GetRequiredService<IInputService>();

        // Assert - Should be the same instance (Singleton)
        inputService1.Should().BeSameAs(inputService2);
    }

    [Fact]
    public void SettingsService_FromDI_ShouldHaveCorrectLifetime()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        // Act - Request the service twice
        var settingsService1 = serviceProvider.GetRequiredService<ISettingsService>();
        var settingsService2 = serviceProvider.GetRequiredService<ISettingsService>();

        // Assert - Should be the same instance (Singleton)
        settingsService1.Should().BeSameAs(settingsService2);
    }

    [Fact]
    public void AllCoreServices_ShouldBeResolvableWithoutCircularDependencies()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        // Act & Assert - Should resolve all services without exceptions
        Action resolveAllServices = () =>
        {
            var audioService = serviceProvider.GetRequiredService<IAudioService>();
            var inputService = serviceProvider.GetRequiredService<IInputService>();
            var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

            // Verify they're not null
            audioService.Should().NotBeNull();
            inputService.Should().NotBeNull();
            settingsService.Should().NotBeNull();
        };

        resolveAllServices.Should().NotThrow();
    }

    [Fact]
    public void AudioService_BasicOperation_ShouldWorkThroughDI()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        var audioService = serviceProvider.GetRequiredService<IAudioService>();

        // Act & Assert - Basic operations should not throw
        // Note: These will likely fail because we don't have Godot context in tests,
        // but they should fail gracefully with Fin<T> error results, not exceptions
        Action testBasicOperations = () =>
        {
            var stopResult = audioService.StopAll();
            ((object)stopResult).Should().NotBeNull(); // Should return a result, not throw

            var volumeResult = audioService.SetBusVolume(AudioBus.Master, 0.8f);
            ((object)volumeResult).Should().NotBeNull(); // Should return a result, not throw
        };

        testBasicOperations.Should().NotThrow();
    }

    [Fact]
    public void SettingsService_BasicOperation_ShouldWorkThroughDI()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

        // Act & Assert - Basic operations should work
        Action testBasicOperations = () =>
        {
            // Get should work and return defaults
            var masterVolume = settingsService.Get(GameSettings.MasterVolume);
            masterVolume.Should().Be(1.0f);

            // Set should work
            var setResult = settingsService.Set(GameSettings.MasterVolume, 0.7f);
            setResult.IsSucc.Should().BeTrue();

            var newVolume = settingsService.Get(GameSettings.MasterVolume);
            newVolume.Should().Be(0.7f);
        };

        testBasicOperations.Should().NotThrow();
    }

    [Fact]
    public void ServiceRegistration_ShouldFollowADR006SelectiveAbstraction()
    {
        // Arrange
        var result = GameStrapper.Initialize();
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper initialization failed: {error}")
        );

        // Assert - Only services that meet ADR-006 criteria should be registered

        // ✅ These SHOULD be registered (per ADR-006 decision matrix)
        serviceProvider.GetService<IAudioService>().Should().NotBeNull("Audio needs testing, platform differences");
        serviceProvider.GetService<IInputService>().Should().NotBeNull("Input needs remapping, replay, platforms");
        serviceProvider.GetService<ISettingsService>().Should().NotBeNull("Settings need platform differences");

        // ❌ These should NOT be registered (if they existed, which they shouldn't per ADR-006)
        // This test documents what we deliberately chose NOT to abstract
        serviceProvider.GetService<ITweenService>().Should().BeNull("Tweens are presentation-only");
        serviceProvider.GetService<IParticleService>().Should().BeNull("Particles are visual-only");
        serviceProvider.GetService<ILabelService>().Should().BeNull("UI controls are already in View layer");
    }

    // Mock interfaces that should NOT exist (for documentation purposes)
    private interface ITweenService { }
    private interface IParticleService { }
    private interface ILabelService { }
}
