# ADR-003: Functional Error Handling with CSharpFunctionalExtensions

**Status**: Approved
**Date**: 2025-09-30
**Last Updated**: 2025-09-30
**Decision Makers**: Tech Lead

**Changelog**:
- 2025-09-30: Added **Three Types of Errors** framework (Domain/Infrastructure/Programmer), Result.Of() vs try-catch decision guidance, layer-specific rules, common mistakes section
- 2025-09-30: Added Result.Of(), TryFirst/TryLast, TryFind, Analyzers recommendation, corrected Match syntax

## Context

Traditional C# error handling uses exceptions (`try/catch`), which has problems:
- **Invisible failures**: Methods don't declare they can fail
- **Control flow**: Exceptions used for flow control
- **Performance**: Exception throwing is expensive
- **Testability**: Hard to test all error paths
- **Railway breaking**: One exception aborts entire operation

We need a functional approach where **errors are data, not control flow**.

## Decision

Use **CSharpFunctionalExtensions** for functional error handling throughout the codebase.

## The Three Types of Errors

**Critical Framework**: Not all errors are the same. Choose your error handling strategy based on error type.

### 1. Domain Errors (Expected Business Failures)

**Definition**: Failures that are part of the business domain and must be handled by callers.

**Characteristics**:
- Expected as part of normal business logic
- Part of the method's contract
- Caller MUST handle these cases
- Should never crash the application

**Examples**:
- Validation failures ("Health cannot be negative")
- Business rule violations ("Cannot attack dead target", "Insufficient resources")
- Expected "not found" scenarios ("Actor not in registry")
- State transition failures ("Cannot move while stunned")

**Implementation Pattern**: Return `Result<T>` with descriptive error messages
```csharp
// ✅ CORRECT - Domain error handling
public Result<Health> TakeDamage(float amount)
{
    if (amount < 0)
        return Result.Failure<Health>("Damage cannot be negative");

    var newValue = Math.Max(0, Current - amount);
    return Result.Success(new Health(newValue, Maximum));
}

// ✅ CORRECT - Business rule validation
public Result ValidateAttack(Actor attacker, Actor target)
{
    if (!target.IsAlive)
        return Result.Failure("Cannot attack dead target");

    if (attacker.IsStunned)
        return Result.Failure("Attacker is stunned");

    return Result.Success();
}
```

**Why Result<T>**: Makes failure modes explicit in the signature, forces callers to handle, enables railway-oriented composition.

### 2. Infrastructure Errors (Expected Technical Failures)

**Definition**: Technical failures from external systems that we expect might happen.

**Characteristics**:
- Expected failures from infrastructure/external systems
- Not part of business domain
- Caller should handle gracefully
- Boundary between our code and external systems

**Examples**:
- File I/O failures (resource loading, save/load)
- Network timeouts
- Database connection failures
- External API errors
- Godot resource loading failures

**Implementation Pattern**: Convert exceptions to `Result<T>` at boundary

```csharp
// ✅ PREFERRED - Use Result.Of() for simple cases
public Result<Scene> LoadScene(string path)
{
    return Result.Of(() => GD.Load<Scene>(path))
        .Ensure(scene => scene != null, "Scene is null after load")
        .MapError(ex => $"Failed to load scene {path}: {ex.Message}");
}

// ✅ ALTERNATIVE - Manual try-catch for fine-grained control
public Result<Config> LoadConfig(string path)
{
    try
    {
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<Config>(json);
        return config != null
            ? Result.Success(config)
            : Result.Failure<Config>("Deserialization returned null");
    }
    catch (FileNotFoundException)
    {
        _logger.LogWarning("Config file not found: {Path}", path);
        return Result.Failure<Config>($"Config file not found: {path}");
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Invalid config JSON in {Path}", path);
        return Result.Failure<Config>("Invalid config file format");
    }
    catch (IOException ex)
    {
        _logger.LogError(ex, "IO error reading config");
        return Result.Failure<Config>($"Could not read config: {ex.Message}");
    }
}
```

**Why Convert to Result**: Keeps the functional pipeline intact, allows composition with domain logic, makes infrastructure failures explicit.

### 3. Programmer Errors (Unexpected Bugs)

**Definition**: Bugs in our code that should never happen if the code is correct.

