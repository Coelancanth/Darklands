using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;
using Darklands.Domain.Common;
using Darklands.Application.Infrastructure.DependencyInjection;

namespace Darklands.Core.Tests.Architecture;

/// <summary>
/// Architecture tests that enforce ADR compliance at compile/test time.
/// These tests prevent architectural drift and ensure code follows established patterns.
/// </summary>
[Collection("GameStrapper")]
public class AdrComplianceTests
{
    private readonly Assembly _coreAssembly = typeof(GameStrapper).Assembly;
    private readonly Assembly _domainAssembly = typeof(IPersistentEntity).Assembly;

    #region ADR-004: Deterministic Simulation Tests

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-004")]
    public void Domain_Should_Not_Use_System_Random()
    {
        // ADR-004: All randomness must flow through IDeterministicRandom
        var violations = FindTypeUsages(_coreAssembly, typeof(System.Random))
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .ToList();

        violations.Should().BeEmpty(
            $"ADR-004 violation: Domain must use IDeterministicRandom, not System.Random. " +
            $"Found in: {string.Join(", ", violations.Select(t => t.Name))}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-004")]
    public void Domain_Should_Not_Use_DateTime_Now()
    {
        // ADR-004: No wall clock time - use game time instead
        var domainTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true);

        var violations = new List<string>();

        foreach (var type in domainTypes)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static |
                                         BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                // Note: We can't inspect IL easily in runtime tests
                // This serves as documentation of the constraint
                // Real detection would use Roslyn analyzers (TD_029)

