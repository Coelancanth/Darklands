# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) - documents that capture important architectural decisions made for the Darklands project.

## Active ADRs

- [ADR-001: Pattern Recognition Framework](ADR-001-pattern-recognition-framework.md) - **Approved**
  - Establishes patterns for identifying and reusing code structures
  
- [ADR-002: Phased Implementation Protocol](ADR-002-phased-implementation-protocol.md) - **Approved**
  - Mandates Domain → Application → Infrastructure → Presentation phases
  
- [ADR-003: Memory Bank Architecture](ADR-003-memory-bank-architecture.md) - **Approved**
  - Single-repo memory bank with auto-sync via embody.ps1
  
- [ADR-004: Embody Script v4.0](ADR-004-embody-script-v4.md) - **Approved**
  - Intelligent Git sync with automatic squash merge resolution
  
- [ADR-005: Persona Completion Authority](ADR-005-persona-completion-authority.md) - **Approved**
  - Personas are advisors; only users mark work as complete
  
- [ADR-006: Model-First Development](ADR-006-model-first-development.md) - **Approved**
  - Always implement domain model before UI
  
- [ADR-007: TileMap Variant Selection Strategy](ADR-007-tilemap-variant-selection-strategy.md) - **Proposed**
  - Hybrid approach: Domain decides WHAT tiles, Godot's autotiling selects WHICH variants
  
- [ADR-008: Functional Error Handling with LanguageExt v5](ADR-008-functional-error-handling.md) - **Approved**
  - Establishes functional error handling patterns using Fin<T>, replacing try/catch in business logic

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