**Characteristics**:
- Indicates programming mistakes
- Should NEVER happen in correct code
- Cannot be meaningfully recovered from
- Should fail fast to expose the bug

**Examples**:
- Null reference violations (`ArgumentNullException`)
- Invalid state transitions (`InvalidOperationException`)
- Contract violations (precondition failures)
- Array out of bounds
- DI misconfiguration (missing service registration)

**Implementation Pattern**: Throw exceptions - FAIL FAST!

```csharp
// ✅ CORRECT - Fail fast on programmer errors
public Result<Actor> GetActor(ActorId id)
{
    // Null check: This is a programming error
    if (id == null)
        throw new ArgumentNullException(nameof(id));

    // Business logic: Not found is a domain concern
    return _actors.TryFind(id)
        .ToResult($"Actor {id} not found");
}

// ✅ CORRECT - Precondition enforcement
public Result ApplyDamage(Actor actor, float amount)
{
    if (actor == null)
        throw new ArgumentNullException(nameof(actor));

    if (amount < 0)
        throw new ArgumentOutOfRangeException(nameof(amount), "Must be non-negative");

    // Business logic starts here
    return actor.TakeDamage(amount);
}

// ❌ WRONG - Don't wrap programmer errors in Result
public Result<ServiceProvider> BuildServiceProvider()
{
    try
    {
        // If service registration fails, it's a config bug
        // Let it throw - you can't run the app anyway!
        return Result.Success(services.BuildServiceProvider());
    }
    catch (Exception ex)
    {
        // This swallows critical startup bugs
        return Result.Failure<ServiceProvider>($"DI failed: {ex.Message}");
    }
}

// ✅ BETTER - Let it crash, fix the bug
public ServiceProvider BuildServiceProvider()
{
    // If this throws, you have a configuration bug
    // Fix the bug, don't hide it!
    return services.BuildServiceProvider();
}
```

**Why Exceptions**: Programmer errors indicate bugs that must be fixed, not business scenarios that need handling. Fail fast exposes bugs quickly during development.

## Decision Framework: Which Pattern to Use?

### Quick Decision Tree

```
Is this error expected as part of the business domain?
├─ YES → Return Result<T> (Domain Error)
└─ NO → Is this error from an external system?
    ├─ YES → Use Result.Of() or try-catch → Result (Infrastructure Error)
    └─ NO → Does this indicate a programming bug?
        ├─ YES → throw Exception (Programmer Error)
        └─ NO → Re-evaluate: probably a Domain or Infrastructure error
```

### Decision Table

| Error Type | When It Occurs | Pattern | Example |
|------------|----------------|---------|---------|
| **Domain** | Business logic execution | `Result<T>` with descriptive error | `Result.Failure<Health>("Damage cannot be negative")` |
| **Infrastructure** | External system calls | `Result.Of()` or `try-catch → Result` | `Result.Of(() => File.ReadAllText(path))` |
| **Programmer** | Code bugs, contract violations | `throw Exception` | `throw new ArgumentNullException(nameof(id))` |

### Result.Of() vs Manual try-catch

**Use Result.Of() when**:
- ✅ Wrapping simple external calls
- ✅ You don't need exception-specific handling
- ✅ You want concise boundary conversion
- ✅ All exceptions can be treated uniformly

**Use manual try-catch → Result when**:
- ✅ You need different handling per exception type
- ✅ You want to add context-specific logging
- ✅ Some exceptions are programmer errors (rethrow them)
- ✅ You need more control over error messages

```csharp
// ✅ Result.Of() - Simple and clean
public Result<Scene> LoadScene(string path)
{
    return Result.Of(() => GD.Load<Scene>(path))
        .MapError(ex => $"Scene load failed: {ex.Message}");
}

// ✅ Manual try-catch - Fine-grained control
public Result<Scene> LoadSceneAdvanced(string path)
{
    try
    {
        if (!ResourceLoader.Exists(path))
            return Result.Failure<Scene>($"Scene not found: {path}");

        var scene = GD.Load<Scene>(path);
        return scene != null
            ? Result.Success(scene)
            : Result.Failure<Scene>("Scene loaded but was null");
    }
    catch (ArgumentException ex)
    {
        // Programmer error - invalid path format
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error loading scene");
        return Result.Failure<Scene>($"Scene load error: {ex.Message}");
    }
}
```

