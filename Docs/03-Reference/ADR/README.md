# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) - documents that capture important architectural decisions made for the Darklands project.

## Active ADRs

- [ADR-001: Strict Model View Separation](ADR-001-strict-model-view-separation.md) - **Approved**
  - Establishes strict separation between domain models and view components
  
- [ADR-002: Phased Implementation Protocol](ADR-002-phased-implementation-protocol.md) - **Approved**
  - Mandates Domain → Application → Infrastructure → Presentation phases
  
- [ADR-003: TileMap Variant Selection Strategy](ADR-003-tilemap-variant-selection-strategy.md) - **Approved**
  - Hybrid approach: Domain decides WHAT tiles, Godot's autotiling selects WHICH variants
  
- [ADR-008: Functional Error Handling with LanguageExt v5](ADR-008-functional-error-handling.md) - **Approved**
  - Establishes functional error handling patterns using Fin<T>, replacing try/catch in business logic
  
- [ADR-009: Sequential Turn-Based Processing](ADR-009-sequential-turn-processing.md) - **Approved**
  - Mandates synchronous, sequential turn processing without async/await in game logic
  
- [ADR-010: UI Event Bus Architecture](ADR-010-ui-event-bus-architecture.md) - **Approved**
  - Establishes UIEventBus pattern for routing domain events to Godot UI components
  
- [ADR-011: Godot Resource Bridge Pattern](ADR-011-godot-resource-bridge-pattern.md) - **Proposed**
  - Infrastructure layer bridges Godot Resources to Domain models while preserving Clean Architecture

- [ADR-012: Localization Bridge Pattern](ADR-012-localization-bridge-pattern.md) - **Approved**
  - Infrastructure bridge to Godot's TranslationServer for i18n support
  
- [ADR-013: Time-Based Action Scheduling](ADR-013-time-based-action-scheduling.md) - **Proposed**
  - Use time-based scheduling instead of energy accumulation for action timing

- [ADR-014: Vision-Based Tactical System](ADR-014-vision-based-tactical-system.md) - **Approved**
  - Establishes vision/FOV as the core mechanism for tactical mode activation
  
- [ADR-015: Namespace Organization Strategy](ADR-015-namespace-organization-strategy.md) - **Proposed**
  - Resolves namespace-class collisions using Bounded Context organization

- [ADR-016: Embrace Engine Scene Graph for UI Composition](ADR-016-embrace-engine-scene-graph.md) - **Proposed**
  - Use parent-child relationships for naturally coupled UI elements instead of fighting the engine

## Missing ADRs (Need Creation)

The following concepts are referenced in persona documents but lack formal ADRs:
- **Memory Bank Protocol**: Referenced as "ADR-004 v3.0" in all personas
- **Persona Completion Authority**: Referenced as "ADR-005" in all personas  
- **Model-First Development**: Referenced as "ADR-006" in tech-lead.md

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