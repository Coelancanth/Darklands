# ADR-001: Clean Architecture Foundation

**Status**: Approved
**Date**: 2025-09-30
**Decision Makers**: Tech Lead, Product Owner

## Context

Darklands is a turn-based tactical roguelike that requires:
- **Extensive modding support** - Modders can create new content without Godot
- **Testable game logic** - Core rules testable without running the game engine
- **Framework independence** - Can switch engines if Godot doesn't work out
- **Fast CI/CD** - Tests run in seconds, not minutes

Traditional game development mixes engine code with game logic, making testing and modding difficult.

## Decision

We enforce **strict Clean Architecture separation** between game logic and Godot framework using a 3-project structure.

### Project Structure

```
darklands/
├── src/
│   └── Darklands.Core/
│       └── Darklands.Core.csproj    ← Pure C#, NO Godot dependencies
│           ├── Domain/               ← Entities, Value Objects, Components
│           ├── Application/          ← Commands, Queries, Handlers (MediatR)
│           └── Infrastructure/       ← Services, DI, Event Bus
│
├── tests/
│   └── Darklands.Core.Tests/
│       └── Darklands.Core.Tests.csproj    ← Test Core without Godot
│
└── Darklands.csproj                 ← Root Godot project
    ├── Components/                  ← Godot component nodes
    ├── Scenes/                      ← Godot .tscn files
    └── Resources/                   ← Assets, sprites, sounds
```

### Dependency Rules