                // Check for DateTime properties that might indicate usage
                if (method.ReturnType == typeof(DateTime) &&
                    (method.Name.Contains("Now") || method.Name.Contains("Today") || method.Name.Contains("UtcNow")))
                {
                    violations.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"ADR-004 violation: Domain must not use DateTime.Now/Today/UtcNow. " +
            $"Use IGameClock or deterministic time. Found in: {string.Join(", ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-004")]
    public void Domain_Should_Not_Use_Float_In_Gameplay_Logic()
    {
        // ADR-004: No floating-point arithmetic in gameplay - use Fixed
        var gameplayTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .Where(t => !t.Name.Contains("Fixed") && !t.Name.Contains("Test")) // Exclude Fixed itself
            .ToList();

        var violations = new List<string>();

        foreach (var type in gameplayTypes)
        {
            // Check properties
            var floatProperties = type.GetProperties()
                .Where(p => p.PropertyType == typeof(float) ||
                           p.PropertyType == typeof(double) ||
                           p.PropertyType == typeof(decimal))
                .Select(p => $"{type.Name}.{p.Name}")
                .ToList();

            violations.AddRange(floatProperties);

            // Check method parameters and returns
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static |
                                         BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                if (method.ReturnType == typeof(float) ||
                    method.ReturnType == typeof(double) ||
                    method.ReturnType == typeof(decimal))
                {
                    violations.Add($"{type.Name}.{method.Name} (return type)");
                }

                var floatParams = method.GetParameters()
                    .Where(p => p.ParameterType == typeof(float) ||
                               p.ParameterType == typeof(double) ||
                               p.ParameterType == typeof(decimal))
                    .Select(p => $"{type.Name}.{method.Name}({p.Name})")
                    .ToList();

                violations.AddRange(floatParams);
            }
        }

        // Allow some exceptions for conversion methods and UI/audio (not gameplay logic)
        // Also allow ShadowcastingFOV as it needs fractional slopes for geometric calculations
        var allowedPatterns = new[] { "ToFloat", "FromFloat", "ConvertTo", "ConvertFrom",
            "IAudioService", "ISettingsService", "HealthPercentage", "EuclideanDistance",
            "ShadowcastingFOV.CastShadow" };
        violations = violations.Where(v => !allowedPatterns.Any(p => v.Contains(p))).ToList();

        violations.Should().BeEmpty(
            $"ADR-004 violation: Domain must use Fixed-point math, not floating-point. " +
            $"Found in: {string.Join(", ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-004")]
    public void Domain_Should_Not_Use_String_GetHashCode_For_Persistence()
    {
        // ADR-004: string.GetHashCode() is non-deterministic across processes
        // This is mainly documentation - real detection needs IL inspection
        // Domain types have been moved to separate Domain assembly as part of TD_046
        var persistentTypes = _domainAssembly.GetTypes()
            .Where(t => typeof(IPersistentEntity).IsAssignableFrom(t))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        persistentTypes.Should().NotBeEmpty("Should have persistent entities to validate");

        // Note: Can't easily detect GetHashCode calls in compiled code
        // This test documents the constraint for manual review
        // Real implementation would use Roslyn analyzer (TD_029)
    }

    #endregion

    #region ADR-005: Save-Ready Architecture Tests

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-005")]
    public void Persistent_Entities_Should_Not_Have_Circular_References()
    {
        // ADR-005: Reference by ID, not object references
        var entityTypes = _coreAssembly.GetTypes()
            .Where(t => typeof(IPersistentEntity).IsAssignableFrom(t))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var entityType in entityTypes)
        {
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                // Check if property type is another entity (potential circular reference)
                if (typeof(IPersistentEntity).IsAssignableFrom(property.PropertyType) &&
                    !property.PropertyType.Name.Contains("Id"))
                {
                    violations.Add($"{entityType.Name}.{property.Name} references {property.PropertyType.Name} directly");
                }
            }
        }

        violations.Should().BeEmpty(
            $"ADR-005 violation: Entities must reference by ID, not direct object references. " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-005")]
    public void Domain_Entities_Should_Not_Have_Delegates_Or_Events()
    {
        // ADR-005: No delegates or events in domain entities (can't serialize)
        // Only check actual entity types that would be persisted
        var entityTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .Where(t => !t.IsInterface && !t.IsAbstract && !t.IsEnum)
            .Where(t =>
                // Check if it's a persistent entity or looks like an entity
                typeof(IPersistentEntity).IsAssignableFrom(t) ||
                t.Name.EndsWith("Entity") ||
                t.Name == "Actor" ||
                t.Name == "DummyActor" ||
                t.Name == "Grid" ||
                t.Name.EndsWith("State") ||
                (t.IsValueType && t.Name.EndsWith("Id")) // Value object IDs
            )
            .ToList();

        var violations = new List<string>();

        foreach (var entityType in entityTypes)
        {
            // Check for delegate/event fields (exclude compiler-generated)
            var delegateFields = entityType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => typeof(Delegate).IsAssignableFrom(f.FieldType))
                .Where(f => !f.Name.StartsWith("<>")) // Exclude compiler-generated fields
                .Select(f => $"{entityType.Name}.{f.Name}")
                .ToList();

            violations.AddRange(delegateFields);

            // Check for events
            var events = entityType.GetEvents()
                .Select(e => $"{entityType.Name}.{e.Name} (event)")
                .ToList();

            violations.AddRange(events);

            // Check for Action/Func properties
            var delegateProperties = entityType.GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                           (p.PropertyType.GetGenericTypeDefinition() == typeof(Action<>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(Func<,>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(Func<,,>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(Func<,,,>) ||
                            p.PropertyType.GetGenericTypeDefinition() == typeof(Action)))
                .Where(p => !p.Name.StartsWith("<>")) // Exclude compiler-generated
                .Select(p => $"{entityType.Name}.{p.Name}")
                .ToList();

            violations.AddRange(delegateProperties);
        }

        violations.Should().BeEmpty(
            $"ADR-005 violation: Domain entities cannot have delegates/events (not serializable). " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-005")]
    public void Domain_Should_Not_Have_Static_Mutable_State()
    {
        // ADR-005: No static mutable state (breaks save/load consistency)
        var domainTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .Where(t => !t.IsInterface && !t.IsEnum)
            .ToList();

        var violations = new List<string>();

        foreach (var type in domainTypes)
        {
            var staticFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(f => !f.IsInitOnly && !f.IsLiteral) // Allow readonly and const
                .Where(f => !f.Name.Contains("BackingField")) // Exclude compiler-generated
                .Where(f => !f.Name.StartsWith("<>")) // Exclude compiler-generated lambdas
                .Select(f => $"{type.Name}.{f.Name}")
                .ToList();

            violations.AddRange(staticFields);

            var staticProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(p => p.CanWrite && p.SetMethod != null)
                .Select(p => $"{type.Name}.{p.Name}")
                .ToList();

            violations.AddRange(staticProperties);
        }

        violations.Should().BeEmpty(
            $"ADR-005 violation: Domain cannot have static mutable state. " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-005")]
    public void Persistent_Entities_Should_Not_Reference_Godot_Types()
    {
        // ADR-005: No framework types in domain entities
        var entityTypes = _coreAssembly.GetTypes()
            .Where(t => typeof(IPersistentEntity).IsAssignableFrom(t))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var entityType in entityTypes)
        {
            // Check properties for Godot types
            var properties = entityType.GetProperties()
                .Where(p => p.PropertyType.FullName?.Contains("Godot") == true)
                .Select(p => $"{entityType.Name}.{p.Name}: {p.PropertyType.Name}")
                .ToList();

            violations.AddRange(properties);

            // Check fields for Godot types
            var fields = entityType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.FieldType.FullName?.Contains("Godot") == true)
                .Select(f => $"{entityType.Name}.{f.Name}: {f.FieldType.Name}")
                .ToList();

            violations.AddRange(fields);

            // Check nested generic types
            foreach (var property in entityType.GetProperties())
            {
                if (property.PropertyType.IsGenericType)
                {
                    var genericArgs = property.PropertyType.GetGenericArguments();
                    var godotGenericArgs = genericArgs
                        .Where(g => g.FullName?.Contains("Godot") == true)
                        .Select(g => $"{entityType.Name}.{property.Name} contains {g.Name}")
                        .ToList();

                    violations.AddRange(godotGenericArgs);
                }
            }
        }

        violations.Should().BeEmpty(
            $"ADR-005 violation: Persistent entities cannot reference Godot types. " +
            $"Found: {string.Join("; ", violations)}");
    }

    #endregion

    #region ADR-006: Selective Abstraction Tests

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-006")]
    public void Core_Interfaces_Should_Not_Expose_Godot_Types()
    {
        // ADR-006: Use Core value objects at boundaries, not Godot types
        var coreInterfaces = _coreAssembly.GetTypes()
            .Where(t => t.IsInterface)
            .Where(t => t.Namespace?.Contains("Core") == true)
            .ToList();

        var violations = new List<string>();

        foreach (var interfaceType in coreInterfaces)
        {
            var methods = interfaceType.GetMethods();

            foreach (var method in methods)
            {
                // Check return type
                if (method.ReturnType.FullName?.Contains("Godot") == true)
                {
                    violations.Add($"{interfaceType.Name}.{method.Name} returns {method.ReturnType.Name}");
                }

                // Check parameters
                var godotParams = method.GetParameters()
                    .Where(p => p.ParameterType.FullName?.Contains("Godot") == true)
                    .Select(p => $"{interfaceType.Name}.{method.Name}({p.Name}: {p.ParameterType.Name})")
                    .ToList();

                violations.AddRange(godotParams);
            }
        }

        violations.Should().BeEmpty(
            $"ADR-006 violation: Core interfaces must use Core value types, not Godot types. " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-006")]
    public void Application_Layer_Should_Only_Use_Interfaces_For_Infrastructure()
    {
        // ADR-006: Application layer should depend on interfaces, not concrete implementations
        var applicationTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Application") == true)
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var appType in applicationTypes)
        {
            // Check constructor dependencies
            var constructors = appType.GetConstructors();

            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();

                foreach (var param in parameters)
                {
                    // Skip primitive types and value types
                    if (param.ParameterType.IsPrimitive || param.ParameterType.IsValueType)
                        continue;

                    // Skip system types
                    if (param.ParameterType.Namespace?.StartsWith("System") == true)
                        continue;

                    // Skip MediatR types
                    if (param.ParameterType.Namespace?.Contains("MediatR") == true)
                        continue;

                    // Skip LanguageExt types
                    if (param.ParameterType.Namespace?.Contains("LanguageExt") == true)
                        continue;

                    // Infrastructure dependencies should be interfaces
                    if (!param.ParameterType.IsInterface &&
                        param.ParameterType.Namespace?.Contains("Infrastructure") == true)
                    {
                        violations.Add($"{appType.Name} depends on concrete type {param.ParameterType.Name}");
                    }
                }
            }
        }

        // Allow some specific exceptions
        var allowedConcreteTypes = new[] { "GameStrapper", "SaveReadyValidator" };
        violations = violations.Where(v => !allowedConcreteTypes.Any(t => v.Contains(t))).ToList();

        violations.Should().BeEmpty(
            $"ADR-006 violation: Application layer should use interfaces for infrastructure dependencies. " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("ADR", "ADR-006")]
    public void Domain_Should_Not_Reference_Application_Or_Infrastructure()
    {
        // ADR-006: Domain must be independent of outer layers
        var domainTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .ToList();

        var violations = new List<string>();

        foreach (var domainType in domainTypes)
        {
            // Check for references to Application layer
            var referencedTypes = GetReferencedTypes(domainType);

            var applicationReferences = referencedTypes
                .Where(t => t.Namespace?.Contains("Application") == true)
                .Select(t => $"{domainType.Name} references {t.Name} from Application layer")
                .ToList();

            violations.AddRange(applicationReferences);

            var infrastructureReferences = referencedTypes
                .Where(t => t.Namespace?.Contains("Infrastructure") == true)
                .Select(t => $"{domainType.Name} references {t.Name} from Infrastructure layer")
                .ToList();

            violations.AddRange(infrastructureReferences);
        }

        violations.Should().BeEmpty(
            $"ADR-006 violation: Domain layer must not reference Application or Infrastructure layers. " +
            $"Found: {string.Join("; ", violations)}");
    }

    #endregion

    #region Forbidden Pattern Detection

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Category", "ForbiddenPatterns")]
    public void Domain_Should_Not_Use_Threading_Primitives()
    {
        // Domain should be single-threaded for determinism
        var domainTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .ToList();

        var violations = new List<string>();

        var threadingTypes = new[]
        {
            typeof(System.Threading.Thread),
            typeof(System.Threading.Tasks.Task),
            typeof(System.Threading.ThreadPool),
            typeof(System.Threading.Mutex),
            typeof(System.Threading.Semaphore),
            typeof(System.Threading.ReaderWriterLock)
        };

        foreach (var domainType in domainTypes)
        {
            var referencedTypes = GetReferencedTypes(domainType);

            foreach (var threadingType in threadingTypes)
            {
                if (referencedTypes.Contains(threadingType))
                {
                    violations.Add($"{domainType.Name} uses {threadingType.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Domain must not use threading primitives (breaks determinism). " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Category", "ForbiddenPatterns")]
    public void Domain_Should_Not_Perform_IO_Operations()
    {
        // Domain should not directly perform I/O
        var domainTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .ToList();

        var violations = new List<string>();

        var ioTypes = new[]
        {
            typeof(System.IO.File),
            typeof(System.IO.Directory),
            typeof(System.IO.FileStream),
            typeof(System.IO.StreamReader),
            typeof(System.IO.StreamWriter),
            typeof(System.Net.Http.HttpClient),
            typeof(System.Net.WebClient)
        };

        foreach (var domainType in domainTypes)
        {
            var referencedTypes = GetReferencedTypes(domainType);

            foreach (var ioType in ioTypes)
            {
                if (referencedTypes.Contains(ioType))
                {
                    violations.Add($"{domainType.Name} uses {ioType.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Domain must not perform I/O operations directly. " +
            $"Found: {string.Join("; ", violations)}");
    }

    [Fact]
    [Trait("Category", "Architecture")]
    [Trait("Category", "ForbiddenPatterns")]
    public void Domain_Should_Not_Use_Console_Or_Debug_Output()
    {
        // Domain should not have direct console/debug output
        var domainTypes = _coreAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Domain") == true)
            .ToList();

        var violations = new List<string>();

        foreach (var domainType in domainTypes)
        {
            var methods = domainType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                               BindingFlags.Instance | BindingFlags.Static |
                                               BindingFlags.DeclaredOnly);

            foreach (var method in methods)
            {
                // Check for Console or Debug usage in method names (heuristic)
                // Exclude PrintMembers which is generated by records
                if ((method.Name.Contains("Console") || method.Name.Contains("Debug") ||
                    method.Name.Contains("WriteLine") || method.Name.Contains("Print")) &&
                    !method.Name.Equals("PrintMembers")) // Exclude record-generated method
                {
                    violations.Add($"{domainType.Name}.{method.Name}");
                }
            }
        }

        violations.Should().BeEmpty(
            $"Domain must not use Console/Debug output directly. Use logging abstractions. " +
            $"Found: {string.Join("; ", violations)}");
    }

    #endregion

    #region Helper Methods

    private static IEnumerable<Type> FindTypeUsages(Assembly assembly, Type targetType)
    {
        var types = new List<Type>();

        foreach (var type in assembly.GetTypes())
        {
            // Check if type inherits from target
            if (targetType.IsAssignableFrom(type) && type != targetType)
            {
                types.Add(type);
                continue;
            }

            // Check fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Instance | BindingFlags.Static);
            if (fields.Any(f => f.FieldType == targetType))
            {
                types.Add(type);
                continue;
            }

            // Check properties
            var properties = type.GetProperties();
            if (properties.Any(p => p.PropertyType == targetType))
            {
                types.Add(type);
                continue;
            }

            // Check method parameters and returns
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                         BindingFlags.Instance | BindingFlags.Static);
            foreach (var method in methods)
            {
                if (method.ReturnType == targetType ||
                    method.GetParameters().Any(p => p.ParameterType == targetType))
                {
                    types.Add(type);
                    break;
                }
            }
        }

        return types.Distinct();
    }

    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        var referencedTypes = new HashSet<Type>();

        // Check base type
        if (type.BaseType != null)
            referencedTypes.Add(type.BaseType);

        // Check implemented interfaces
        foreach (var interfaceType in type.GetInterfaces())
            referencedTypes.Add(interfaceType);

        // Check fields
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.Static);
        foreach (var field in fields)
            referencedTypes.Add(field.FieldType);

        // Check properties
        foreach (var property in type.GetProperties())
            referencedTypes.Add(property.PropertyType);

        // Check method signatures
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.Static |
                                     BindingFlags.DeclaredOnly);
        foreach (var method in methods)
        {
            referencedTypes.Add(method.ReturnType);
            foreach (var param in method.GetParameters())
                referencedTypes.Add(param.ParameterType);
        }

        // Check constructor parameters
        foreach (var constructor in type.GetConstructors())
        {
            foreach (var param in constructor.GetParameters())
                referencedTypes.Add(param.ParameterType);
        }

        return referencedTypes;
    }

    #endregion
}
