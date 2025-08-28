# BlockLife Testing Guide

**Last Updated**: 2025-08-23
**Purpose**: Comprehensive testing patterns and troubleshooting guide

## 🎯 Test Framework Discovery

**CRITICAL**: Always check which framework is used before writing tests

```bash
# Check mocking framework
grep -r "using Moq" tests/        # This project uses Moq
grep -r "using NSubstitute" tests/ # NOT this

# Check test framework
grep -r "using Xunit" tests/       # XUnit for unit tests
grep -r "using GdUnit" tests/      # GdUnit4 for Godot integration
```

## 🧪 Test Types & Patterns

### Unit Tests (Most Common)
- **Location**: `tests/BlockLife.Core.Tests/`
- **Speed**: <10ms per test
- **Coverage**: Business logic, domain models, services

### Integration Tests
- **Location**: `tests/GdUnit4/Features/`
- **Speed**: <100ms per test
- **Coverage**: End-to-end flows, Godot integration

### Architecture Tests
- **Location**: `tests/BlockLife.Core.Tests/Architecture/`
- **Purpose**: Enforce patterns, prevent regressions

## 📦 Mocking with Moq

```csharp
// CORRECT - This project uses Moq
var mockService = new Mock<IPlayerStateService>();
mockService.Setup(x => x.GetCurrentPlayer())
           .Returns(new PlayerState { /* ... */ });
var service = mockService.Object;

// WRONG - Don't use NSubstitute
var mockService = Substitute.For<IPlayerStateService>(); // ❌
```

## 🔧 LanguageExt Testing Patterns

### Testing Fin<T> Results
```csharp
// ✅ Test success case
result.IsSucc.Should().BeTrue();
result.IfSucc(value => {
    value.Id.Should().Be(expectedId);
    value.State.Should().Be(ExpectedState.Active);
});

// ✅ Test failure case
result.IsFail.Should().BeTrue();
result.IfFail(error => {
    error.Code.Should().Be("VALIDATION_ERROR");
    error.Message.Should().Contain("invalid");
});

// ✅ Pattern matching approach
result.Match(
    Succ: value => value.State.Should().Be(ExpectedState.Active),
    Fail: error => Assert.Fail($"Expected success: {error.Message}")
);
```

### Testing Option<T>
```csharp
// Test Some case
option.IsSome.Should().BeTrue();
option.IfSome(entity => {
    entity.Name.Should().Be("Expected");
    entity.IsActive.Should().BeTrue();
});

// Test None case
option.IsNone.Should().BeTrue();
```

### Testing Collections
```csharp
// ✅ Correct Seq initialization for tests
var items = new[] { item1, item2 }.ToSeq();
var patterns = new IPattern[] { pattern1, pattern2 }.ToSeq();

// ✅ Correct Map construction
var resourceMap = Map((ResourceType.Money, 100), (ResourceType.Social, 50));

// ❌ WRONG - Don't use these
var seq = Seq<Item>(item);  // Wrong
var map = Map<K,V>((key, value));  // Wrong syntax
```

## 🎲 Property-Based Testing with FsCheck 3.x

### Basic Pattern
```csharp
[Fact]
public void Property_Should_Hold_For_All_Inputs()
{
    Prop.ForAll(
        GenValidInput().ToArbitrary(),
        input => {
            // Test invariant
            var result = ProcessInput(input);
            return result.IsValid();
        }
    ).QuickCheckThrowOnFailure();
}
```

### Custom Generators (FsCheck 3.x API)
```csharp
// Generators return Gen<T> directly in v3
private static Gen<BlockType> GenBlockType() =>
    Gen.Elements(BlockType.Work, BlockType.Study, BlockType.Health,
                 BlockType.Creativity, BlockType.Fun, BlockType.Relationship);

private static Gen<Position> GenPosition() =>
    from x in Gen.Choose(0, 9)
    from y in Gen.Choose(0, 9)
    select new Position(x, y);

// Combine generators
private static Gen<Block> GenBlock() =>
    from type in GenBlockType()
    from pos in GenPosition()
    select new Block { 
        Type = type, 
        Position = pos,
        Id = Guid.NewGuid()
    };
```