**Darklands.Core.csproj** (Pure C#):
- ✅ CAN reference: CSharpFunctionalExtensions, MediatR, Serilog
- ❌ CANNOT reference: Godot, GodotSharp, any framework

**Darklands.csproj** (Godot):
- ✅ CAN reference: Darklands.Core.csproj
- ✅ CAN reference: Godot, GodotSharp
- ✅ Implements presentation layer only

### Tech Stack

| Library | Purpose | Version |
|---------|---------|---------|
| **CSharpFunctionalExtensions** | Result/Maybe types, railway-oriented programming | Latest stable |
| **MediatR** | CQRS pattern (Commands, Queries, Events) | Latest stable |
| **Serilog** | Structured logging | Latest stable |
| **Microsoft.Extensions.DependencyInjection** | DI container | Latest stable |
| **xUnit** | Unit testing | Latest stable |
| **FluentAssertions** | Test assertions | Latest stable |

**Why CSharpFunctionalExtensions instead of LanguageExt?**
- Lightweight (200KB vs 5MB)
- Stable releases (not beta)
- C#-idiomatic (not F#-inspired)
- Covers 100% of our needs

### Clean Architecture Layers

```
┌─────────────────────────────────────────┐
│ Domain Layer (Pure C#)                  │
│ - Entities (Actor, Grid)                │
│ - Value Objects (Health, Position)      │
│ - Components (IHealthComponent, etc.)   │
│ - Business Rules                         │
└─────────────┬───────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ Application Layer (Pure C#)             │
│ - Commands (ExecuteAttackCommand)       │
│ - Queries (GetActorQuery)               │
│ - Handlers (ExecuteAttackCommandHandler)│
│ - Events (AttackExecutedEvent)          │
│ - Services (IActorStateService)         │
└─────────────┬───────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ Infrastructure Layer (Pure C#)          │
│ - DI Container (GameStrapper)           │
│ - Event Bus (GodotEventBus)             │
│ - Service Implementations               │
└─────────────┬───────────────────────────┘
              ↓
┌─────────────────────────────────────────┐
│ Presentation Layer (Godot C#)           │
│ - Component Nodes (HealthComponentNode) │
│ - Scenes (.tscn files)                  │
│ - Input handling                         │
│ - Visual updates                         │
└─────────────────────────────────────────┘
```

### .csproj Enforcement

**src/Darklands.Core/Darklands.Core.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <ItemGroup>
    <!-- NO Godot packages allowed here! -->
    <PackageReference Include="CSharpFunctionalExtensions" Version="*" />
    <PackageReference Include="MediatR" Version="*" />
    <PackageReference Include="Serilog" Version="*" />
    <PackageReference Include="Serilog.Sinks.Console" Version="*" />
    <PackageReference Include="Serilog.Sinks.File" Version="*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="*" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="*" />
  </ItemGroup>
</Project>
```

**Darklands.csproj** (Root):
```xml
<Project Sdk="Godot.NET.Sdk/4.4.1">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Core project -->
    <ProjectReference Include="src\Darklands.Core\Darklands.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Exclude Core from Godot compilation -->
    <Compile Remove="src\**" />
    <Compile Remove="tests\**" />
    <EmbeddedResource Remove="src\**" />
    <EmbeddedResource Remove="tests\**" />
  </ItemGroup>
</Project>
```

### Modding Support

Modders write pure C# code in Darklands.Core:

```csharp
// Modder creates new attack type
public record CastFireballCommand(
    ActorId CasterId,
    Position Target
) : IRequest<Result<SpellResult>>;

public class CastFireballCommandHandler
    : IRequestHandler<CastFireballCommand, Result<SpellResult>>
{
    public async Task<Result<SpellResult>> Handle(...)
    {
        // Pure C# game logic
        var damage = CalculateFireDamage(caster);
        var affectedCells = GetAoECells(target, radius: 2);

        // Apply damage, publish events
        return Result.Success(new SpellResult(...));
    }
}
```

**Modders can:**
- Write handlers in pure C#
- Test without Godot (`dotnet test`)
- Distribute as DLLs
- Optionally provide Godot scenes for visuals

## CI/CD Strategy

### GitHub Actions Workflow

```yaml
name: Core Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      # Build Core (no Godot needed!)
      - run: dotnet build src/Darklands.Core/Darklands.Core.csproj

      # Run tests (fast!)
      - run: dotnet test tests/Darklands.Core.Tests/Darklands.Core.Tests.csproj
```

**Benefits:**
- Tests run in < 30 seconds
- No Godot installation needed in CI
- No export templates, no cache issues
- Fast feedback on PRs

## Consequences

### Positive
- ✅ **Modding**: Pure C# mods without Godot
- ✅ **Testability**: Core logic testable in milliseconds
- ✅ **CI Speed**: 30 seconds vs 5 minutes
- ✅ **Framework Independence**: Can switch engines
- ✅ **Clean Boundaries**: Clear separation of concerns
- ✅ **Compiler Enforcement**: .csproj prevents Godot references

### Negative
- ❌ **Initial Complexity**: Three projects instead of one
- ❌ **Discipline Required**: Must resist putting logic in Godot layer
- ❌ **Data Passing**: Must pass data between layers explicitly

### Neutral
- ➖ **Learning Curve**: Team must understand Clean Architecture
- ➖ **More Boilerplate**: Commands, handlers, events require more files

## Alternatives Considered

### 1. Single Project (All Code in Godot Project)
**Rejected**: Cannot enforce separation, modding requires Godot

### 2. Dual Project (Core + Godot Only)
**Rejected**: Tests and Core should be separate for CI optimization

### 3. Scripting Language for Mods (Lua, JavaScript)
**Rejected**: C# mods are more powerful and type-safe

## Success Metrics

- ✅ Core tests complete in < 30 seconds
- ✅ Zero Godot references in Darklands.Core.csproj
- ✅ Modders can reference only Core.dll
- ✅ 80%+ code coverage in Core project
- ✅ CI runs without Godot installation

## References

- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) - Robert C. Martin
- [Ports and Adapters](https://alistair.cockburn.us/hexagonal-architecture/) - Alistair Cockburn
- [Railway Oriented Programming](https://fsharpforfunandprofit.com/rop/) - Scott Wlaschin