### Layer-Specific Rules

| Layer | Domain Errors | Infrastructure Errors | Programmer Errors |
|-------|---------------|----------------------|-------------------|
| **Domain** | Return `Result<T>` | N/A (no infrastructure) | Throw exceptions |
| **Application** | Return `Result<T>` | Convert to `Result<T>` | Throw exceptions |
| **Infrastructure** | Return `Result<T>` | Convert to `Result<T>` at boundary | Throw exceptions |
| **Presentation** | Handle with `Match()` | Handle with `Match()` | Let crash (fix bug) |

### Common Mistakes to Avoid

```csharp
// ❌ MISTAKE 1: Using Result for programmer errors
public Result<Actor> UpdateActor(Actor actor)
{
    if (actor == null)
        return Result.Failure<Actor>("Actor is null");  // WRONG! Throw ArgumentNullException
    // ...
}

// ❌ MISTAKE 2: Using exceptions for domain errors
public Health TakeDamage(float amount)
{
    if (amount < 0)
        throw new ArgumentException("Negative damage");  // WRONG! Return Result.Failure
    // ...
}

// ❌ MISTAKE 3: Catching and hiding programmer errors
try
{
    var service = provider.GetRequiredService<IMyService>();
}
catch (InvalidOperationException)
{
    return Result.Failure<IMyService>("Service not found");  // WRONG! Let it throw
}

// ❌ MISTAKE 4: Not converting infrastructure exceptions
public Scene LoadScene(string path)
{
    return GD.Load<Scene>(path);  // WRONG! Throws exception, breaks railway
}
```

### Core Types

| Type | Purpose | Use When |
|------|---------|----------|
| `Result` | Success/failure without value | Void operations |
| `Result<T>` | Success/failure with value | Operations returning data |
| `Maybe<T>` | Value might not exist | Optional values |
| `Result<T, TError>` | Custom error types | Domain-specific errors |

### Philosophy

**1. Errors Are Data**
```csharp
// ❌ BAD: Hidden failure
public Actor GetActor(ActorId id)
{
    var actor = _actors[id]; // Throws if not found!
    return actor;
}

// ✅ GOOD: Explicit failure
public Result<Actor> GetActor(ActorId id)
{
    return _actors.TryGetValue(id, out var actor)
        ? Result.Success(actor)
        : Result.Failure<Actor>("Actor not found");
}
```

**2. Railway-Oriented Programming**
```csharp
// Success track → Success track
// Failure track → stays on failure track (short-circuits)

public async Task<Result<AttackResult>> ExecuteAttack(...)
{
    return await GetAttacker(attackerId)           // Result<Actor>
        .Bind(attacker => GetTarget(targetId)      // Result<Actor>
            .Bind(target => ValidateAttack(attacker, target)  // Result
                .Bind(_ => CalculateDamage(attacker, target)   // Result<int>
                    .Map(damage => new AttackResult(damage)))));
}
```

**3. No Exceptions in Business Logic**
```csharp
// Domain/Application layers: NEVER throw exceptions
// Infrastructure boundary: try/catch ONLY here
```

## Usage Guidelines

### Domain Layer

**Value Objects**:
```csharp
public sealed record Health
{
    public float Current { get; }
    public float Maximum { get; }

    private Health(float current, float maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    // Smart constructor with validation
    public static Result<Health> Create(float current, float maximum)
    {
        if (maximum <= 0)
            return Result.Failure<Health>("Maximum health must be positive");

        if (current < 0)
            return Result.Failure<Health>("Current health cannot be negative");

        if (current > maximum)
            return Result.Failure<Health>("Current exceeds maximum");

        return Result.Success(new Health(current, maximum));
    }

    // Immutable operations
    public Result<Health> Reduce(float amount)
    {
        if (amount < 0)
            return Result.Failure<Health>("Damage cannot be negative");

        return Result.Success(new Health(
            Math.Max(0, Current - amount),
            Maximum));
    }
}
```

**Entities**:
```csharp
public class Actor
{
    public ActorId Id { get; }
    public Health Health { get; private set; }

    public Result<Health> TakeDamage(float amount)
    {
        return Health.Reduce(amount)
            .Tap(newHealth => Health = newHealth);  // Side effect on success, returns Result<Health>
    }
}
```