### Using Generators in Tests
```csharp
[Fact]
public void Blocks_Should_Not_Overlap()
{
    Prop.ForAll(
        Gen.ListOf(GenBlock(), 10).ToArbitrary(),
        blocks => {
            var positions = blocks.Select(b => b.Position).ToList();
            return positions.Distinct().Count() == positions.Count;
        }
    ).QuickCheckThrowOnFailure();
}
```

## 🔍 DI Testing & Troubleshooting

### Regression Tests for DI
```csharp
[Fact]
public void All_Handlers_Should_Be_Registered()
{
    var services = new ServiceCollection();
    services.AddBlockLifeCore();
    var provider = services.BuildServiceProvider();
    
    var handlerTypes = typeof(ApplyMatchRewardsCommandHandler).Assembly
        .GetTypes()
        .Where(t => t.GetInterfaces().Any(i => 
            i.IsGenericType && 
            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
        .Where(t => !t.IsAbstract && !t.IsInterface);
    
    foreach (var handlerType in handlerTypes)
    {
        var handler = provider.GetService(handlerType);
        handler.Should().NotBeNull($"{handlerType.Name} not registered");
    }
}
```

### Namespace Convention Tests
```csharp
[Fact]
public void Handlers_Should_Be_In_Core_Namespace()
{
    var handlers = typeof(IRequestHandler<,>).Assembly
        .GetTypes()
        .Where(t => t.GetInterfaces().Any(i => 
            i.IsGenericType && 
            i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));
    
    foreach (var handler in handlers)
    {
        handler.Namespace.Should().StartWith("BlockLife.Core",
            $"{handler.Name} has wrong namespace - MediatR won't discover it");
    }
}
```

### DI Cascade Failure Pattern
**CRITICAL**: When you see multiple test failures, check DI first!

```csharp
// One missing registration...
// services.AddScoped<IPlayerStateService, PlayerStateService>(); // FORGOT THIS

// ...causes cascade of failures:
// ❌ 2 DI Resolution tests
// ❌ 2 DI Lifetime tests  
// ❌ 6 SimulationManager stress tests
// ❌ 4 SimplifiedStressTest tests
// ❌ 17 other integration tests

// Debug approach:
// 1. Run DI validation tests first
// 2. Check GameStrapper.cs registrations
// 3. Verify namespace is BlockLife.Core.*
```

## 🚀 Performance Testing

### Basic Performance Test
```csharp
[Fact]
public void Recognition_Should_Be_Fast()
{
    var recognizer = new MatchPatternRecognizer();
    var grid = GenerateLargeGrid(100, 100);
    
    var stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
    {
        recognizer.RecognizeAt(grid, RandomPosition());
    }
    stopwatch.Stop();
    
    var avgMs = stopwatch.ElapsedMilliseconds / 1000.0;
    avgMs.Should().BeLessThan(1.0, "Recognition must be <1ms for 60fps");
}
```

### Performance Optimization Verification
```csharp
[Fact]
public void CanRecognizeAt_Should_Provide_Speedup()
{
    var recognizer = new MatchPatternRecognizer();
    
    // Measure with optimization
    var withOptimization = MeasureTime(() => {
        if (recognizer.CanRecognizeAt(grid, pos))
            recognizer.RecognizePattern(grid, pos);
    });
    
    // Measure without
    var withoutOptimization = MeasureTime(() => {
        recognizer.RecognizePattern(grid, pos);
    });
    
    var speedup = withoutOptimization / withOptimization;
    speedup.Should().BeGreaterThan(4.0, "Expected 4x+ speedup");
}
```

## 🐛 Common Test Issues & Solutions

### 1. Tests Not Discovered
```csharp
// ❌ WRONG - Missing attribute
public class MyTests {
    public void Test_Something() { }
}

// ✅ CORRECT - Has TestSuite attribute
[TestSuite]
public class MyTests {
    [Test]
    public void Test_Something() { }
}
```

### 2. Namespace Causes Test Failures
```csharp
// ❌ WRONG - Outside Core namespace
namespace BlockLife.Features.Player
public class MyHandler : IRequestHandler<Command, Result>

// ✅ CORRECT - In Core namespace
namespace BlockLife.Core.Features.Player
public class MyHandler : IRequestHandler<Command, Result>
```

