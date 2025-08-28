# Test Automation Scripts

Zero-friction test execution with progressive feedback (TD_071).

## 🚀 Available Scripts

### quick.ps1 - Architecture Tests Only
**Purpose**: Ultra-fast feedback on code structure  
**Execution Time**: ~1.3 seconds  
**Test Count**: 30 architecture tests  
**When to Use**: 
- During development for rapid iteration
- Before committing (optional via hook)
- When modifying architecture/patterns

```bash
./scripts/test/quick.ps1          # Normal output
./scripts/test/quick.ps1 -Silent  # Minimal output (for hooks)
./scripts/test/quick.ps1 -Verbose # Detailed output
```

### full.ps1 - Staged Test Execution
**Purpose**: Complete test suite with progressive feedback  
**Execution Time**: 3-5 seconds (without slow tests)  
**Test Count**: 340+ tests  
**When to Use**:
- Before creating a PR
- After major changes
- For complete validation

```bash
./scripts/test/full.ps1              # All tests
./scripts/test/full.ps1 -SkipSlow   # Skip Performance/Stress
./scripts/test/full.ps1 -StopOnFailure  # Stop at first failure
```

**Execution Stages**:
1. Architecture (1.3s) - Code structure validation
2. Core Tests (20s) - Unit and integration
3. Performance & Stress (variable) - Optional

### incremental.ps1 (Coming Soon - TD_077)
**Purpose**: Test only changed code  
**Execution Time**: ~2 seconds for typical changes  
**When to Use**:
- After every file save (with watcher)
- Before committing
- In CI/CD for PRs

```bash
./scripts/test/incremental.ps1      # Auto-detect changes
./scripts/test/incremental.ps1 -Since HEAD~1  # Since last commit
./scripts/test/incremental.ps1 -Watch  # Real-time testing
```

## 📊 Test Categories

Tests are organized using xUnit's `[Trait("Category", "...")]` attribute:

| Category | Tests | Time | Purpose |
|----------|-------|------|---------|
| Architecture | 30 | 1.3s | Code structure & patterns |
| Unit (default) | 300+ | 20s | Component isolation |
| Performance | 7 | 1-10s | Speed requirements |
| ThreadSafety | 1 | 5s | Concurrency validation |

See [TEST_CATEGORIES_GUIDE.md](../../Docs/03-Reference/TEST_CATEGORIES_GUIDE.md) for complete details.

## 🎯 Usage Recommendations

### Development Workflow
```bash
# 1. During coding - rapid feedback
./scripts/test/quick.ps1

# 2. Before commit - full validation
./scripts/core/build.ps1 test

# 3. Before PR - complete coverage
./scripts/test/full.ps1
```

### CI/CD Integration
- **PR Pipeline**: Runs incremental tests (30s)
- **Main Branch**: Full test suite (3 min)
- **Nightly**: Performance & stress tests

### Pre-commit Hook (Optional)
Enable architecture tests in pre-commit:
```bash
export BLOCKLIFE_PRECOMMIT_TESTS=true
```

## 🔧 Technical Details

### Filter Syntax
```bash
# Category filtering
--filter "Category=Architecture"
--filter "Category!=Performance"
--filter "Category=Architecture|Category=Unit"

# Name filtering
--filter "FullyQualifiedName~Move"
--filter "FullyQualifiedName~BlockLife.Core.Tests.Architecture"
```

### Performance Metrics
- **Full suite**: 39 seconds → 3.3 seconds (staged)
- **Architecture only**: 1.3 seconds
- **Incremental (planned)**: 2 seconds average

### Implementation
- Uses `dotnet test` with xUnit filters
- Progressive execution for fail-fast
- Clear result reporting
- Exit codes for CI/CD integration

## 📈 Future Enhancements

### TD_077: Incremental Test Runner
- Git-based change detection
- Convention-based test mapping
- Cache test results by file hash
- CI/CD optimization (90% faster PRs)

### Additional Scripts Planned
- `coverage.ps1` - Code coverage reporting
- `performance.ps1` - Benchmark tracking
- `watch.ps1` - File watcher integration

## 🔗 Related Documentation

- [HANDBOOK.md](../../Docs/03-Reference/HANDBOOK.md) - Complete build/test reference
- [TEST_CATEGORIES_GUIDE.md](../../Docs/03-Reference/TEST_CATEGORIES_GUIDE.md) - Category details
- [TD_071](../../Docs/01-Active/Backlog.md) - Test categorization implementation
- [TD_077](../../Docs/01-Active/Backlog.md) - Incremental testing plan

---

*Zero-friction testing: Get feedback in seconds, not minutes.*