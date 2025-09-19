using System;
using Microsoft.Extensions.DependencyInjection;

namespace Darklands.Application.Common;

/// <summary>
/// Interface for managing service scopes tied to specific nodes or contexts.
/// This abstracts the platform-specific scope management implementation.
/// </summary>
public interface IScopeManager : IDisposable
{
    /// <summary>
    /// Creates a new service scope for the specified node/context.
    /// </summary>
    /// <param name="node">The node or context to create a scope for</param>
    /// <returns>Service scope, or null if scope creation fails</returns>
    IServiceScope? CreateScope(object node);

    /// <summary>
    /// Disposes the service scope associated with the specified node/context.
    /// </summary>
    /// <param name="node">The node or context to dispose the scope for</param>
    void DisposeScope(object node);

    /// <summary>
    /// Gets the service provider for the specified node/context.
    /// </summary>
    /// <param name="node">The node or context to get the provider for</param>
    /// <returns>Service provider for the node</returns>
    IServiceProvider GetProviderForNode(object node);

    /// <summary>
    /// Gets diagnostic information about the scope manager.
    /// </summary>
    /// <returns>Diagnostic information</returns>
    ScopeManagerDiagnostics GetDiagnostics();
}
