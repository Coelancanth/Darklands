# BlockLife Design Documentation

This folder contains both game design and development workflow design specifications.

## 📚 Document Structure

### Game Design
- **[Game/](Game/)** - Creative vision and game mechanics
  - **[Vision.md](Game/Vision.md)** ⭐⭐⭐⭐⭐ - THE complete game design document
  - Core mechanics, future systems, design philosophy

### Development Protocols
- **[Protocols/](Protocols/)** - Workflow design and AI persona protocols
  - **[BranchAndCommitDecisionProtocols.md](Protocols/BranchAndCommitDecisionProtocols.md)** ⭐⭐⭐⭐⭐ - Git workflow protocols
  - Decision trees, automation systems, quality standards

### Archived Ideas
- **[../07-Archive/Future-Ideas/](../07-Archive/Future-Ideas/)** - Premature detailed specs
  - Moved here to prevent confusion
  - Will be resurrected only when needed
  - Not promises, just possibilities


## 🎮 For AI Personas

### Game Design (Creative Authority: User)
**IMPORTANT**: The user is the Game Designer. Game design documents represent their creative vision.
- **Reference Only** - Never modify game design without Game Designer approval
- **Ask When Unclear** - If design intent is ambiguous, ask the user
- **Build What's in Backlog** - Not everything in Vision is approved for building

### Development Protocols (Implementation Authority: DevOps Engineer)
**IMPORTANT**: Development protocols define how we work, not what we build.
- **Follow Decision Trees** - Use documented protocols for workflow decisions
- **Leverage Automation** - Use provided scripts and intelligent tooling
- **Suggest Improvements** - Identify gaps or unclear scenarios in protocols

## 🎯 Domain Separation

### Game Design Domain
- **What**: Game mechanics, player experience, creative vision
- **Authority**: Game Designer (User)
- **Documents**: Game/Vision.md
- **Modification**: Requires explicit Game Designer approval

### Development Protocol Domain  
- **What**: Workflow processes, automation, quality standards
- **Authority**: DevOps Engineer + Tech Lead
- **Documents**: Protocols/ folder
- **Modification**: Can be improved based on development experience

## 📋 Quick Navigation

### For Game Implementation
1. Read **[Game/Vision.md](Game/Vision.md)** for creative requirements
2. Check **[Backlog.md](../01-Active/Backlog.md)** for approved work items
3. Follow implementation patterns in **[HANDBOOK.md](../03-Reference/HANDBOOK.md)**

### For Workflow Decisions
1. Read **[Protocols/](Protocols/)** for workflow guidance
2. Use automation scripts in **[scripts/](../../scripts/)**
3. Follow git protocols and quality standards

---

*Separate concerns: Game design defines WHAT to build, protocols define HOW to build it.*