### Application Layer

**Commands**:
```csharp
public record ExecuteAttackCommand(
    ActorId AttackerId,
    ActorId TargetId
) : IRequest<Result<AttackResult>>;  // Explicit Result return!
```

**Handlers**:
```csharp
public class ExecuteAttackCommandHandler
    : IRequestHandler<ExecuteAttackCommand, Result<AttackResult>>
{
    public async Task<Result<AttackResult>> Handle(
        ExecuteAttackCommand cmd,
        CancellationToken ct)
    {
        // Railway-oriented composition
        var result = await GetAttacker(cmd.AttackerId)
            .Bind(attacker => GetTarget(cmd.TargetId)
                .Bind(target => ValidateAttack(attacker, target)
                    .Bind(_ => ApplyDamage(target, CalculateDamage(attacker, target))
                        .Map(() => new AttackResult(attacker.Id, target.Id)))));

        // Match for side effects (logging, events)
        return result.Tap(
            success: r => PublishAttackEvent(r),
            failure: err => _logger.Warning("Attack failed: {Error}", err));
    }

    private Result<Actor> GetAttacker(ActorId id) =>
        _actors.GetActor(id)
            .ToResult("Attacker not found");

    private Result<Actor> GetTarget(ActorId id) =>
        _actors.GetActor(id)
            .ToResult("Target not found");

    private Result ValidateAttack(Actor attacker, Actor target)
    {
        if (!target.IsAlive)
            return Result.Failure("Cannot attack dead target");

        if (!IsAdjacent(attacker.Position, target.Position))
            return Result.Failure("Target not in range");

        return Result.Success();
    }
}
```

### Infrastructure Layer

**Services**:
```csharp
public class ActorStateService : IActorStateService
{
    private readonly Dictionary<ActorId, Actor> _actors = new();

    // ✅ Use TryFind extension for safe dictionary access
    public Maybe<Actor> GetActor(ActorId id)
    {
        return _actors.TryFind(id);  // Returns Maybe<Actor> directly
    }

    public Result AddActor(Actor actor)
    {
        if (_actors.ContainsKey(actor.Id))
            return Result.Failure($"Actor {actor.Id} already exists");

        _actors[actor.Id] = actor;
        return Result.Success();
    }
}
```

**Boundary Exception Handling**:
```csharp
// Infrastructure boundary: Convert exceptions to Results
public class GodotResourceLoader : IResourceLoader
{
    // ✅ PREFERRED: Use Result.Of() to wrap risky operations
    public Result<T> Load<T>(string path) where T : Resource
    {
        return Result.Of(() => {
                if (!ResourceLoader.Exists(path))
                    throw new FileNotFoundException($"Resource not found: {path}");

                var resource = GD.Load<T>(path);
                if (resource == null)
                    throw new InvalidOperationException($"Failed to load: {path}");

                return resource;
            })
            .MapError(ex => $"Error loading {path}: {ex.Message}");
    }

    // ⚠️ ALTERNATIVE: Manual try/catch (only if Result.Of doesn't fit)
    public Result<T> LoadManual<T>(string path) where T : Resource
    {
        try
        {
            if (!ResourceLoader.Exists(path))
                return Result.Failure<T>($"Resource not found: {path}");

            var resource = GD.Load<T>(path);
            return resource != null
                ? Result.Success(resource)
                : Result.Failure<T>($"Failed to load: {path}");
        }
        catch (Exception ex)
        {
            return Result.Failure<T>($"Error loading {path}: {ex.Message}");
        }
    }
}
```

### Presentation Layer (Godot)

**Components**:
```csharp
public partial class HealthComponentNode : EventAwareNode
{
    private ILogger<HealthComponentNode>? _logger;

    public async void ApplyDamage(float amount)
    {
        var result = await _mediator!.Send(
            new TakeDamageCommand(_ownerId!.Value, amount));

        // Correct Match syntax (no named parameters)
        result.Match(
            onSuccess: () => _logger?.LogInformation("Damage applied successfully"),
            onFailure: err => _logger?.LogError("Failed to apply damage: {Error}", err));
    }
}
```

## Enabling LINQ Query Syntax

CSharpFunctionalExtensions doesn't include `SelectMany` by default. Add these extensions:

