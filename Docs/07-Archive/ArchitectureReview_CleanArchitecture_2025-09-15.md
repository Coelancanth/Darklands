# Clean Architecture Compliance Report

**Date**: 2025-09-15
**Persona**: Tech Lead
**Scope**: Full repository review against Clean Architecture and ADRs (ADR-001, -002, -004, -005, -006, -009, -010, -011, -012, -018)

## Executive Summary

Overall, the codebase is strongly aligned with Clean Architecture: core logic is isolated in `src`, the Godot project excludes `src` and `tests` from compilation, CQRS via MediatR and functional error handling via LanguageExt are consistently applied, and determinism abstraction (`IDeterministicRandom`) is implemented and registered. The MVP presentation layer exists and bridges to Application without leaking Godot types into Domain.

However, there are several critical gaps to address immediately:
- ADR-009 violations: `Task.Run` usage in `GameManager.cs`, `Views/GridView.cs`, and `src/Presentation/Presenters/ActorPresenter.cs` introduces concurrency in a sequential turn model.
- ADR-004 violations: Floating-point arithmetic in Domain (`ShadowcastingFOV`, `Position`, `Movement`, `Health`) risks cross-platform nondeterminism.
- Solution hygiene: `Darklands.sln` does not include `Darklands.csproj`, hurting DX.
- Structural clarity: `src/Core/Domain/Services/IScopeManager.cs` coexists with `ServiceLocator` and presentation-scoped DI; clarify placement to avoid confusion.

## Architecture Layout and Layering

- `Darklands.csproj` references `src/Darklands.Core.csproj` and explicitly excludes `src/**` and `tests/**` from Godot compilation, enforcing separation.
- `src/Darklands.Core.csproj` targets net8, references DI, logging, LanguageExt, MediatR, and has no Godot packages. TreatWarningsAsErrors enabled.
- Layers:
  - Domain: `src/Domain/**` (records/value objects, events, determinism, grid, combat, vision)
  - Application: `src/Application/**` (Commands/Queries/Handlers/Services, MediatR behaviors)
  - Infrastructure: `src/Infrastructure/**` (DI bootstrap, logging, adapters, identity, services)
  - Presentation (Core): `src/Presentation/**` (presenters, view interfaces)
  - Godot-side Presentation: `Views/**`, `GameManager.cs`, `Presentation/Infrastructure/**`

Dependency flow complies: Presentation -> Application -> Domain; Infrastructure depends on Domain and provides implementations; Application depends on abstractions, not Godot.

## Findings by Layer

### Domain
- Entities and values use `record`/`record struct`. Example compliance:
```12:14:src/Domain/Grid/Grid.cs
public sealed record Grid(
    GridId Id,
```
- IDs are value types (`ActorId`, `GridId`), complying with ADR-005 (save-ready, ID references only).
- Determinism: `IDeterministicRandom` and `DeterministicRandom` implemented and used by infra identity generator. DI registration present.
```285:293:src/Infrastructure/DependencyInjection/GameStrapper.cs
services.AddSingleton<Domain.Determinism.IDeterministicRandom>(provider =>
{
    const ulong developmentSeed = 12345UL;
    var logger = provider.GetService<ILogger<Domain.Determinism.DeterministicRandom>>();
    return new Domain.Determinism.DeterministicRandom(developmentSeed, logger: logger);
});
```
- Issues:
  - Floating-point math in Domain:
```98:101:src/Domain/Vision/ShadowcastingFOV.cs
double tileSlopeHigh = distance == 0 ? 1.0 : (angle + 0.5) / (distance - 0.5);
double tileSlopeLow = (angle - 0.5) / (distance + 0.5);
double prevTileSlopeLow = (angle + 0.5) / (distance + 0.5);
```
```29:33:src/Domain/Grid/Position.cs
public double EuclideanDistanceTo(Position other)
{
    var dx = X - other.X;
    var dy = Y - other.Y;
    return Math.Sqrt(dx * dx + dy * dy);
}
```
```31:34:src/Domain/Actor/Health.cs
public double HealthPercentage => Maximum > 0 ? (double)Current / Maximum : 0.0;
```
```31:34:src/Domain/Grid/Movement.cs
public double EuclideanDistance => From.EuclideanDistanceTo(To);
```
  - Domain events depend on MediatR `INotification` directly:
```19:24:src/Domain/Combat/ActorDamagedEvent.cs
public sealed record ActorDamagedEvent(... ) : INotification
```
This couples Domain to MediatR. Acceptable if MediatR is treated as an application-level contract, but stricter Clean Architecture would define a domain-local notification abstraction and adapt in Application.

### Application
- Extensive use of MediatR for Commands/Queries/Handlers; LanguageExt `Fin<T>` used consistently. No Godot dependencies found in Application search.
- UI event forwarding pattern exists via `UIEventForwarder<T>` implementing `INotificationHandler<T>`, bridging domain events to UI bus.
- Coordinators and state services follow DI and interface segregation; in-memory services used for current phase.

