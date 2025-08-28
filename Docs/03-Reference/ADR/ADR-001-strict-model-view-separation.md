# ADR-001: Strict Model-View Separation

**Status**: Proposed  
**Date**: 2025-08-29  
**Decision Makers**: Product Owner (proposed), Tech Lead (to approve)

## Context

Darklands requires extensive modding support from day one. Modders should be able to:
- Add new items, enemies, skills without touching Godot
- Create new game mechanics in pure C#
- Test their mods without running Godot

## Decision

Enforce STRICT separation between game logic and presentation using MVP (Model-View-Presenter) pattern:

### Architecture Layers

```
src/
└── Darklands.Core.csproj    (Pure C# - NO Godot dependencies)
    ├── Domain/               (Game entities, value objects)
    ├── Application/          (Commands, Queries, Handlers - MediatR)
    ├── Features/             (Feature-based organization)
    │   └── [Feature]/
    │       ├── Commands/     (Feature commands)
    │       ├── Presenters/   (MVP Presenters - orchestrate logic)
    │       └── Views/        (View interfaces only)
    └── Infrastructure/       (Services, repositories)

tests/
└── Darklands.Core.Tests.csproj (Unit tests for Core)
    
Darklands.csproj              (Godot project - references Core)
└── godot_project/
    ├── scenes/               (Godot scenes)
    ├── features/             (Feature-based views)
    │   └── [feature]/
    │       └── XxxView.cs    (Godot view implementations)
    └── resources/            (Art, sounds, Godot assets)
```

### MVP Pattern Implementation

**Critical**: Presenters live in Core but NEVER reference Godot types directly

```csharp
// ✅ CORRECT - In Darklands.Core
namespace Darklands.Core.Features.Combat.Presenters;

public class CombatPresenter : PresenterBase<ICombatView>
{
    private readonly IMediator _mediator;
    
    public void ProcessAttack(GridCoordinates from, GridCoordinates to)
    {
        // Pure C# logic, testable without Godot
        var timeUnits = CalculateTimeUnits(from, to);
        View.UpdateTimeDisplay(timeUnits);
    }
}

// View interface in Core
public interface ICombatView
{
    void UpdateTimeDisplay(int timeUnits);
    IObservable<GridCoordinates> CellClicked { get; }
}

// ✅ CORRECT - In Darklands.Godot
namespace Darklands.Godot.Features.Combat;

public partial class CombatView : Control, ICombatView
{
    // Godot-specific implementation
    public void UpdateTimeDisplay(int timeUnits)
    {
        GetNode<Label>("TimeLabel").Text = $"Time: {timeUnits}";
    }
}
```

### Enforcement via .csproj (Based on BlockLife Pattern)

```xml
<!-- src/Darklands.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <ItemGroup>
    <!-- Core dependencies only - NO Godot! -->
    <PackageReference Include="LanguageExt.Core" Version="*" />
    <PackageReference Include="MediatR" Version="*" />
  </ItemGroup>
</Project>

<!-- Darklands.csproj (Root Godot Project) -->
<Project Sdk="Godot.NET.Sdk/4.4.1">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="src\Darklands.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <!-- Exclude src folder from Godot compilation -->
    <Compile Remove="src\**" />
    <EmbeddedResource Remove="src\**" />
    <None Remove="src\**" />
  </ItemGroup>
</Project>

<!-- tests/Darklands.Core.Tests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\src\Darklands.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="FluentAssertions" Version="8.6.0" />
    <PackageReference Include="FsCheck.Xunit" Version="3.3.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  </ItemGroup>
</Project>
```

## CI/CD Strategy

### What Gets Tested in CI
- **GitHub Actions**: Only tests `Darklands.Core.csproj` (pure C# tests)
- **No Godot in CI**: Integration tests run locally only
- **Fast feedback**: Core tests run in seconds, not minutes

### Why This Works
1. **Core logic is 80% of bugs** - Most issues are in game logic, not rendering
2. **Modders trust CI** - They see green checkmarks for Core logic
3. **Fast iteration** - PRs get feedback in <30 seconds
4. **No Godot setup pain** - No export templates, no cache issues

## Consequences

### Positive
- Mods can be pure C# DLLs without Godot dependency
- Core game logic is unit testable in CI without Godot
- MVP pattern makes UI changes independent of logic
- Clean architecture enforced by build system
- Can switch from Godot to another engine if needed
- CI runs in seconds, not minutes

### Negative  
- More initial setup complexity (three projects)
- Presenter/View separation requires discipline
- Must pass data between layers (no direct Godot node access)
- Some boilerplate for view interfaces

## Alternatives Considered

1. **Mixed Architecture** - Rejected: Makes modding require Godot knowledge
2. **Scripting Language** - Rejected: C# mods are more powerful
3. **Single Project** - Rejected: Can't enforce separation

## Implementation Notes

### Critical Setup Steps (Based on BlockLife)

1. **Create folder structure first**:
   ```
   /src/Darklands.Core.csproj
   /tests/Darklands.Core.Tests.csproj  
   /Darklands.csproj (root - Godot project)
   ```

2. **Key .csproj configurations**:
   - `<Compile Remove="src\**" />` in Godot project prevents dual compilation
   - `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` enforces quality
   - `<Nullable>enable</Nullable>` prevents null reference bugs

3. **Tech stack decisions needed**:
   - **LanguageExt**: For functional patterns (Option, Either, Validation)
   - **MediatR**: For command/query separation (optional but clean)
   - **FsCheck**: For property-based testing (great for time-unit logic)

### Build Commands

```bash
# Build just the core (fast, no Godot)
dotnet build src/Darklands.Core.csproj

# Run tests without Godot
dotnet test tests/Darklands.Core.Tests.csproj

# Build everything (slower, includes Godot)
dotnet build Darklands.csproj
```

**For Tech Lead**: This setup allows modders to reference only `Darklands.Core.dll` without needing Godot installed

## Status

⚠️ **Awaiting Tech Lead Review** - This architectural decision needs technical validation before proceeding with VS_001.