```csharp
// Infrastructure/Functional/ResultExtensions.cs
namespace CSharpFunctionalExtensions
{
    public static class ResultLinqExtensions
    {
        // Enable: from x in result select Transform(x)
        public static Result<T2> Select<T, T2>(
            this Result<T> result,
            Func<T, T2> selector)
        {
            return result.Map(selector);
        }

        // Enable: from x in r1 from y in r2 select Project(x, y)
        public static Result<T3> SelectMany<T, T2, T3>(
            this Result<T> result,
            Func<T, Result<T2>> bind,
            Func<T, T2, T3> project)
        {
            return result.Bind(x => bind(x).Map(y => project(x, y)));
        }

        // Async versions
        public static async Task<Result<T2>> Select<T, T2>(
            this Task<Result<T>> resultTask,
            Func<T, T2> selector)
        {
            var result = await resultTask;
            return result.Map(selector);
        }

        public static async Task<Result<T3>> SelectMany<T, T2, T3>(
            this Task<Result<T>> resultTask,
            Func<T, Task<Result<T2>>> bind,
            Func<T, T2, T3> project)
        {
            var result = await resultTask;
            if (result.IsFailure)
                return Result.Failure<T3>(result.Error);

            var result2 = await bind(result.Value);
            if (result2.IsFailure)
                return Result.Failure<T3>(result2.Error);

            return Result.Success(project(result.Value, result2.Value));
        }
    }
}
```

**Now LINQ syntax works**:
```csharp
public async Task<Result<AttackResult>> Execute(...)
{
    return await (
        from attacker in GetAttacker(attackerId)
        from target in GetTarget(targetId)
        from validation in ValidateAttack(attacker, target)
        from damage in CalculateDamage(attacker, target)
        select new AttackResult(damage)
    );
}
```

## Common Patterns

### Pattern 1: Combining Results

```csharp
// Collect all errors
var results = new[] {
    ValidateName(name),
    ValidateAge(age),
    ValidateEmail(email)
};

var combinedResult = Result.Combine(results);
if (combinedResult.IsFailure)
    return Result.Failure<User>(combinedResult.Error);

return CreateUser(name, age, email);
```

### Pattern 2: Maybe → Result Conversion

```csharp
Maybe<Actor> maybeActor = _actors.GetActor(id);

Result<Actor> result = maybeActor.ToResult("Actor not found");
```

### Pattern 3: Try Pattern

```csharp
var result = Result.Try(() => RiskyOperation(),
    ex => $"Operation failed: {ex.Message}");
```

### Pattern 4: Ensure (Validation)

```csharp
return Result.Success(actor)
    .Ensure(a => a.IsAlive, "Actor must be alive")
    .Ensure(a => a.Position.IsValid(), "Invalid position")
    .Map(a => ProcessActor(a));
```

### Pattern 5: Safe Collection Access

```csharp
// ❌ OLD: Nullable returns
Actor? firstActor = _actors.FirstOrDefault(a => a.IsAlive);
if (firstActor == null) { /* handle */ }

// ✅ NEW: TryFirst returns Maybe<T>
Maybe<Actor> firstActor = _actors.TryFirst(a => a.IsAlive);
Result<Actor> result = firstActor.ToResult("No alive actors");

// ✅ TryLast for last element
Maybe<Actor> lastActor = _actors.TryLast(a => a.Health.Current > 50);
```

### Pattern 6: Safe Dictionary Access

```csharp
// ❌ OLD: Throws KeyNotFoundException
Actor actor = _actorDict[id];

// ❌ OLD: TryGetValue with out parameter
if (_actorDict.TryGetValue(id, out var actor)) { /* ... */ }

// ✅ NEW: TryFind returns Maybe<T>
Maybe<Actor> maybeActor = _actorDict.TryFind(id);
Result<Actor> result = maybeActor.ToResult($"Actor {id} not found");
```

### Pattern 7: Result.Of for Exception Boundaries

```csharp
// ✅ Wrap risky external calls
Result<FileData> fileResult = Result.Of(() => File.ReadAllText(path));

// ✅ With async operations
Result<Scene> sceneResult = await Result.Of(() => GD.LoadAsync<Scene>(path));

// ✅ Custom error mapping
Result<Config> config = Result.Of(() => JsonSerializer.Deserialize<Config>(json))
    .MapError(ex => $"Invalid config: {ex.Message}");
```

