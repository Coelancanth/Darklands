# ADR-008: Functional Error Handling with LanguageExt v5

## Status
Approved (2025-08-30)

## Context

We're using LanguageExt v5.0.0-beta-54 for functional programming patterns in Darklands. Our current codebase has inconsistent error handling:
- 15+ try/catch blocks in Presentation layer (anti-pattern)
- Mixed use of exceptions and Fin<T> returns
- No clear guidelines on when to use functional vs exception-based error handling

LanguageExt v5 brings significant changes:
- **Try<A> is removed** - replaced by Eff<A> for effectful computations
- **IO<A> monad** - new unified approach for side effects and async
- **Fin<A>** - simplified error handling (isomorphic to Either<Error, A>)
- **Error type hierarchy** - Expected, Exceptional, ManyErrors

## Philosophy

Following LanguageExt's functional error handling philosophy:

1. **Pure Functions Always Return** - No exceptions, no side effects
2. **Errors Are Data** - Part of the function's type signature, not control flow
3. **Short-Circuiting Is Declarative** - Errors propagate automatically via monadic bind
4. **Railway-Oriented Programming** - Success track and failure track, with switches between them
5. **Explicit Over Implicit** - Everything that can fail is visible in the type

## Decision

We will adopt functional error handling as our primary error management strategy:

### 1. Business Logic (Domain & Application Layers)
**NEVER use try/catch**. All business operations return:
- `Fin<T>` for operations that can fail
- `Option<T>` for nullable/missing values  
- `Validation<Error, T>` for collecting multiple errors
- `Eff<T>` for effectful computations (replacing Try<T>)

### 2. Infrastructure Boundaries
**Try/catch is ONLY acceptable at**:
- Godot integration points (GameManager._Ready, _ExitTree)
- Third-party library boundaries that throw exceptions
- File I/O, network, database operations
- Top-level application entry points for containment

### 3. Presentation Layer (Presenters)
**Must use functional patterns**:
```csharp
// ❌ WRONG - Current anti-pattern
try {
    await RefreshGridDisplayAsync();
} catch (Exception ex) {
    _logger.Error(ex, "Failed");
}

// ✅ CORRECT - Functional approach
await RefreshGridDisplayAsync()
    .Match(
        Succ: _ => _logger.Information("Grid refreshed"),
        Fail: error => _logger.Error("Grid refresh failed: {Error}", error)
    );
```

### 4. Error Type Selection Guide

| Scenario | Use | Example |
|----------|-----|---------|
| Operation can fail | `Fin<T>` | `Fin<Grid> LoadGrid()` |
| Value might not exist | `Option<T>` | `Option<Actor> FindActor(id)` |
| Multiple validation errors | `Validation<Error, T>` | Form validation |
| Side effects with errors | `Eff<T>` | Database operations |
| Async I/O operations | `IO<T>` | File reading, network calls |
| Domain invariants | Return `Fin<T>` | `Fin<Position> Move(...)` |

### 5. Error Philosophy and Creation

#### Expected vs Exceptional (Critical Distinction)
- **Expected**: Part of normal flow (user not found, invalid input)
- **Exceptional**: System failures (OutOfMemory, null reference)
- **Rule**: Use Expected for recoverable, Exceptional for system failures

```csharp
// Expected errors (business logic failures)
Error.New("Actor not found");               // Simple expected error
Error.New(404, "Position out of bounds");   // With error code

// Exceptional errors (unexpected failures)  
Error.New(new InvalidOperationException("Unexpected state"));

// Multiple errors (for validation scenarios)
Error.Many(error1, error2);  // or error1 + error2
```

### 6. Railway-Oriented Programming (Recovery Patterns)

#### The Pipe Operator | for Error Recovery
```csharp
// Try multiple sources in order
Fin<Actor> GetActor(ActorId id) =>
    GetFromCache(id)           // Try cache first
    | GetFromDatabase(id)       // Fall back to database
    | GetDefaultActor();        // Final fallback

// With specific error handling
var result = ParseInt(input)
    | @catch(Error.New("Invalid"), _ => Pure(0))  // Default on specific error
    | Pure(-1);                                    // Final fallback
```

