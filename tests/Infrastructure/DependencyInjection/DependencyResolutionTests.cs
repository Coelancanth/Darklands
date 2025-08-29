using Darklands.Core.Infrastructure.DependencyInjection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.DependencyInjection;

/// <summary>
/// DI Resolution Tests for GameStrapper.
/// 
/// Validates that all services registered in the DI container can be resolved
/// successfully, preventing DI configuration regressions.
/// 
/// This test acts as an architectural fitness function to ensure all services 
/// remain resolvable.
/// </summary>
public class DependencyResolutionTests
{
    [Fact]
    public void GameStrapper_Should_Initialize_Successfully()
    {
        // Arrange - Use minimal configuration for testing
        var config = GameStrapperConfiguration.Testing;
        
        // Act
        var result = GameStrapper.Initialize(config);
        
        // Assert
        result.IsSucc.Should().BeTrue("GameStrapper should initialize without errors");
        
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"GameStrapper failed to initialize: {error}"));
        
        ValidateAllServicesResolvable(serviceProvider, "GameStrapper Configuration");
    }

    [Fact]
    public void All_Core_Services_Should_Be_Resolvable()
    {
        // Arrange
        var config = GameStrapperConfiguration.Testing;
        var result = GameStrapper.Initialize(config);
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"Setup failed: {error}"));
        
        // CRITICAL: Test key service interfaces that should be registered
        var serviceTypesToTest = new Type[]
        {
            // Core services
            typeof(IMediator),
            typeof(ILogger),
            typeof(Microsoft.Extensions.Logging.ILoggerFactory),
            
            // TODO: Add business services as they're implemented in future phases:
            // typeof(ICombatStateService),
            // typeof(IGameStateService),
            // typeof(ITimeUnitCalculator),
        };

        serviceTypesToTest.Should().NotBeEmpty("service interface list should contain testable services");

        // Act & Assert - Comprehensive resolution validation  
        foreach (var serviceType in serviceTypesToTest)
        {
            try
            {
                var service = serviceProvider.GetRequiredService(serviceType);
                service.Should().NotBeNull($"service '{serviceType.Name}' should be resolvable");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve service '{serviceType.Name}'. " +
                    $"Check DI registration in GameStrapper. Error: {ex.Message}", ex);
            }
        }
    }

    [Fact]
    public void GameStrapper_Should_Handle_Invalid_Configuration_Gracefully()
    {
        // Arrange - Create configuration that might cause issues
        var problematicConfig = new GameStrapperConfiguration(
            LogLevel: LogEventLevel.Fatal, // Very restrictive logging
            LogFilePath: "/invalid/path/that/should/not/exist.log",
            ValidateOnBuild: true,
            ValidateScopes: true);
        
        // Act - Should not throw, should return Fin<T> result
        var result = GameStrapper.Initialize(problematicConfig);
        
        // Assert - Even with problematic config, should initialize successfully
        // (due to fallback mechanisms)
        result.IsSucc.Should().BeTrue(
            "GameStrapper should handle invalid configuration gracefully with fallbacks");
    }

    [Fact]
    public void MediatR_Should_Be_Configured_Correctly()
    {
        // Arrange
        var config = GameStrapperConfiguration.Testing;
        var result = GameStrapper.Initialize(config);
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"Setup failed: {error}"));
        
        // Act
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        
        // Assert
        mediator.Should().NotBeNull("IMediator should be registered");
        mediator.Should().BeAssignableTo<IMediator>("Should implement IMediator interface");
    }

    [Fact]
    public void Logging_Should_Be_Configured_With_Multiple_Abstractions()
    {
        // Arrange
        var config = GameStrapperConfiguration.Testing;
        var result = GameStrapper.Initialize(config);
        var serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"Setup failed: {error}"));
        
        // Act & Assert - Should support both Serilog and Microsoft.Extensions.Logging
        var serilogLogger = serviceProvider.GetRequiredService<ILogger>();
        var msLoggerFactory = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>();
        var msLogger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DependencyResolutionTests>>();
        
        serilogLogger.Should().NotBeNull("Serilog ILogger should be registered");
        msLoggerFactory.Should().NotBeNull("Microsoft ILoggerFactory should be registered");  
        msLogger.Should().NotBeNull("Microsoft ILogger<T> should be available");
    }

    private static void ValidateAllServicesResolvable(IServiceProvider serviceProvider, string context)
    {
        // This is a basic smoke test - more specific service tests will be added as we implement features
        var basicServices = new[]
        {
            typeof(IMediator),
            typeof(ILogger),
            typeof(Microsoft.Extensions.Logging.ILoggerFactory)
        };

        foreach (var serviceType in basicServices)
        {
            try
            {
                var service = serviceProvider.GetRequiredService(serviceType);
                service.Should().NotBeNull($"Basic service '{serviceType.Name}' should be resolvable in {context}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Basic service resolution failed in {context} for '{serviceType.Name}': {ex.Message}", ex);
            }
        }
    }

    // Cleanup
    ~DependencyResolutionTests()
    {
        GameStrapper.Dispose();
    }
}