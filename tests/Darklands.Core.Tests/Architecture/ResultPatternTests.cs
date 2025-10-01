using System.Reflection;
using CSharpFunctionalExtensions;
using Darklands.Core.Features.Health.Application.Commands;
using FluentAssertions;
using MediatR;
using NetArchTest.Rules;
using Xunit;

namespace Darklands.Core.Tests.Architecture;

/// <summary>
/// Architecture tests enforcing ADR-003: Functional Error Handling with CSharpFunctionalExtensions.
/// Validates that all command/query handlers return Result&lt;T&gt; for functional error handling.
/// </summary>
/// <remarks>
/// WHY: ADR-003 mandates Result&lt;T&gt; pattern for all operations that can fail.
/// This prevents hidden failures and enforces explicit error handling at compile-time.
///
/// PATTERN: Railway-Oriented Programming
/// - Success track → Success track
/// - Failure track → stays on failure (short-circuits)
/// - No exceptions for business logic
///
/// References:
/// - ADR-003: Functional Error Handling
/// - Docs/03-Reference/ADR/ADR-003-functional-error-handling.md
/// </remarks>
[Trait("Category", "Architecture")]
public class ResultPatternTests
{
    // Cache assemblies for performance
    private static readonly Assembly CoreAssembly = typeof(TakeDamageCommand).Assembly;

    [Fact]
    public void CommandHandlers_ShouldReturnTaskResult()
    {
        // WHY (ADR-003): All command handlers must return Task<Result> or Task<Result<T>>
        // This makes failure modes explicit in the signature and forces callers to handle errors.
        //
        // RAILWAY-ORIENTED PROGRAMMING:
        // - Result<T> represents success/failure without exceptions
        // - Failures short-circuit (no need for try-catch in every layer)
        // - Composable via Bind/Map/Tap
        //
        // MEDIATR PATTERN:
        // - IRequestHandler<TRequest, TResponse> where TResponse is Result or Result<T>
        // - Handle method signature is: Task<TResponse> Handle(TRequest, CancellationToken)
        // - So Handle returns Task<Result> or Task<Result<T>>

        var handlers = Types.InAssembly(CoreAssembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .GetTypes();

        handlers.Should().NotBeEmpty("Core assembly should have command handlers");

        foreach (var handler in handlers)
        {
            // Get IRequestHandler<TRequest, TResponse> interface
            var requestHandlerInterface = handler.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            requestHandlerInterface.Should().NotBeNull($"{handler.Name} should implement IRequestHandler<,>");

            // Get TResponse (second generic argument of IRequestHandler<TRequest, TResponse>)
            // NOTE: TResponse IS Result or Result<T> (NOT Task<Result>!)
            // MediatR's Handle method returns Task<TResponse>
            var responseType = requestHandlerInterface!.GetGenericArguments()[1];

            // Validate TResponse is Result or Result<T>
            ValidateIsResult(responseType, handler.Name);
        }
    }

    [Fact]
    public void Commands_ShouldImplementIRequestWithResult()
    {
        // WHY (ADR-003): Commands should declare their return type as IRequest<Result<T>>
        // This enforces the functional pattern at the command level.

        var commands = Types.InAssembly(CoreAssembly)
            .That().ImplementInterface(typeof(IRequest<>))
            .And().HaveNameEndingWith("Command")
            .GetTypes();

        commands.Should().NotBeEmpty("Core assembly should have commands");

        foreach (var command in commands)
        {
            // Get IRequest<TResponse> interface
            var requestInterface = command.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRequest<>));

            requestInterface.Should().NotBeNull($"{command.Name} should implement IRequest<>");

            // Get TResponse
            var responseType = requestInterface!.GetGenericArguments()[0];

            // Validate TResponse is Result or Result<T>
            ValidateIsResult(responseType, command.Name);
        }
    }

    /// <summary>
    /// Validates that a type is Result or Result&lt;T&gt;.
    /// </summary>
    private static void ValidateIsResult(Type responseType, string typeName)
    {
        // Should be Result or Result<T>
        var isResult = responseType.IsGenericType
            ? responseType.GetGenericTypeDefinition() == typeof(Result<>)
            : responseType == typeof(Result);

        isResult.Should().BeTrue(
            because: $"{typeName} should return Result or Result<T> (ADR-003), but returns {responseType.Name}");
    }
}
