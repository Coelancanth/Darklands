# Clean Architecture Review Report

**Date**: 2025-09-15
**Author**: Tech Lead
**Status**: COMPLETE

## 1. Executive Summary

This report provides a comprehensive analysis of the Darklands codebase's alignment with Clean Architecture principles as of 2025-09-15.

The overall architecture is strong and demonstrates a solid understanding of Clean Architecture, CQRS, and Domain-Driven Design principles. The core business logic (`Darklands.Core`) is commendably decoupled from the Godot presentation layer, with excellent separation of concerns, adherence to the dependency rule, and consistent use of functional error handling (`LanguageExt`) and CQRS (`MediatR`).

However, several critical architectural violations and structural issues have been identified that directly contradict our established Architectural Decision Records (ADRs). The most severe issue is the improper use of `Task.Run` in the presentation layer, which violates ADR-009 (Sequential Turn Processing) and introduces significant risk of race conditions. Additionally, the Visual Studio solution is incomplete, and there are known determinism violations that must be addressed.

This report details the findings, identifies critical issues, and provides actionable recommendations for remediation.

## 2. Overall Architecture Assessment

The project structure and code organization show a mature and disciplined approach to software architecture.

### ✅ Strengths & Alignment

*   **Excellent Dependency Management**: The core project (`Darklands.Core.csproj`) has **zero dependencies on Godot**, perfectly adhering to the Dependency Rule. The main Godot project (`Darklands.csproj`) correctly references the core project, ensuring dependencies flow in the right direction (from UI towards the Domain).
*   **Clean Domain Layer**: The `src/Domain` layer is pure C#. A search for `Godot` dependencies reveals only comments explaining the intentional avoidance of framework types. Entities like `Actor.cs` are implemented as immutable records and use `Fin<T>` for robust, functional error handling, aligning with ADR-005 (Save-Ready) and ADR-008 (Functional Error Handling).
*   **CQRS Pattern**: The `src/Application` layer is well-structured around the Command Query Responsibility Segregation pattern using MediatR. Use cases are clearly defined as `Commands` and `Queries` with their corresponding `Handlers`, creating a clean and scalable application core.
*   **Dependency Inversion**: The `Application` layer correctly depends on abstractions (interfaces like `IActorStateService`), while the `src/Infrastructure` layer provides the concrete implementations (e.g., `InMemoryActorStateService`). This is a textbook example of Dependency Inversion.
*   **MVP in Presentation Layer**: The Godot-side code correctly implements the Model-View-Presenter (MVP) pattern.
    *   **Views** (`GridView.cs`, `ActorView.cs`) are concrete Godot nodes responsible only for UI rendering and input.
    *   **Presenters** (`GridPresenter.cs`) contain the presentation logic, are decoupled from concrete views via interfaces (`IGridView`), and communicate with the application layer exclusively through MediatR commands, which is the correct interaction pattern.
*   **Centralized DI Setup**: Dependency Injection is correctly managed in `GameStrapper.cs`, which acts as the composition root for the core application, configuring services, logging, and the MediatR pipeline.

## 3. 🚨 Critical Issues and Architectural Violations

Despite the strong foundation, several critical issues require immediate attention. These are not minor style points; they are direct violations of our core architectural principles.

### 3.1. CRITICAL: Concurrency Violations (ADR-009)

The most severe issue is the improper use of `Task.Run` for handling UI events and initialization, which breaks our sequential processing model (ADR-009) and is a known cause of race conditions (ref: BR_007).

*   **Violation**: `GameManager.cs:56`
    *   **Code**: `_ = Task.Run(async () => { await CompleteInitializationAsync(); });`
    *   **Impact**: Kicking off initialization on a background thread can lead to race conditions where game logic starts before the system is fully ready. Initialization must be sequential.