## Error Logging

```csharp
// Log on failure track
return GetActor(id)
    .Tap(actor => _logger.Information("Found actor: {Id}", actor.Id))
    .TapError(err => _logger.Warning("Actor lookup failed: {Error}", err));
```

## Testing

```csharp
[Fact]
public void TakeDamage_LethalDamage_ReturnsSuccess()
{
    // Arrange
    var health = Health.Create(10, 100).Value;
    var actor = new Actor(ActorId.NewId(), health);

    // Act
    var result = actor.TakeDamage(20);

    // Assert
    result.IsSuccess.Should().BeTrue();
    actor.Health.Current.Should().Be(0);
    actor.IsAlive.Should().BeFalse();
}

[Fact]
public void Create_NegativeMaximum_ReturnsFailure()
{
    // Act
    var result = Health.Create(10, -50);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("positive");
}
```

## Consequences

### Positive
- ✅ **Explicit Errors**: Method signatures declare failures
- ✅ **Composable**: Railway-oriented programming
- ✅ **Type-Safe**: Compiler enforces error handling
- ✅ **Testable**: Easy to test success/failure paths
- ✅ **No Hidden Failures**: All errors visible
- ✅ **Performance**: No exception overhead

### Negative
- ❌ **Learning Curve**: Team must understand functional patterns
- ❌ **Verbose**: `Result<T>` everywhere in signatures
- ❌ **Chaining**: Bind chains can get nested

### Neutral
- ➖ **Different Paradigm**: Not traditional C# style
- ➖ **LINQ Extension Needed**: Must add SelectMany manually

## Tooling

### Roslyn Analyzers (Highly Recommended)

Install the analyzers package to enforce proper Result handling at compile-time:

```bash
dotnet add package CSharpFunctionalExtensions.Analyzers
```

**What it catches**:
- ✅ Ignored Result values (prevents silent failures)
- ✅ Dangerous `.Value` access without checking `.IsSuccess`
- ✅ Forgetting to `await` async Results
- ✅ Improper error handling patterns

**Example warnings**:

```csharp
// ❌ Analyzer warning: Result value ignored
GetActor(id);  // WARNING: Result not handled

// ✅ Correct: Result consumed
var result = GetActor(id);
result.Match(
    onSuccess: actor => ProcessActor(actor),
    onFailure: err => _logger.LogError(err));

// ❌ Analyzer warning: Unsafe Value access
var actor = GetActor(id).Value;  // WARNING: Check IsSuccess first

// ✅ Correct: Safe access
GetActor(id).Match(
    onSuccess: actor => UseActor(actor),
    onFailure: err => HandleError(err));
```

**Why this matters**: Analyzers prevent the most common mistake—ignoring Result values and accidentally reverting to exception-based error handling.

## Alternatives Considered

### 1. LanguageExt
**Rejected**: Too heavy (5MB), v5 is beta, F#-inspired

### 2. Traditional Exceptions
**Rejected**: Hidden failures, hard to test, expensive

### 3. OneOf (Discriminated Unions)
**Rejected**: More verbose, less railway-oriented

## Success Metrics

- ✅ **Domain errors** always return `Result<T>` (never throw exceptions for business logic)
- ✅ **Infrastructure errors** converted to `Result<T>` at boundaries (use `Result.Of()` or try-catch)
- ✅ **Programmer errors** throw exceptions (ArgumentNullException, InvalidOperationException)
- ✅ Zero `try/catch` in Domain/Application layers except at infrastructure boundaries
- ✅ All operations returning `Result<T>` or `Maybe<T>`
- ✅ Error handling tested in all handlers
- ✅ No unhandled exceptions in production
- ✅ CSharpFunctionalExtensions.Analyzers installed and enforcing patterns
- ✅ Use `TryFirst/TryLast/TryFind` instead of nullable-returning methods
- ✅ Team can articulate the difference between Domain/Infrastructure/Programmer errors

## References

- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Scott Wlaschin
- [CSharpFunctionalExtensions GitHub](https://github.com/vkhorikov/CSharpFunctionalExtensions)
- [Functional Error Handling](https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/) - Vladimir Khorikov