### 3. Mock Setup Incorrect
```csharp
// ❌ WRONG - Forgot .Object
var service = new Mock<IService>();
UseService(service); // Type mismatch!

// ✅ CORRECT - Use .Object
var mockService = new Mock<IService>();
UseService(mockService.Object);
```

### 4. Async Test Hanging
```csharp
// ❌ WRONG - Deadlock risk
[Fact]
public void Test_Async()
{
    var result = AsyncMethod().Result; // Blocks
}

// ✅ CORRECT - Async all the way
[Fact]
public async Task Test_Async()
{
    var result = await AsyncMethod();
}
```

## 📊 Test Organization Best Practices

### Test Naming Convention
```csharp
// Pattern: Method_Scenario_ExpectedBehavior
[Fact]
public void ApplyRewards_WithValidMatch_IncreasesResources() { }

[Fact]
public void ApplyRewards_WithNullPattern_ReturnsFail() { }
```

### Test Categories
```csharp
[Fact]
[Trait("Category", "Unit")]
public void Unit_Test() { }

[Fact]
[Trait("Category", "Integration")]
public void Integration_Test() { }

[Fact]
[Trait("Category", "Performance")]
public void Performance_Test() { }

// Run specific category
// dotnet test --filter "Category=Unit"
```

### Test Data Builders
```csharp
public class PlayerStateBuilder
{
    private string _name = "TestPlayer";
    private int _level = 1;
    
    public PlayerStateBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public PlayerState Build() => new()
    {
        Name = _name,
        Level = _level,
        CreatedAt = DateTime.UtcNow
    };
}

// Usage in tests
var player = new PlayerStateBuilder()
    .WithName("Alice")
    .WithLevel(5)
    .Build();
```

## 🔄 Regression Test Protocol

**MANDATORY after bug fixes:**

1. **Create test that reproduces the bug**
   ```csharp
   [Fact]
   [Trait("Category", "Regression")]
   public void TD_068_Namespace_Should_Be_Correct()
   {
       // Test that would have caught the bug
   }
   ```

2. **Verify test fails without fix**
   ```bash
   git stash  # Remove fix
   dotnet test --filter "TD_068"  # Should fail
   git stash pop  # Restore fix
   ```

3. **Verify test passes with fix**
   ```bash
   dotnet test --filter "TD_068"  # Should pass
   ```

4. **Add to regression suite**
   - Location: `tests/BlockLife.Core.Tests/Regression/`
   - Naming: `TD_XXX_Description_RegressionTests.cs`

## 🎯 Quick Test Commands

```bash
# Run all tests
./scripts/core/build.ps1 test

# Run specific test class
dotnet test --filter "FullyQualifiedName~MatchPatternTests"

# Run tests matching pattern
dotnet test --filter "DisplayName~Should_Recognize"

# Run unit tests only
dotnet test --filter "Category=Unit"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run in watch mode
dotnet watch test

# Run specific test project
dotnet test tests/BlockLife.Core.Tests

# Debug specific test
dotnet test --filter "Method_Name" --logger:"console;verbosity=detailed"
```

## 📈 Coverage Guidelines

### Target Coverage
- **Unit Tests**: 80%+ coverage
- **Critical Paths**: 95%+ coverage
- **Domain Models**: 100% coverage
- **UI/Presenters**: 60%+ coverage

### Check Coverage
```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# View coverage locally
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

## 🔗 Related Documentation

- [HANDBOOK.md](HANDBOOK.md) - General testing patterns
- [FsCheck3xMigrationGuide.md](FsCheck3xMigrationGuide.md) - Property testing migration
- [Workflow.md](../01-Active/Workflow.md) - Test-driven development workflow

## Key Takeaways

1. **Always check test framework first** - Moq not NSubstitute
2. **Namespace must be BlockLife.Core.**** for MediatR
3. **DI failures cascade** - Check registrations first
4. **Use property tests for invariants** - FsCheck 3.x patterns
5. **Create regression tests immediately** - Prevent recurrence
6. **Performance test critical paths** - <1ms for 60fps
7. **Mock correctly** - Use .Object with Moq