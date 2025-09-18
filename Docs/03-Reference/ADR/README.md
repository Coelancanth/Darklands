# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) - documents that capture important architectural decisions made for the Darklands project.

## Active ADRs

### Foundational Architecture
- [ADR-001: Strict Model View Separation](ADR-001-strict-model-view-separation.md) - **Approved** *(Updated 2025-09-16 for ADR-021)*
  - Establishes strict MVP separation enforced by project boundaries

- [ADR-002: Phased Implementation Protocol](ADR-002-phased-implementation-protocol.md) - **Approved**
  - Mandates Domain → Application → Infrastructure → Presentation phases

- [ADR-004: Deterministic Simulation](ADR-004-deterministic-simulation.md) - **Approved** ⭐ **CRITICAL**
  - Enforces deterministic behavior for saves, debugging, and replay

- [ADR-005: Save-Ready Architecture](ADR-005-save-ready-architecture.md) - **Approved** ⭐ **CRITICAL**
  - Entities use records and ID references for serialization-ready design

- [ADR-006: Selective Abstraction Strategy](ADR-006-selective-abstraction-strategy.md) - **Approved** ⭐ **CRITICAL**
  - Defines what to abstract (Audio, Input, Random) vs use directly (UI, Particles)

### Project Organization
- [ADR-018: Godot DI Lifecycle Alignment](ADR-018-godot-di-lifecycle-alignment.md) - **Approved** *(Updated 2025-09-16)*
  - Aligns MS.DI scopes with Godot scene lifecycle using MVP pattern

- [ADR-019: .csproj-Enforced Clean Architecture](ADR-019-csproj-enforced-clean-architecture.md) - **REJECTED**
  - Over-engineered project separation (superseded by ADR-021)

- [ADR-020: Feature-Based VSA Organization](ADR-020-feature-based-vsa-organization.md) - **Approved**
  - Namespace organization by features: World, Characters, Combat, Vision

- [ADR-021: Minimal Project Separation](ADR-021-minimal-project-separation.md) - **Approved** ⭐ **CRITICAL**
  - 4-project separation with compile-time MVP enforcement

### Domain Patterns
- [ADR-008: Functional Error Handling](ADR-008-functional-error-handling.md) - **Approved**
  - LanguageExt Fin<T> patterns replacing try/catch in business logic

- [ADR-009: Sequential Turn Processing](ADR-009-sequential-turn-processing.md) - **Approved**
  - Synchronous, sequential turn processing without async/await in game logic

- [ADR-014: Vision-Based Tactical System](ADR-014-vision-based-tactical-system.md) - **Approved**
  - Vision/FOV as core mechanism for tactical mode activation

### Infrastructure Patterns
- [ADR-007: Unified Logger Architecture](ADR-007-unified-logger-architecture.md) - **Approved**
  - Category-based logging with Godot integration

- [ADR-010: UI Event Bus Architecture](ADR-010-ui-event-bus-architecture.md) - **Approved** *(Updated 2025-09-16)*
  - Event bus for Domain→Presenter event routing (EventAwarePresenter pattern)

- [ADR-011: Godot Resource Bridge Pattern](ADR-011-godot-resource-bridge-pattern.md) - **Proposed** *(Updated 2025-09-16)*
  - Bridge Godot Resources to Domain models while preserving Clean Architecture

- [ADR-012: Localization Bridge Pattern](ADR-012-localization-bridge-pattern.md) - **Approved**
  - Infrastructure bridge to Godot's TranslationServer for i18n

### Architecture Patterns
- [ADR-022: Logical-Visual Position Separation](ADR-022-logical-visual-position-separation.md) - **Approved** ⭐ **REVISED 2025-09-18**
  - Decouples game logic timing from visual presentation timing
  - Amendment 1: Supports both discrete and interpolated visual updates

- [ADR-023: Layered Game State Management](ADR-023-game-state-management.md) - **Approved**
  - Three-layer state system for game flow, combat, and UI overlays

### Game Design
- [ADR-003: TileMap Variant Selection Strategy](ADR-003-tilemap-variant-selection-strategy.md) - **Approved**
  - Domain decides WHAT tiles, Godot autotiling selects WHICH variants

- [ADR-013: Time-Based Action Scheduling](ADR-013-time-based-action-scheduling.md) - **Proposed**
  - Time-based scheduling instead of energy accumulation for action timing

- [ADR-015: Namespace Organization Strategy](ADR-015-namespace-organization-strategy.md) - **Proposed**
  - Resolves namespace-class collisions using Bounded Context organization

- [ADR-016: Embrace Engine Scene Graph for UI Composition](ADR-016-embrace-engine-scene-graph.md) - **Proposed**
  - Use parent-child relationships for naturally coupled UI elements

## Consistency Status

**Last Full Review**: 2025-09-16 - All ADRs updated for MVP-enforced project separation (ADR-021)

**Critical Dependencies**:
- ADR-021 supersedes ADR-019 (project separation)
- ADR-018 updated for MVP pattern (EventAwarePresenter)
- ADR-010 updated for Presenter-based event handling
- ADR-001 updated for 4-project structure

## ADR Template

Use [template.md](template.md) when creating new ADRs.

## ADR Process

1. **Identify** significant architectural decisions during development
2. **Draft** ADR using the template
3. **Status**: Start as "Proposed"
4. **Review** with team or during Tech Lead review
5. **Update** status to "Approved" or "Rejected"
6. **Reference** in code comments where decision impacts implementation

## Status Definitions

- **Proposed**: Under consideration
- **Approved**: Accepted and should be followed
- **Rejected**: Considered but not adopted
- **Deprecated**: Was approved but no longer applies
- **Superseded**: Replaced by another ADR