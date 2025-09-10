using Darklands.Core.Infrastructure.DependencyInjection;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Darklands.Core.Tests.Infrastructure.DependencyInjection;

/// <summary>
/// MediatR handler registration validation tests.
/// These tests ensure that:
/// 1. All handlers are in the correct namespace for auto-discovery
/// 2. All handler dependencies are registered in DI
/// 3. MediatR can discover and instantiate all handlers
///
/// Prevents runtime MediatR resolution failures by catching registration issues at test time.
/// </summary>
[Collection("GameStrapper")]
public class MediatRHandlerRegistrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Assembly _coreAssembly;

    public MediatRHandlerRegistrationTests()
    {
        // Initialize test DI container
        var config = GameStrapperConfiguration.Testing;
        var result = GameStrapper.Initialize(config);
        _serviceProvider = result.Match(
            Succ: provider => provider,
            Fail: error => throw new InvalidOperationException($"Test setup failed: {error}"));
        _coreAssembly = typeof(GameStrapper).Assembly;
    }

    [Fact]
    public void All_RequestHandlers_Should_Be_In_Correct_Namespace()
    {
        // Arrange - Find all IRequestHandler implementations
        var handlerTypes = _coreAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        // Act & Assert - Verify namespace alignment
        var incorrectNamespaces = new List<string>();

        foreach (var handlerType in handlerTypes)
        {
            var namespaceName = handlerType.Namespace ?? string.Empty;

            // CRITICAL: All handlers must be in Darklands.Core namespace hierarchy
            // This ensures MediatR's assembly scanning finds them
            if (!namespaceName.StartsWith("Darklands.Core"))
            {
                incorrectNamespaces.Add($"{handlerType.Name} in {namespaceName}");
            }
        }

        // Provide detailed error message for debugging
        incorrectNamespaces.Should().BeEmpty(
            $"All handlers must be in Darklands.Core.* namespace for MediatR auto-discovery. " +
            $"Found {incorrectNamespaces.Count} handlers with incorrect namespaces: " +
            $"{string.Join(", ", incorrectNamespaces)}");
    }

    [Fact]
    public void All_RequestHandlers_Should_Have_Dependencies_Registered()
    {
        // Arrange - Find all handler types
        var handlerTypes = _coreAssembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)) &&
                !t.IsAbstract &&
                t.Namespace?.StartsWith("Darklands.Core") == true)
            .ToList();

        var missingDependencies = new List<string>();

        // Act - Check each handler's constructor dependencies
        foreach (var handlerType in handlerTypes)
        {
            var constructors = handlerType.GetConstructors();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                foreach (var parameter in parameters)
                {
                    try
                    {
                        // Try to resolve each dependency
                        var service = _serviceProvider.GetService(parameter.ParameterType);

                        if (service == null && !parameter.HasDefaultValue)
                        {
                            missingDependencies.Add(
                                $"{handlerType.Name} requires {parameter.ParameterType.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        missingDependencies.Add(
                            $"{handlerType.Name} dependency check failed: {ex.Message}");
                    }
                }
            }
        }

        // Assert with detailed diagnostics
        missingDependencies.Should().BeEmpty(
            $"All handler dependencies must be registered. Found {missingDependencies.Count} missing: " +
            $"{string.Join("; ", missingDependencies)}");
    }

    [Fact]
    public void MediatR_Should_Discover_All_Handlers_In_Core_Assembly()
    {
        // Arrange
        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Find all command/query types (requests) - exclude interfaces and abstract classes
        var requestTypes = _coreAssembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract &&
                       t.GetInterfaces().Any(i => i == typeof(IRequest) ||
                (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))))
            .ToList();

        // Currently no requests implemented in Phase 1, but test framework is ready
        // This test will validate handlers as we implement them in Phase 2+

        // For now, validate that MediatR is configured correctly
        mediator.Should().NotBeNull("MediatR should be properly configured");

        // When we have requests in Phase 2, this will validate they have handlers
        if (requestTypes.Any())
        {
            var unmappedRequests = new List<string>();

            foreach (var requestType in requestTypes)
            {
                // Build the handler interface type
                var responseType = requestType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IRequest<>))
                    ?.GetGenericArguments()[0];

                Type handlerInterfaceType;
                if (responseType != null)
                {
                    handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
                }
                else
                {
                    // For IRequest without response type
                    handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);
                }

                // Check if handler can be resolved
                try
                {
                    var handlers = _serviceProvider.GetServices(handlerInterfaceType);
                    if (!handlers.Any())
                    {
                        unmappedRequests.Add($"{requestType.Name} has no registered handler");
                    }
                }
                catch
                {
                    unmappedRequests.Add($"{requestType.Name} handler resolution failed");
                }
            }

            unmappedRequests.Should().BeEmpty(
                $"All requests should have discoverable handlers. Found {unmappedRequests.Count} without handlers: " +
                $"{string.Join(", ", unmappedRequests)}");
        }
    }

    [Fact]
    public void Handler_Registration_Should_Not_Have_Duplicate_Implementations()
    {
        // Ensure no handler is registered multiple times (can cause resolution issues)

        var handlerInterfaces = _coreAssembly.GetTypes()
            .SelectMany(t => t.GetInterfaces())
            .Where(i => i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .Distinct()
            .ToList();

        foreach (var handlerInterface in handlerInterfaces)
        {
            var implementations = _serviceProvider.GetServices(handlerInterface).ToList();

            implementations.Count.Should().BeLessThanOrEqualTo(1,
                $"Handler interface {handlerInterface.Name} should have at most one implementation registered. " +
                $"Multiple implementations can cause ambiguous resolution.");
        }
    }

    [Fact]
    public void MediatR_Pipeline_Behaviors_Should_Be_Registered()
    {
        // Verify that our custom pipeline behaviors are registered
        var behaviorsFromDI = _serviceProvider.GetServices<IPipelineBehavior<IRequest, object>>().ToList();

        // Should have at least logging and error handling behaviors
        behaviorsFromDI.Should().NotBeEmpty(
            "MediatR pipeline should have custom behaviors (logging, error handling) registered");

        // Verify specific behaviors are present
        var behaviorTypeNames = behaviorsFromDI.Select(b => b.GetType().Name).ToList();

        behaviorTypeNames.Should().Contain(name => name.Contains("Logging"),
            "LoggingBehavior should be registered in MediatR pipeline");

        behaviorTypeNames.Should().Contain(name => name.Contains("Error"),
            "ErrorHandlingBehavior should be registered in MediatR pipeline");
    }

    // Note: GameStrapper uses singleton pattern - no per-test cleanup needed
    // Disposal handled globally at application termination
}