### Infrastructure
- Composition root `GameStrapper` configures logging, MediatR pipeline behaviors (`LoggingBehavior`, `ErrorHandlingBehavior`), state services, and determinism/identity services.
- Identity: `DeterministicIdGenerator` consumes `IDeterministicRandom` for deterministic GUIDs.
- Logging abstraction via `ICategoryLogger` adapters and composite outputs.
- Mock services for Audio/Input/Settings aligned to ADR-006 selective abstraction.
- Note: `MockInputService` contains `Task.Run` for simulated events:
```128:130:src/Infrastructure/Services/MockInputService.cs
Task.Run(async () =>
```
For simulation this is acceptable within Infrastructure tests but should avoid impacting game loop determinism. Consider replacing with scheduled ticks or reactive streams that remain single-threaded.

### Presentation
- Presenters in `src/Presentation` use interfaces to views; no Godot types leak into core Presenters.
- Godot views under `Views/` implement those interfaces and use `CallDeferred` for main thread updates.
- Issues (ADR-009):
```55:69:GameManager.cs
_ = Task.Run(async () =>
{
    await CompleteInitializationAsync();
```
```321:333:Views/GridView.cs
_ = Task.Run(async () =>
{
    await _presenter.HandleTileClickAsync(gridPosition);
```
```80:88:src/Presentation/Presenters/ActorPresenter.cs
_ = Task.Run(async () => { await View.DisplayActorAsync(...); ... });
```
Replace with synchronous calls, `.GetAwaiter().GetResult()`, or use `CallDeferred` to marshal to main thread without adding concurrency.

## Cross-Cutting Concerns

- Determinism (ADR-004): Strong RNG abstraction and DI registration; tests enforce no `System.Random`. However, floating-point usage remains in Domain algorithms and metrics. Replace with fixed-point (`Fixed` type exists) and integer percentages.
- Save-Ready (ADR-005): Domain uses records and IDs; no Godot types in Domain. Continue to enforce with architecture tests.
- Selective Abstraction (ADR-006): Audio/Input/Settings abstracted; UI not abstracted. Compliance good.
- Sequential Turn Processing (ADR-009): Violations present in presentation as noted.
- UI Event Bus (ADR-010): Present and used via event forwarding and `EventAwareNode`.
- DI Lifecycle (ADR-018): `ServiceLocator` autoload with scope manager extensions present; `src/Core/Domain/Services/IScopeManager.cs` co-located under `src/Core` can be confusing. Ensure single authoritative contract location and consistent use.

## Risks and Impact

- Floating-point nondeterminism can cause replay/save divergence across platforms and compiler targets.
- `Task.Run` can introduce race conditions, leading to intermittent bugs that are hard to reproduce (already evidenced by BR history).
- Solution file omissions slow onboarding and CI integration.
- DI lifecycle ambiguity can cause memory leaks or dangling scopes in long-running scenes.

## Recommendations and Actions

1) Eliminate Task.Run in Presentation (Critical, ADR-009)
- `GameManager._Ready`: replace background initialization with deferred main-thread sequence; call sync `.GetAwaiter().GetResult()` where needed.
- `Views/GridView.HandleMouseClick`: replace `Task.Run` with `CallDeferred` wrapper invoking `_presenter.HandleTileClickAsync(...).GetAwaiter().GetResult()`.
- `ActorPresenter.Initialize`: replace display calls with main-thread deferred sync.
→ Tracked as TD_039 in `Backlog.md` with detailed before/after diffs.

2) Replace Domain floating-point with Fixed-point (Critical, ADR-004)
- `ShadowcastingFOV`: replace double slopes and params with `Fixed` operations.
- `Position.EuclideanDistanceTo`, `Movement.EuclideanDistance`, `Health.HealthPercentage`: avoid double; use integer-scaled or `Fixed` return values; expose helper to format for UI.
→ Tracked as TD_040 with precise code transforms.

3) Add Godot project to solution (High DX)
- Update `Darklands.sln` to include `Darklands.csproj` under the root so IDE builds and navigation include UI project.

4) Clarify DI lifecycle contracts (Medium)
- Move `IScopeManager` out of `src/Core` into `src/Presentation/Infrastructure` or `src/Infrastructure/DependencyInjection` (depending on usage), or promote to `src/Application` if treated as cross-cutting. Ensure `ServiceLocator` is the single entry for Godot autoload and `NodeServiceExtensions` primary access.

5) Strengthen architecture tests (Nice-to-have)
- Add tests to flag `double/float` usage in Domain gameplay types.
- Add tests to fail on `Task.Run` inside `Views/` and `src/Presentation/` except clearly whitelisted test utilities.

## Evidence Snippets

```55:69:GameManager.cs
_ = Task.Run(async () =>
{
    try
    {
        await CompleteInitializationAsync();
```

```321:333:Views/GridView.cs
_ = Task.Run(async () =>
{
    try
    {
        await _presenter.HandleTileClickAsync(gridPosition);
```

```98:101:src/Domain/Vision/ShadowcastingFOV.cs
double tileSlopeHigh = distance == 0 ? 1.0 : (angle + 0.5) / (distance - 0.5);
double tileSlopeLow = (angle - 0.5) / (distance + 0.5);
```

```6:9:Darklands.sln
Project(...) = "Darklands.Core", "src\Darklands.Core.csproj", ...
// Missing: Darklands.csproj entry
```

## Conclusion

The project is close to exemplary Clean Architecture for a Godot+C# game. Address the identified ADR-004 and ADR-009 violations and tidy up solution and DI lifecycle organization to solidify the foundation. The suggested TD items already documented in `Backlog.md` map directly to these fixes; prioritize TD_039 and TD_040 next.


