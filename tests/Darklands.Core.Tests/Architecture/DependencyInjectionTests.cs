using Darklands.Core.Features.Health.Application.Events;
using Darklands.Core.Infrastructure.Events;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Darklands.Core.Tests.Architecture;

/// <summary>
/// Architecture tests validating DI registration patterns and preventing regressions from VS_004 post-mortem.
/// These tests enforce lessons learned from debugging MediatR registration during the Walking Skeleton implementation.
/// </summary>
/// <remarks>
/// SCOPE: Core layer MediatR registration only.
/// Presentation layer services (LoggingService, GodotEventBus) cannot be tested from Core.Tests
/// as they require Godot runtime and reside in the Presentation layer.
///
/// VS_004 POST-MORTEM LESSON - MediatR Double-Registration Bug:
///    - Assembly scan found UIEventForwarder (it's in Core)
///    - Open generic registration added UIEventForwarder AGAIN
///    - Result: TWO instances = duplicate events fired!
///    - Fix: Use ONLY assembly scan, remove open generic
///
/// References:
/// - VS_004 Implementation (EventBus + UIEventForwarder)
/// - Main.cs lines 82-87 (MediatR configuration with post-mortem comments)
/// - Docs/01-Active/Backlog.md (VS_004 completed with lessons learned)
/// </remarks>
[Trait("Category", "Architecture")]
public class DependencyInjectionTests
{
    [Fact]
    public void MediatR_ShouldNotDoubleRegisterEventHandlers()
    {
        // VS_004 POST-MORTEM: Double-registration caused duplicate events
        //
        // ROOT CAUSE:
        // - cfg.RegisterServicesFromAssembly() scans Core and finds UIEventForwarder
        // - services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>)) adds it AGAIN
        //
        // SYMPTOM:
        // - Two UIEventForwarder instances registered
        // - HealthChangedEvent published once → TWO health bar updates!
        //
        // FIX:
        // - Removed open generic registration (Main.cs line 97 removed)
        // - Now assembly scan is the ONLY registration source
        //
        // This test prevents regression by validating count == 1

        var provider = TestServiceProviderFactory.Build();

        var handlers = provider.GetServices<INotificationHandler<HealthChangedEvent>>();

        handlers.Should().HaveCount(1,
            because: "UIEventForwarder should only be registered once via assembly scan (VS_004 post-mortem fix)");
    }

    [Fact]
    public void MediatR_ShouldRegisterUIEventForwarderViaAssemblyScan()
    {
        // WHY: Validates that assembly scan correctly finds and registers UIEventForwarder.
        // This is the ONLY registration mechanism after VS_004 fix.

        var provider = TestServiceProviderFactory.Build();

        var handler = provider.GetService<INotificationHandler<HealthChangedEvent>>();

        handler.Should().NotBeNull("UIEventForwarder should be registered via assembly scan");
        handler.Should().BeOfType<UIEventForwarder<HealthChangedEvent>>(
            "Assembly scan should find UIEventForwarder<T> and register it");
    }

    // NOTE: Presentation Layer Service Registration Tests
    //
    // The following services are registered in Main.cs (Presentation layer)
    // but cannot be tested from Core.Tests as they require Godot runtime:
    //
    // - LoggingService (must be Singleton for shared filter state)
    // - GodotEventBus (must be Singleton for shared subscription state)
    //
    // VS_004 POST-MORTEM LESSON:
    // - ALL services used by Godot autoloads MUST be registered in Main.cs
    // - ServiceLocator is only used at Godot boundary (_Ready methods)
    // - Missing registrations cause runtime exceptions: "Service not registered"
    //
    // These services are validated manually during Phase 3/4 integration testing
    // and by Main.cs startup (BuildServiceProvider will throw if dependencies are missing).

    [Fact]
    public void AllRegisteredServices_ShouldBeResolvable()
    {
        // WHY: Validates DI configuration health - no missing dependencies.
        // If any service has unregistered constructor dependencies, BuildServiceProvider() throws.
        // This test catches misconfigured DI early in development.

        var buildAction = () => TestServiceProviderFactory.Build();

        buildAction.Should().NotThrow(
            because: "All registered services should have their dependencies satisfied");

        var provider = buildAction();
        provider.Should().NotBeNull("ServiceProvider should build successfully");
    }
}