#### The @catch Operator for Selective Recovery
```csharp
// Catch all errors and transform
var result = DangerousOperation()
    | @catch(err => Pure(SafeDefault()));

// Catch specific error types
var result = NetworkCall()
    | @catch(err => err.Is<TimeoutException>(), 
             err => RetryWithBackoff())
    | @catch(err => err.Code == 404,
             err => Pure(NotFoundDefault()));

// Catch and transform error message
var result = Operation()
    | @catch(err => err.IsExpected,
             err => Fail(Error.New($"User-friendly: {err}")));
```

### 7. Migration from v4 Patterns
- Replace `Try<T>` with `Eff<T>`
- Replace `TryAsync<T>` with `IO<T>` or `Eff<T>`
- Replace `Result<T>` with `Fin<T>`
- Replace `EitherAsync<L,R>` with `EitherT<L, IO, R>`
- Replace `OptionAsync<T>` with `OptionT<IO, T>`

## Consequences

### Positive
- **Type-safe error handling** - Errors visible in method signatures
- **Composable error flows** - Chain operations with LINQ/monadic bind
- **No hidden exceptions** - All failure paths explicit
- **Better testing** - Pure functions easier to test
- **Reduced debugging** - Errors are data, not control flow
- **Aligned with v5** - Using modern LanguageExt patterns

### Negative  
- **Learning curve** - Team needs to understand functional patterns
- **Verbose signatures** - `Fin<T>` everywhere in business logic
- **Migration effort** - ~6 hours to fix existing anti-patterns
- **Performance overhead** - Minor allocation cost for monadic wrappers

### Neutral
- **Different paradigm** - Shifts from imperative to functional error handling
- **Tooling support** - IDE may not understand monadic patterns as well

## Implementation Guidelines

### Phase 1: Fix Critical Anti-patterns (TD_004)
1. Convert all Presenter try/catch to Match patterns
2. Update Domain methods to return Fin<T>
3. Fix Application handlers to use Fin<T> consistently

### Phase 2: Establish Patterns
1. Create example error handling in VS_008
2. Document patterns in HANDBOOK.md
3. Add architecture tests to enforce rules

### Phase 3: Tooling Support
1. Add analyzer rules to warn on try/catch in Domain/Application
2. Create code snippets for common patterns
3. Consider custom Roslyn analyzer

## Examples

### Domain Layer
```csharp
public static Fin<Grid> CreateGrid(int width, int height) =>
    width > 0 && height > 0
        ? Pure(new Grid(width, height))
        : Fail(Error.New("Invalid grid dimensions"));
```

### Application Layer  
```csharp
public Task<Fin<Unit>> Handle(MoveActorCommand request, CancellationToken ct) =>
    from current in GetActorPosition(request.ActorId)
        .ToFin(Error.New("Actor not found"))
    from valid in ValidateMove(current, request.ToPosition)
    from moved in MoveActor(request.ActorId, request.ToPosition)
    select unit;
```

### Presentation Layer
```csharp
public async Task HandleTileClickAsync(Position position) =>
    await MoveActor(position).Match(
        Succ: _ => View.ShowSuccessFeedback(position),
        Fail: error => View.ShowErrorFeedback(position, error.Message)
    );
```

### Infrastructure Boundary
```csharp
// ONLY place where try/catch is acceptable
public override void _Ready() {
    try {
        InitializeGame();
    } catch (Exception ex) {
        // Last resort containment at system boundary
        GD.PrintErr($"Fatal initialization error: {ex}");
    }
}
```

## References
- [LanguageExt v5 Documentation](https://louthy.github.io/language-ext/)
- [LanguageExt GitHub](https://github.com/louthy/language-ext)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- [Functional Error Handling Wiki](https://github.com/louthy/language-ext/wiki/How-to-handle-errors-in-a-functional-way)

## Decision Record
- **Proposed**: 2025-08-30 by Tech Lead
- **Status**: Awaiting approval
- **Stakeholders**: All developers working on Darklands