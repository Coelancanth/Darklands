# FsCheck 3.x Migration Guide

**Created**: 2025-08-22  
**Author**: Test Specialist  
**Purpose**: Document migration patterns from FsCheck 2.x to 3.x for future reference

## Overview

FsCheck 3.x introduced significant breaking changes to its API, particularly in how generators and arbitrary instances are created and used. This guide documents the patterns discovered during the TD_048 migration.

## Key API Changes

### 1. Namespace Changes

```csharp
// FsCheck 2.x
using FsCheck;

// FsCheck 3.x - Add Fluent namespace for C# users
using FsCheck;
using FsCheck.Fluent;  // Required for extension methods
```

### 2. Generator Return Types

**Before (2.x):**
```csharp
public static Arbitrary<Vector2Int> ValidPosition(int width, int height)
{
    return Arb.From(
        from x in Gen.Choose(0, width - 1)
        from y in Gen.Choose(0, height - 1)
        select new Vector2Int(x, y)
    );
}
```

**After (3.x):**
```csharp
public static Gen<Vector2Int> ValidPosition(int width, int height)
{
    return
        from x in Gen.Choose(0, width - 1)
        from y in Gen.Choose(0, height - 1)
        select new Vector2Int(x, y);
}
```

**Key change**: Return `Gen<T>` directly, no `Arbitrary<T>` wrapper needed.

### 3. Using Generators in Tests

**Before (2.x):**
```csharp
[Fact]
public void MyPropertyTest()
{
    Check.QuickThrowOnFailure(
        Prop.ForAll(
            MyGenerator(),  // Arbitrary<T> passed directly
            value => { /* test */ }
        )
    );
}
```

**After (3.x):**
```csharp
[Fact]
public void MyPropertyTest()
{
    Prop.ForAll(
        MyGenerator().ToArbitrary(),  // Convert Gen<T> to Arbitrary
        value => { /* test */ }
    ).QuickCheckThrowOnFailure();  // Method moved to property
}
```

### 4. Combining Generators

**Before (2.x):**
```csharp
public static Arbitrary<Block> ValidBlock(int width, int height)
{
    return Arb.From(
        from position in ValidPosition(width, height).Generator
        from blockType in PrimaryBlockType().Generator
        select Block.CreateNew(blockType, position)
    );
}
```

**After (3.x):**
```csharp
public static Gen<Block> ValidBlock(int width, int height)
{
    return
        from position in ValidPosition(width, height)  // Direct Gen<T> usage
        from blockType in PrimaryBlockType()
        select Block.CreateNew(blockType, position);
}
```

### 5. Collection Generators

**Before (2.x):**
```csharp
Gen.ListOf(count, generator).Select(list => list.ToArray())
```

**After (3.x):**
```csharp
Gen.ArrayOf(generator).Resize(count)
```

## Migration Checklist

- [ ] Add `using FsCheck.Fluent;` to all test files
- [ ] Change all custom generator methods from `Arbitrary<T>` to `Gen<T>`
- [ ] Remove `Arb.From()` wrappers from generators
- [ ] Remove `.Generator` property access when combining generators
- [ ] Add `.ToArbitrary()` when passing generators to `Prop.ForAll`
- [ ] Update `Check.QuickThrowOnFailure(prop)` to `prop.QuickCheckThrowOnFailure()`
- [ ] Replace `Gen.ListOf(...).Select(x => x.ToArray())` with `Gen.ArrayOf(...).Resize(...)`
- [ ] Re-enable FsCheck.Xunit package reference in .csproj
- [ ] Run all tests to verify migration success

## Common Errors and Solutions

### Error: `Gen<T>` does not contain definition for 'ToArbitrary'
**Solution**: Add `using FsCheck.Fluent;`

### Error: Using generic type 'Arbitrary<T>' requires 1 type argument
**Solution**: Change return type to `Gen<T>`

### Error: 'Generator' not found
**Solution**: Remove `.Generator` - work with `Gen<T>` directly

### Error: Cannot find 'Arb.From'
**Solution**: Return the generator directly without wrapping

## Testing After Migration

1. **Build**: Ensure all files compile without errors
2. **Run Tests**: Verify all property-based tests pass
3. **Check Coverage**: Ensure test coverage remains the same
4. **Performance**: Property tests should run at similar speeds

## Benefits of FsCheck 3.x

- Cleaner API with less wrapper types
- Better separation between F# and C# APIs
- Improved performance for generator operations
- More intuitive generator composition
- Better type inference in LINQ expressions

## References

- [FsCheck GitHub Repository](https://github.com/fscheck/FsCheck)
- [FsCheck Release Notes](https://github.com/fscheck/FsCheck/blob/master/FsCheck%20Release%20Notes.md)
- Context7 Library ID: `/fscheck/fscheck`

## Example: Complete Migration

See the actual migration in:
- `tests/BlockLife.Core.Tests/Properties/BlockLifeGenerators.cs`
- `tests/BlockLife.Core.Tests/Properties/SimplePropertyTests.cs`

These files demonstrate all the patterns described above in a real-world context.