# Test Categories Guide - Complete Reference

**Created**: 2025-08-24 by DevOps Engineer  
**Purpose**: Comprehensive guide to BlockLife's test categorization system (TD_071)

## 🎯 What Are Test Categories?

Test categories are **tags** applied to test methods using xUnit's `[Trait]` attribute. They allow us to:
- **Filter** which tests to run
- **Group** tests by purpose
- **Optimize** execution time
- **Enable** incremental testing

## 📊 Current Test Categories in BlockLife

### 1. Architecture Category
**Purpose**: Validate code structure and design rules  
**Count**: 30 tests  
**Execution Time**: ~1.3 seconds  
**When to Run**: Every commit, pre-commit hook

```csharp
[Fact]
[Trait("Category", "Architecture")]
public void Core_Should_Not_Reference_Godot()
{
    // Ensures clean architecture boundaries
}
```

**What's Tested**:
- Namespace conventions (MediatR discovery)
- Assembly dependencies (no Godot in Core)
- Command/Handler patterns
- Immutability rules
- DI registration patterns

### 2. Performance Category
**Purpose**: Validate performance requirements  
**Count**: 7 tests  
**Execution Time**: Variable (1-10 seconds)  
**When to Run**: Before release, performance investigations

```csharp
[Fact]
[Trait("Category", "Performance")]
public async Task FirstBlockClick_ShouldCompleteWithin16ms()
{
    // Ensures UI responsiveness
}
```

**What's Tested**:
- First-click latency (<16ms requirement)
- Animation performance
- Drag operation responsiveness
- Memory allocation patterns

### 3. ThreadSafety Category
**Purpose**: Validate concurrent operation safety  
**Count**: 1 test  
**Execution Time**: ~5 seconds  
**When to Run**: After threading changes

```csharp
[Fact]
[Trait("Category", "ThreadSafety")]
public async Task ConcurrentOperations_ShouldMaintainPerformance()
{
    // Stress test with multiple threads
}
```

### 4. Unit (Default - No Category)
**Purpose**: Fast, isolated component tests  
**Count**: ~300 tests  
**Execution Time**: ~20 seconds  
**When to Run**: Every build

Tests without a category are treated as unit tests by default.

## 🔧 How Categories Work Technically

### 1. Applying Categories to Tests

```csharp
// Single category
[Fact]
[Trait("Category", "Architecture")]
public void Test_Method() { }

// Multiple categories (use multiple Trait attributes)
[Fact]
[Trait("Category", "Architecture")]
[Trait("Category", "Critical")]
public void Critical_Architecture_Test() { }

// Category with Theory tests
[Theory]
[Trait("Category", "Architecture")]
[InlineData("BlockLife.Application", "BlockLife.Core.Application")]
public void Test_With_Data(string wrong, string correct) { }
```

### 2. Running Tests by Category

#### Command Line (dotnet test)
```bash
# Run only Architecture tests
dotnet test --filter "Category=Architecture"

# Run all EXCEPT Performance tests
dotnet test --filter "Category!=Performance"

# Run Architecture OR Performance
dotnet test --filter "Category=Architecture|Category=Performance"

# Complex filters (Architecture but not Slow)
dotnet test --filter "Category=Architecture&Category!=Slow"
```

#### Our Custom Scripts
```powershell
# Quick architecture tests only
./scripts/test/quick.ps1

# Full suite with categories
./scripts/test/full.ps1

# Future: Incremental based on changes
./scripts/test/incremental.ps1
```

### 3. How Filtering Works Under the Hood

xUnit's test discovery uses these filters at the **test discovery phase**, not execution:

1. **Discovery Phase**: xUnit scans assemblies for tests
2. **Filter Application**: Applies the filter expression
3. **Execution List**: Builds list of matching tests
4. **Run Phase**: Executes only filtered tests

This means:
- Filtering is **very fast** (milliseconds)
- No wasted time loading excluded tests
- Works with test runners (VS, Rider, CLI)

## 📋 Category Strategy & Best Practices

### When to Create a New Category

Create a category when tests share:
- **Execution characteristics** (slow, fast, flaky)
- **Purpose** (architecture, integration, acceptance)
- **Dependencies** (database, file system, network)
- **Frequency** (every commit, nightly, release)

### Category Naming Conventions

```csharp
// Purpose-based (PREFERRED)
[Trait("Category", "Architecture")]   // What it validates
[Trait("Category", "Integration")]    // How it tests
[Trait("Category", "Acceptance")]     // User scenarios

// Speed-based (USE SPARINGLY)
[Trait("Category", "Slow")]          // >10 seconds
[Trait("Category", "Fast")]          // <100ms

// Dependency-based
[Trait("Category", "RequiresDB")]    // Needs database
[Trait("Category", "RequiresGodot")] // Needs Godot runtime
```

