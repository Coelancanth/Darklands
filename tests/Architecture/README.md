# Architecture Testing Framework

## Overview

This directory contains comprehensive architecture tests that enforce ADR compliance and architectural boundaries. The testing framework combines two powerful approaches:

1. **Reflection-based tests** (ArchitectureTests.cs, AdrComplianceTests.cs)
2. **NetArchTest-powered tests** (NetArchitectureTests.cs)

## 🎯 TD_024 + NetArchTest Enhancement

### What We Have (40 Total Tests)

| Test File | Tests | Purpose |
|-----------|-------|---------|
| **ArchitectureTests.cs** | 14 | Basic architectural patterns and conventions |
| **AdrComplianceTests.cs** | 14 | ADR-004/005/006 compliance via reflection |
| **NetArchitectureTests.cs** | 12 | Enhanced ADR enforcement via IL analysis |

### Why Both Approaches?

`★ Insight ─────────────────────────────────────`
**Reflection tests** excel at complex logic and custom validation (e.g., save-ready validation, compiler-generated code filtering). **NetArchTest** excels at precise dependency analysis and catching IL-level violations that reflection can't detect.
`─────────────────────────────────────────────────`

## 🔍 NetArchTest Advantages

### 1. Precise Dependency Detection
```csharp
// NetArchTest can detect System.Random usage at IL level
Types.InAssembly(_coreAssembly)
    .That().ResideInNamespace("Darklands.Core.Domain")
    .Should().NotHaveDependencyOn("System.Random")
```

**vs. Reflection limitations:**
```csharp
// Can only check field/property/parameter types, not method calls
var violations = FindTypeUsages(_coreAssembly, typeof(System.Random))
```

### 2. Comprehensive Namespace Analysis
```csharp
// NetArchTest automatically checks all dependency types
.Should().NotHaveDependencyOnAny("System.Threading", "System.IO", "System.Net")

// vs manual reflection checking each field/property individually
```

### 3. Fluent Rule Composition
```csharp
// Complex filtering in a readable chain
Types.InAssembly(_coreAssembly)
    .That().ResideInNamespace("Darklands.Core.Domain")
    .And().AreClasses()
    .And().AreNotAbstract()
    .Should().NotHaveDependencyOn("Godot")
```

## 🧪 Test Categories

### ADR-004: Deterministic Simulation
- ✅ **No System.Random** (NetArchTest detects IL usage)
- ✅ **No DateTime.Now** (Enhanced detection)
- ✅ **No Threading** (Comprehensive namespace checking)
- ✅ **Fixed-point math** (Reflection-based type analysis)

### ADR-005: Save-Ready Architecture  
- ✅ **No circular references** (Reflection logic)
- ✅ **No delegates/events** (Reflection with filtering)
- ✅ **No I/O operations** (NetArchTest namespace analysis)
- ✅ **Save-ready validation** (Custom reflection logic)

### ADR-006: Selective Abstraction
- ✅ **No Godot in Core** (NetArchTest with exceptions)
- ✅ **Clean layer boundaries** (NetArchTest dependency rules)
- ✅ **Interface abstractions** (Reflection analysis)

### Performance & Quality
- ✅ **Sealed commands** (NetArchTest type checking)
- ✅ **Naming conventions** (NetArchTest pattern matching)
- ✅ **Service patterns** (Enhanced rule composition)

## 🚀 Running Architecture Tests

### Quick Architecture Validation
```bash
# All architecture tests (40 tests)
dotnet test --filter "Category=Architecture"

# Only NetArchTest-powered tests (12 tests)  
dotnet test --filter "Tool=NetArchTest"

# Specific ADR compliance
dotnet test --filter "ADR=ADR-004"
```

### Integration with Build Pipeline
Architecture tests run automatically in the build pipeline and will fail the build if architectural violations are detected.

## 📊 Test Execution Performance

| Test Suite | Count | Time | Purpose |
|------------|-------|------|---------|
| Reflection-based | 28 | ~100ms | Complex validation logic |
| NetArchTest | 12 | ~90ms | Precise dependency analysis |
| **Total** | **40** | **~190ms** | **Complete coverage** |

## 💡 Future Enhancements

### TD_029: Roslyn Analyzers
Once NetArchTest proves valuable, TD_029 will add Roslyn analyzers for:
- **Immediate IDE feedback** (red squiggles)
- **Compile-time enforcement** (not just test-time)
- **Custom rule creation** for domain-specific patterns

### Potential Additions
- **Performance benchmarks** via BenchmarkDotNet
- **Complexity analysis** via NDepend integration  
- **Custom architecture rules** for game-specific patterns

## 📋 Maintenance Notes

### When Adding New Types
1. **Domain entities**: Ensure they pass save-ready validation
2. **Service classes**: Follow naming conventions (Service/Operation suffix)
3. **Commands**: Should be sealed record types
4. **Value objects**: Verify immutability patterns

### When Modifying Rules
1. **Test both approaches**: Ensure reflection and NetArchTest agree
2. **Update exceptions**: Maintain allowed patterns list
3. **Document rationale**: ADR violations should have clear reasons

---

*This architecture testing framework ensures that Darklands maintains high code quality and architectural integrity as the codebase grows.*