*   **Violation**: `Views/GridView.cs:322`
    *   **Code**: `_ = Task.Run(async () => { await _presenter.HandleTileClickAsync(gridPosition); });`
    *   **Impact**: Handling player input on a background thread for a turn-based game is fundamentally incorrect. It breaks the sequential nature of turns and can lead to multiple actions being processed concurrently, corrupting game state.
*   **Violation**: `ActorPresenter.cs` (per TD_039)
    *   **Impact**: Similar `Task.Run` usage in the actor presenter for display operations introduces further concurrency issues.

**Recommendation**: These violations must be fixed immediately. The work is already captured in **TD_039**, which should be prioritized. The correct pattern is synchronous invocation with `.GetAwaiter().GetResult()` or using Godot's `CallDeferred` for main-thread safety, as outlined in the TD.

### 3.2. CRITICAL: Determinism Violations (ADR-004)

The codebase contains floating-point arithmetic in gameplay-critical calculations, which is a direct violation of our determinism rules (ADR-004). This can cause save-game incompatibility and simulation divergence across different platforms.

*   **Violation**: `src/Domain/Vision/ShadowcastingFOV.cs`
    *   **Impact**: Uses `double` for slope calculations. Floating-point math is not deterministic across different hardware architectures (e.g., x86 vs. ARM).
    *   **Recommendation**: This work is captured in **TD_040**. It needs to be implemented by replacing `double` with the provided `Fixed.cs` fixed-point math implementation.

### 3.3. HIGH: Incomplete Solution File (`.sln`)

The `Darklands.sln` file is missing the main Godot project (`Darklands.csproj`).

*   **Impact**: This severely degrades the developer experience. It's impossible to build the full game from Visual Studio or Rider, and project references are not easily discoverable. This must be fixed for any new developer to be productive.
*   **Recommendation**: The `Darklands.csproj` project must be added to `Darklands.sln`.

### 3.4. MEDIUM: Confusing Project Structure (`src/Core`)

The presence of a `src/Core` directory that contains a `Domain/Services` subdirectory is confusing and redundant given the top-level `src/Domain` and `src/Application` folders.

*   **Impact**: It creates ambiguity about where domain services and core interfaces should reside. It appears to be a remnant of a previous structure.
*   **Recommendation**: Consolidate all domain logic into `src/Domain` and application logic into `src/Application`. The `src/Core` directory should be removed, and its files refactored into the appropriate layers. This aligns with the intent of **TD_032** (Fix Namespace-Class Collisions).

### 3.5. MEDIUM: DI Lifecycle and Memory Management

As identified in the backlog, the current DI implementation has known issues.

*   **Impact**: Potential memory leaks and incorrect service lifetimes, as described in **TD_041**. The current `GameStrapper` approach is not robust enough for production-level scene and node lifecycle management in Godot.
*   **Recommendation**: The solution proposed in **TD_041** (implementing a `GodotScopeManager` with `ConditionalWeakTable`) should be implemented to provide a production-ready DI scope management system.

## 4. Actionable Recommendations

Based on this review, I propose the following actions, in order of priority:

1.  **Prioritize and Implement TD_039 (Remove Task.Run)**: This is the highest-priority architectural fix. The stability of the turn-based system depends on it.
2.  **Prioritize and Implement TD_040 (Replace Double Math)**: This is critical for long-term save compatibility and deterministic gameplay, a cornerstone of our architecture.
3.  **Fix the Solution File**: Add the `Darklands.csproj` to `Darklands.sln`. This is a low-effort, high-impact fix for developer productivity.
4.  **Implement TD_041 (DI Lifecycle Management)**: Fix the memory leaks and scope management issues before they become more deeply integrated into the codebase.
5.  **Execute TD_032 (Namespace Refactoring)**: As part of this, clean up the `src/Core` vs. `src/Domain` directory structure to create a single, unambiguous location for all domain logic.

By addressing these issues, we can solidify our architectural foundation and ensure the project remains scalable, maintainable, and aligned with its core technical principles.
