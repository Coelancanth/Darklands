# Bug Post-Mortem Template

## Bug ID: [YYYY-MM-DD-ShortName]

### Summary
One sentence description of the bug and its architectural impact.

### Timeline
- **Introduced**: [Commit/Date when bug was likely introduced]
- **Discovered**: [How it was found - test failure, runtime, code review]
- **Fixed**: [Commit reference]

### Root Cause Analysis

#### Immediate Cause
What broke in the code.

#### Architectural Cause
Which architectural principle was violated:
- [ ] Dependency Inversion (concrete dependency where abstraction needed)
- [ ] Single Responsibility (class doing too much)
- [ ] Interface Segregation (fat interface)
- [ ] Explicit Dependencies (hidden dependency)
- [ ] Command/Query Separation (state change outside handler)

#### Process Cause
How did this slip through:
- [ ] Missing test coverage
- [ ] Inadequate type constraints
- [ ] Documentation gap
- [ ] Code review miss
- [ ] Didn't query Context7 for API documentation
- [ ] Assumed method/behavior without verification

### Impact Analysis
- **Scope**: Which layers affected (Core/Presentation/Infrastructure)
- **Blast Radius**: Other components that could have similar issues
- **Technical Debt**: What refactoring is now needed

### Fix Details
```csharp
// Before (problematic code)

// After (fixed code)
```

### Prevention Measures

#### Immediate Actions
- [ ] Add specific test case
- [ ] Update style guide rule
- [ ] Add compiler warning/analyzer
- [ ] Document API gotcha in QuickReference.md
- [ ] Add Context7 query example for this scenario

#### Systemic Changes
- [ ] Architectural constraint to enforce
- [ ] Testing strategy adjustment
- [ ] DI registration pattern change

### Lessons Learned
Key takeaway for the team about Clean Architecture in practice.

---

**Related:**