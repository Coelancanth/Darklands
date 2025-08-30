# LanguageExt v5 Usage Guide for Darklands

**Version**: LanguageExt.Core v5.0.0-beta-54  
**Last Updated**: 2025-08-30  
**Status**: MANDATORY reading for all developers

## 🚨 Critical: We Use v5, NOT v4

Many online examples use v4 syntax. **v5 has breaking changes**:
- ❌ `Try<T>` is REMOVED - use `Eff<T>` instead
- ❌ `TryAsync<T>` is REMOVED - use `IO<T>` instead  
- ❌ `Result<T>` is REMOVED - use `Fin<T>` instead
- ❌ `EitherAsync<L,R>` is REMOVED - use `EitherT<L, IO, R>` instead

## 🎯 Core Philosophy

**"Everything that can fail is explicit in the type signature"**

This is the fundamental principle. If a function can fail, its return type MUST indicate that:
- ❌ `User GetUser(int id)` - Lies! What if user doesn't exist?
- ✅ `Fin<User> GetUser(int id)` - Honest! Might fail, handle both cases

**Pure Functions Always Return**
- No throwing exceptions (breaks purity)
- No returning null (that's a side effect)
- Always return a value in the co-domain

**Errors Are Data, Not Control Flow**
- Exceptions are GOTO in disguise
- Errors should be values you can manipulate
- Use composition, not try/catch pyramids

## 📋 Quick Reference: Which Type to Use?

| Scenario | Use This | Example |
|----------|----------|---------|
| Operation can fail | `Fin<T>` | `Fin<Grid> LoadGrid()` |
| Value might not exist | `Option<T>` | `Option<Actor> FindActor(id)` |
| Need specific error type | `Either<L,R>` | `Either<ValidationError, User>` |
| Multiple errors possible | `Validation<Error, T>` | Form validation |
| Side effects (sync/async) | `Eff<T>` | Database operations |
| Pure I/O operations | `IO<T>` | File reading |
| Async with cancellation | `IO<T>` | Network calls |

## 🎯 Core Types and Patterns

### 1. Fin<T> - Our Primary Error Type

```csharp
using LanguageExt;
using static LanguageExt.Prelude;

// Creating Fin<T> values
Fin<int> success = Pure(42);                        // Success case
Fin<int> failure = Fail(Error.New("Not found"));    // Failure case

// Pattern matching
var result = myFin.Match(
    Succ: value => $"Got {value}",
    Fail: error => $"Error: {error.Message}"
);

// LINQ syntax (monadic composition)
Fin<string> GetUserName(int id) =>
    from user in FindUser(id)           // Fin<User>
    from name in ValidateName(user)     // Fin<string>
    select name.ToUpper();

// Converting from other types
Option<int> opt = Some(5);
Fin<int> fin = opt.ToFin(Error.New("No value"));  // Option → Fin
```

### 2. Option<T> - Nullable Values

```csharp
// Creating Options
Option<string> some = Some("Hello");
Option<string> none = None;

// From nullable
string? nullable = GetNullableString();
Option<string> opt = Optional(nullable);  // null becomes None

// Pattern matching
opt.Match(
    Some: s => Console.WriteLine(s),
    None: () => Console.WriteLine("No value")
);

// Default values
string value = opt.IfNone("default");
string value2 = opt | "default";  // Same thing

// LINQ syntax
Option<int> result = 
    from x in Some(5)
    from y in Some(10)
    select x + y;  // Some(15)
```

### 3. Eff<T> - Effects with Error Handling

```csharp
// Eff replaces Try<T> from v4
Eff<int> SafeDivide(int a, int b) =>
    b == 0 
        ? Fail(Error.New("Division by zero"))
        : Pure(a / b);

// Running effects
var result = SafeDivide(10, 2).Run();  // Returns Fin<int>

// Composing effects
Eff<string> ComplexOperation() =>
    from a in ReadFromDatabase()        // Eff<int>
    from b in CallWebService(a)         // Eff<string>  
    from c in ValidateResult(b)         // Eff<string>
    select c.ToUpper();

// Error recovery
Eff<int> WithFallback() =>
    DatabaseOperation()
        | CacheOperation()     // Try cache if DB fails
        | Pure(0);            // Default if both fail
```

### 4. IO<T> - Pure I/O Operations

```csharp
// IO for async operations
IO<string> ReadFileAsync(string path) =>
    IO.liftAsync(async () => await File.ReadAllTextAsync(path));

// Composing IO operations
IO<Unit> ProcessFile(string path) =>
    from content in ReadFileAsync(path)
    from _ in IO.lift(() => Console.WriteLine($"Read {content.Length} chars"))
    from result in ProcessContent(content)
    from _ in WriteFileAsync($"{path}.processed", result)
    select unit;

// Running IO
var fin = await ProcessFile("data.txt").RunAsync();  // Returns Fin<Unit>
```

### 5. Error Type Hierarchy

#### Critical Distinction: Expected vs Exceptional

**Expected Errors** - Part of your domain:
- User not found
- Invalid password
- Insufficient funds
- Out of stock
- **These are NOT exceptions - they're normal business flow**

**Exceptional Errors** - System failures:
- OutOfMemoryException
- NullReferenceException  
- StackOverflowException
- Database connection lost
- **These are true exceptions - unexpected system issues**

```csharp
// Expected errors (normal business failures)
var expected = Error.New("User not found");           // Expected
var withCode = Error.New(404, "Not found");          // Expected with code

// Exceptional errors (unexpected failures)
var exceptional = Error.New(new InvalidOperationException("Unexpected"));

// Check error type
if (error.IsExpected) {
    // Show user-friendly message
} else if (error.IsExceptional) {
    // Log to monitoring, show generic error
}

// Multiple errors (great for validation)
var validationErrors = Error.Many(
    Error.New("Email required"),
    Error.New("Password too short"),
    Error.New("Username taken")
);

// Combine errors with +
var combined = error1 + error2 + error3;

// Custom error types with context
public record NotFoundError(string Resource, int Id) 
    : Expected($"{Resource} with ID {Id} not found", 404, None);

public record ValidationError(string Field, string Rule) 
    : Expected($"{Field} violates {Rule}", 400, None);
```

## 🚂 Railway-Oriented Programming

### The Power of | (Pipe Operator)

The `|` operator enables elegant error recovery and fallback patterns:

```csharp
// Try multiple strategies in order
Fin<Config> LoadConfig() =>
    LoadFromFile("user.config")      // Try user config
    | LoadFromFile("default.config") // Fall back to default
    | LoadFromEnvironment()          // Try environment variables
    | Pure(DefaultConfig);           // Final fallback

// Chain operations with recovery
var result = 
    ValidateInput(input)
    | PromptUserForCorrection()
    | UseDefaultValue();
```

### The @catch Operator

Selective error recovery with powerful pattern matching:

```csharp
// Catch and transform specific errors
Fin<User> GetUser(int id) =>
    FetchFromDatabase(id)
    | @catch(err => err.Code == 404, 
             err => CreateGuestUser())
    | @catch(err => err.Is<TimeoutException>(),
             err => FetchFromCache(id))
    | @catch(err => err.IsExceptional,
             err => Fail(Error.New("System error, try later")));

// Transform error messages for users
var result = ComplexOperation()
    | @catch(err => err.IsExpected,
             err => Fail(Error.New($"Sorry: {SimplifyMessage(err)}")));
```

### Short-Circuiting Pattern

Errors automatically propagate through monadic chains:

```csharp
// If ANY step fails, the whole chain fails
Fin<Order> ProcessOrder(OrderRequest request) =>
    from validated in ValidateRequest(request)      // Fails? Stop here
    from customer in LoadCustomer(validated.CustomerId)  // Fails? Stop here
    from inventory in CheckInventory(validated.Items)    // Fails? Stop here
    from payment in ProcessPayment(customer, validated)  // Fails? Stop here
    from order in CreateOrder(customer, validated)       // Fails? Stop here
    from _ in SendConfirmation(customer, order)
    select order;
// One function, multiple failure points, zero try/catch!
```

## ⚠️ Anti-Patterns to AVOID

### ❌ NEVER: Try/Catch in Business Logic

```csharp
// ❌ WRONG - Anti-pattern
public string ProcessData(string input)
{
    try 
    {
        return Transform(input);
    }
    catch (Exception ex)
    {
        _logger.Error(ex, "Failed");
        return "default";
    }
}

// ✅ CORRECT - Functional approach
public Fin<string> ProcessData(string input) =>
    from validated in ValidateInput(input)
    from transformed in Transform(validated)
    select transformed;
```

### ❌ NEVER: Throwing Exceptions

```csharp
// ❌ WRONG - Throws exception
public int Divide(int a, int b)
{
    if (b == 0) throw new ArgumentException("Cannot divide by zero");
    return a / b;
}

// ✅ CORRECT - Returns Fin
public Fin<int> Divide(int a, int b) =>
    b == 0 
        ? Fail(Error.New("Cannot divide by zero"))
        : Pure(a / b);
```

### ❌ NEVER: Ignoring Fin Results

```csharp
// ❌ WRONG - Assumes success
Fin<User> userFin = GetUser(id);
var name = userFin.SuccValue.Name;  // Will throw if Fail!

// ✅ CORRECT - Handle both cases
var name = GetUser(id).Match(
    Succ: user => user.Name,
    Fail: _ => "Unknown"
);
```

## ✅ Best Practices

### 1. Import Prelude Statically

```csharp
using LanguageExt;
using static LanguageExt.Prelude;  // Gives you Some, None, Pure, Fail, etc.
```

### 2. Use LINQ for Composition

```csharp
// Clean, readable error propagation
public Fin<Order> ProcessOrder(int userId, int productId, int quantity) =>
    from user in FindUser(userId)
    from product in FindProduct(productId)
    from inventory in CheckInventory(product, quantity)
    from order in CreateOrder(user, product, quantity)
    from _ in ChargePayment(user, order)
    from _ in UpdateInventory(product, -quantity)
    select order;
```

### 3. Provide Meaningful Errors

```csharp
// ❌ BAD - Generic error
Fail(Error.New("Error"));

// ✅ GOOD - Specific, actionable
Fail(Error.New(404, $"Actor {actorId} not found at position {position}"));
```

### 4. Use Match for Side Effects

```csharp
await ProcessCommand(cmd).Match(
    Succ: result => View.ShowSuccess(result),
    Fail: error => View.ShowError(error.Message)
);
```

### 5. Layer-Specific Patterns

```csharp
// Domain Layer - Pure functions
public static Fin<Position> Move(Position from, Direction dir) =>
    dir switch {
        Direction.North => Pure(from with { Y = from.Y - 1 }),
        Direction.South => Pure(from with { Y = from.Y + 1 }),
        _ => Fail(Error.New($"Invalid direction: {dir}"))
    };

// Application Layer - Orchestration
public Task<Fin<Unit>> Handle(MoveCommand cmd, CancellationToken ct) =>
    from actor in GetActor(cmd.ActorId)
    from newPos in Move(actor.Position, cmd.Direction)
    from _ in UpdatePosition(cmd.ActorId, newPos)
    select unit;

// Presentation Layer - UI updates
public async Task OnMoveClicked(Direction dir) =>
    await MoveActor(dir).Match(
        Succ: _ => RefreshDisplay(),
        Fail: err => ShowErrorToast(err.Message)
    );
```

## 🔄 Migration from v4

### Try<T> → Eff<T>

```csharp
// v4
Try<int> Compute() => () => 42;

// v5
Eff<int> Compute() => Pure(42);
```

### TryAsync<T> → IO<T>

```csharp
// v4
TryAsync<string> ReadAsync() => async () => await ReadFile();

// v5
IO<string> ReadAsync() => IO.liftAsync(async () => await ReadFile());
```

### Result<T> → Fin<T>

```csharp
// v4
Result<int> result = new Result<int>(42);

// v5
Fin<int> result = Pure(42);
```

## 🛠️ Useful Extension Methods

```csharp
// Option → Fin
option.ToFin(Error.New("No value"));

// Either → Fin
either.ToFin();

// Task<T> → IO<T>
IO.liftAsync(async () => await SomeTask());

// Fin → Option
fin.ToOption();

// Collection operations
list.Map(x => x * 2);           // Transform each element
list.Filter(x => x > 5);        // Keep matching elements
list.Fold(0, (acc, x) => acc + x);  // Reduce to single value
```

## 📖 Further Reading

- [Official LanguageExt v5 Docs](https://louthy.github.io/language-ext/)
- [Functional Programming in C#](https://github.com/louthy/language-ext/wiki)
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/)
- ADR-008: Functional Error Handling (Our architectural decision)

## ⚡ Quick Lookup

```csharp
// Imports
using LanguageExt;
using static LanguageExt.Prelude;

// Success values
Pure(value)           // Fin<T>, Eff<T>
Some(value)          // Option<T>
Right(value)         // Either<L,R>

// Failure values
Fail(error)          // Fin<T>, Eff<T>
None                 // Option<T>
Left(error)          // Either<L,R>

// Pattern matching
result.Match(
    Succ: x => HandleSuccess(x),
    Fail: e => HandleError(e)
);

// Default values
option.IfNone(defaultValue)
fin.IfFail(defaultValue)

// Chaining (railroad)
operation1 | operation2 | operation3

// Error recovery
mainOperation | fallbackOperation | Pure(defaultValue)
```

---

**Remember**: When in doubt, make errors explicit in your return type. It's better to have `Fin<T>` everywhere than hidden exceptions anywhere.