### Category Anti-Patterns to Avoid

❌ **Too Many Categories**
```csharp
// BAD: Over-categorization
[Trait("Category", "Unit")]
[Trait("Category", "Fast")]
[Trait("Category", "Player")]
[Trait("Category", "Command")]
[Trait("Category", "Important")]
```

✅ **Focused Categories**
```csharp
// GOOD: Clear, single purpose
[Trait("Category", "Architecture")]
```

❌ **Vague Categories**
```csharp
[Trait("Category", "Misc")]
[Trait("Category", "Other")]
[Trait("Category", "Test")]
```

## 🚀 Execution Strategies

### 1. Progressive Testing (Current)
```
Architecture (1.3s) → Unit (20s) → Integration (10s) → Performance (variable)
```

### 2. Risk-Based Testing (Future with TD_077)
```
Changed Files → Affected Tests → Related Integration → Full Suite
```

### 3. CI/CD Pipeline Stages
```yaml
# PR Pipeline
1. incremental-tests (30s) - Only changed code
2. architecture-tests (5s) - If architecture files changed
3. full-tests (3min) - Only if previous fail or major change

# Main Branch Pipeline  
1. full-tests (3min) - Always run everything
2. performance-tests (5min) - Baseline metrics
3. stress-tests (10min) - Nightly only
```

## 📊 Current Test Distribution

| Category | Count | Time | Purpose | When to Run |
|----------|-------|------|---------|-------------|
| Architecture | 30 | 1.3s | Structure validation | Every commit |
| Unit (default) | ~300 | 20s | Component testing | Every build |
| Performance | 7 | 1-10s | Speed validation | Before release |
| ThreadSafety | 1 | 5s | Concurrency | After threading changes |

## 🔮 Future Categories (Planned)

### Integration Category (Coming Soon)
```csharp
[Trait("Category", "Integration")]
// Tests that verify multiple components work together
// Run after unit tests pass
```

### Acceptance Category (Planned)
```csharp
[Trait("Category", "Acceptance")]
// End-to-end user scenarios
// Run before release
```

### Platform-Specific (If Needed)
```csharp
[Trait("Platform", "Windows")]
[Trait("Platform", "Linux")]
// Handle OS-specific behavior
```

## 💡 Tips for Developers

### 1. Choose the Right Category
- **Modifying architecture?** → Your tests need `[Trait("Category", "Architecture")]`
- **Testing performance?** → Add `[Trait("Category", "Performance")]`
- **Default unit test?** → No category needed

### 2. Use Categories in Development
```bash
# Working on architecture? Run only those tests
dotnet test --filter "Category=Architecture"

# Before committing, run quick tests
./scripts/test/quick.ps1

# Full validation before PR
./scripts/test/full.ps1
```

### 3. Category Decision Tree
```
Is it validating code structure/rules? → Architecture
Is it testing speed/performance? → Performance  
Is it testing thread safety? → ThreadSafety
Is it testing multiple components? → Integration (future)
Else → No category (unit test)
```

## 🛠️ Implementation Details

### How We Added Categories (TD_071)

1. **Identified test groups** by execution time and purpose
2. **Added Trait attributes** to test methods
3. **Created filter scripts** (quick.ps1, full.ps1)
4. **Integrated with hooks** (optional pre-commit)
5. **Documented patterns** (this guide)

### Files Modified
- `tests/Architecture/ArchitectureTests.cs` - Added Architecture category
- `tests/Architecture/ArchitectureFitnessTests.cs` - Added Architecture category  
- `tests/BlockLife.Core.Tests/Infrastructure/Architecture/NamespaceConventionTests.cs` - Added Architecture category
- `scripts/test/quick.ps1` - Runs Architecture only
- `scripts/test/full.ps1` - Staged execution
- `.husky/pre-commit` - Optional quick tests

## 📈 Metrics & Success

### Before Categories (TD_071)
- All tests always run: 39 seconds
- No way to get quick feedback
- CI runs everything for every PR

### After Categories (Current)
- Architecture only: 1.3 seconds
- Staged execution: Progressive feedback
- Developer can choose what to run

### Future with Incremental (TD_077)
- Single file change: 2 seconds
- Smart detection: Only affected tests
- CI optimization: 90% faster for typical PRs

## 🔗 Related Documentation

- [TD_071 Backlog Entry](../01-Active/Backlog.md) - Original implementation
- [TD_077 Backlog Entry](../01-Active/Backlog.md) - Incremental testing plan
- [scripts/test/](../../scripts/test/) - Test execution scripts
- [.github/workflows/ci.yml](../../.github/workflows/ci.yml) - CI configuration

---

*This guide is the single source of truth for test categorization in BlockLife. Keep it updated as categories evolve.*