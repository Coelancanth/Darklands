using System.Reflection;
using Darklands.Core.Features.Health.Application.Commands;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Darklands.Core.Tests.Architecture;

/// <summary>
/// Architecture tests enforcing ADR-001: Clean Architecture Three-Layer design.
/// Uses NetArchTest to validate dependency rules at compile-time via reflection.
/// </summary>
/// <remarks>
/// WHY: These tests are "living documentation" that enforces architectural rules automatically.
/// Static docs drift; tests FAIL when rules are violated, forcing immediate correction.
///
/// PERFORMANCE: Pure reflection scanning (<100ms) - no runtime overhead, no test database.
///
/// References:
/// - ADR-001: Clean Architecture Three-Layer
/// - Docs/03-Reference/ADR/ADR-001-clean-architecture-layers.md
/// </remarks>
[Trait("Category", "Architecture")]
public class LayerDependencyTests
{
    // Cache assemblies for performance (prevents repeated reflection scanning)
    private static readonly Assembly CoreAssembly = typeof(TakeDamageCommand).Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnApplication()
    {
        // WHY (ADR-001): Domain must be pure - no application logic dependencies
        // Domain is the innermost layer containing business rules and value objects.
        // It must not know about Application (use cases) or Infrastructure (I/O).

        var result = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespace("Darklands.Core.Domain")
            .ShouldNot().HaveDependencyOn("Darklands.Core.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Application layer (ADR-001)");
    }

    [Fact]
    public void Domain_ShouldNotDependOnInfrastructure()
    {
        // WHY (ADR-001): Domain must be pure - no infrastructure dependencies
        // Domain contains business logic and should be testable without I/O.

        var result = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespace("Darklands.Core.Domain")
            .ShouldNot().HaveDependencyOn("Darklands.Core.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Infrastructure layer (ADR-001)");
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructure_ExceptGameStrapper()
    {
        // WHY (ADR-001): Application defines interfaces; Infrastructure implements them.
        // Application layer orchestrates use cases via interfaces (IRepository, IEventBus).
        // Infrastructure provides concrete implementations.
        //
        // EXCEPTION: GameStrapper is the DI bootstrap - it wires interfaces to implementations.
        // This is an acceptable architectural boundary crossing.

        var result = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespace("Darklands.Core.Application")
            .And().DoNotHaveName("GameStrapper") // DI bootstrap exception
            .ShouldNot().HaveDependencyOn("Darklands.Core.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application should not depend on Infrastructure except in DI bootstrap (ADR-001)");
    }

    [Fact]
    public void Core_ShouldNotDependOnGodot()
    {
        // WHY (ADR-002): Core must have ZERO Godot dependencies for portability.
        // This ensures Core can be tested without Godot runtime and used in other contexts.
        //
        // NOTE: This is already enforced at COMPILE-TIME by .csproj SDK choice!
        // - Darklands.Core.csproj uses Microsoft.NET.Sdk (pure C#)
        // - Attempting to use Godot types in Core results in compile error
        //
        // This test provides additional validation for documentation purposes.

        var result = Types.InAssembly(CoreAssembly)
            .ShouldNot().HaveDependencyOn("Godot")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Core must have zero Godot dependencies (ADR-002) - already enforced by .csproj SDK");
    }

    [Fact]
    public void Features_ShouldFollowCleanArchitectureStructure()
    {
        // WHY (ADR-004): Feature-based organization with Clean Architecture layers.
        // Each feature has Domain/Application/Infrastructure folders.
        // This test validates the folder structure convention is followed.
        //
        // Example: Features/Health/ should have Domain/, Application/, Infrastructure/

        var featureTypes = Types.InAssembly(CoreAssembly)
            .That().ResideInNamespace("Darklands.Core.Features")
            .GetTypes();

        // Validate that feature types are in recognized layer namespaces
        foreach (var type in featureTypes)
        {
            var hasValidLayerNamespace =
                type.Namespace!.Contains(".Domain") ||
                type.Namespace.Contains(".Application") ||
                type.Namespace.Contains(".Infrastructure");

            hasValidLayerNamespace.Should().BeTrue(
                because: $"{type.FullName} should be in Domain/Application/Infrastructure namespace (ADR-004)");
        }
    }
}
