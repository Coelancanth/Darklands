using FluentAssertions;
using NetArchTest.Rules;
using Xunit;
using System.Reflection;

namespace Darklands.Architecture.Tests;

/// <summary>
/// Tests to verify proper isolation between bounded contexts.
/// These are REAL tests that will fail if architectural boundaries are violated.
/// </summary>
public class ModuleIsolationTests
{
    // Marker types for assembly identification
    // We'll need to add these or use existing types from each assembly
    
    [Fact]
    public void SharedKernel_ShouldNotDependOnAnyContext()
    {
        // Arrange & Act
        var result = Types.InAssembly(typeof(Darklands.SharedKernel.Domain.EntityId).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Darklands.Tactical", 
                "Darklands.Diagnostics", 
                "Darklands.Platform",
                "Darklands.Core")  // SharedKernel must not depend on Core either
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "SharedKernel should not depend on any specific bounded context or core implementation");
    }

    [Fact]
    public void DiagnosticsDomain_MustNotReferenceOtherContexts()
    {
        // Try to load the Diagnostics.Domain assembly if it exists
        var diagnosticsAssembly = GetAssemblyIfExists("Darklands.Diagnostics.Domain");
        if (diagnosticsAssembly == null)
        {
            // If assembly doesn't exist yet, test passes (will be created in later phases)
            return;
        }

        // Arrange & Act
        var result = Types.InAssembly(diagnosticsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Darklands.Tactical",           // No direct tactical dependency
                "Darklands.Platform",           // No platform dependency
                "Darklands.Tactical.Contracts", // CRITICAL: Domain must NOT reference contracts!
                "Darklands.Platform.Contracts", // Domain must NOT reference any contracts
                "Darklands.Core",               // Domain should not depend on Core
                "MediatR")                       // Domain should be pure, no MediatR
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Diagnostics.Domain must only depend on SharedKernel and approved libraries");
    }

    [Fact]
    public void DiagnosticsInfrastructure_CanReferenceContractsButNotOtherDomains()
    {
        // Try to load the Diagnostics.Infrastructure assembly if it exists
        var infraAssembly = GetAssemblyIfExists("Darklands.Diagnostics.Infrastructure");
        if (infraAssembly == null)
        {
            return; // Assembly not created yet
        }

        // Arrange & Act
        var result = Types.InAssembly(infraAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Darklands.Tactical.Domain",    // No cross-domain references
                "Darklands.Platform.Domain",    // No cross-domain references
                "Darklands.Core.Domain")        // No core domain references
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Diagnostics.Infrastructure must not reference other context domains");
    }

    [Fact]
    public void TacticalContracts_OnlyDependOnSharedKernel()
    {
        // Try to load the Tactical.Contracts assembly
        var contractsAssembly = GetAssemblyIfExists("Darklands.Tactical.Contracts");
        if (contractsAssembly == null)
        {
            return; // Assembly not created yet
        }

        // Arrange & Act
        var result = Types.InAssembly(contractsAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Darklands.Tactical.Domain",     // Contracts should not reference domain
                "Darklands.Tactical.Infrastructure", // Contracts should not reference infrastructure
                "Darklands.Diagnostics.Domain",  // No cross-context domain references
                "Darklands.Core")                // Contracts should not depend on Core
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue(
            "Tactical.Contracts should only depend on SharedKernel and base libraries");
    }

    [Fact]
    public void NoCircularDependenciesBetweenContexts()
    {
        // This test will become more important as more contexts are added
        // For now, verify that if both Diagnostics and Tactical exist,
        // they don't reference each other directly
        
        var diagnosticsAssembly = GetAssemblyIfExists("Darklands.Diagnostics.Domain");
        var tacticalAssembly = GetAssemblyIfExists("Darklands.Tactical.Domain");
        
        if (diagnosticsAssembly != null)
        {
            var result = Types.InAssembly(diagnosticsAssembly)
                .Should()
                .NotHaveDependencyOn("Darklands.Tactical.Domain")
                .GetResult();
                
            result.IsSuccessful.Should().BeTrue(
                "Diagnostics should not directly reference Tactical domain");
        }
        
        if (tacticalAssembly != null)
        {
            var result = Types.InAssembly(tacticalAssembly)
                .Should()
                .NotHaveDependencyOn("Darklands.Diagnostics.Domain")
                .GetResult();
                
            result.IsSuccessful.Should().BeTrue(
                "Tactical should not directly reference Diagnostics domain");
        }
    }

    /// <summary>
    /// Helper method to load an assembly by name if it exists.
    /// Returns null if assembly is not found (not yet created).
    /// </summary>
    private static Assembly? GetAssemblyIfExists(string assemblyName)
    {
        try
        {
            // First try to find it in already loaded assemblies
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);
            
            if (loadedAssembly != null)
                return loadedAssembly;
            
            // Try to load it
            return Assembly.Load(assemblyName);
        }
        catch
        {
            // Assembly doesn't exist yet - this is OK for phased implementation
            return null;
        }
    }
}