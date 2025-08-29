# Commit Message Conventions

This project follows [Conventional Commits](https://www.conventionalcommits.org/) with auto-fix capabilities in our git hooks.

## Quick Reference

**Format**: `type(scope): description`  
**Example**: `feat(combat): add time-based turn system`

## Supported Commit Types

Our hooks support and auto-suggest the following types:

| Type | Emoji | Description | When to Use |
|------|-------|-------------|-------------|
| **feat** | ✨ | New feature or functionality | Adding new capabilities, user-facing features |
| **fix** | 🐛 | Bug fix or error correction | Fixing broken functionality, resolving issues |
| **docs** | 📚 | Documentation changes only | README updates, code comments, guides |
| **style** | 💄 | Code style/formatting | Formatting, whitespace, semicolons (no logic change) |
| **refactor** | ♻️ | Code restructuring | Improving code structure without changing behavior |
| **perf** | ⚡ | Performance improvements | Making code faster, reducing memory usage |
| **test** | ✅ | Adding or updating tests | Unit tests, integration tests, test fixes |
| **build** | 🏗️ | Build system or dependencies | Project configuration, compilation settings |
| **ci** | 👷 | CI/CD pipeline changes | GitHub Actions, build scripts, deployment |
| **chore** | 🔧 | Maintenance tasks | Routine tasks, cleanup, tooling |
| **revert** | ⏪ | Reverting previous commit | Undoing a previous change |
| **wip** | 🚧 | Work in progress | Temporary commits (use sparingly!) |
| **deps** | 📦 | Dependency updates | Package updates, library upgrades |
| **security** | 🔒 | Security fixes | Vulnerability patches, security improvements |
| **breaking** | 💥 | Breaking changes | Changes requiring major version bump |

## Scopes

Common scopes in our project:

- **domain**: Domain layer changes
- **app**: Application layer changes  
- **infra**: Infrastructure layer changes
- **tests**: Test-related changes
- **ci**: CI/CD configuration
- **scripts**: Build/utility scripts

## Smart Auto-Detection

Our commit-msg hook automatically detects the appropriate type based on:

1. **Keywords in your message**:
   - "fix", "bug", "issue" → `fix`
   - "add", "implement", "feature" → `feat`
   - "performance", "optimize", "faster" → `perf`
   - "security", "vulnerability", "CVE" → `security`

2. **Changed files**:
   - `src/Domain/*` → `feat(domain)`
   - `tests/*` → `test(tests)`
   - `.github/workflows/*` → `ci(ci)`
   - `*.md` files → `docs`

## Examples

### Feature Development
```bash
feat(combat): implement time-based turn system
feat(ui): add health bar visualization
feat(domain): create Character entity with attributes
```

### Bug Fixes
```bash
fix(combat): resolve negative damage calculation
fix(infra): correct database connection timeout
fix: prevent crash on empty inventory
```

### Performance
```bash
perf(combat): optimize pathfinding algorithm
perf(domain): cache frequently accessed calculations
perf: reduce memory allocation in game loop
```

### Documentation
```bash
docs: update README with setup instructions
docs(api): add endpoint documentation
docs: fix typos in code comments
```

### CI/CD and Build
```bash
ci: add auto-merge for safe dependencies
ci(github): enhance workflow with caching
build: update to .NET 8.0
deps: upgrade MediatR to v12.0
```

### Refactoring
```bash
refactor(domain): extract combat logic to separate class
refactor: simplify error handling pattern
refactor(tests): reorganize test structure
```

### Breaking Changes
```bash
breaking(api): change response format to JSON
breaking: rename core interfaces
breaking(domain): change entity ID from int to Guid
```

## Phase Markers (Darklands Specific)

For phased implementation following our ADR-002:

```bash
feat(combat): implement TimeUnit value object [Phase 1/4]
feat(combat): add combat action commands [Phase 2/4]
feat(combat): integrate state persistence [Phase 3/4]
feat(combat): create combat UI [Phase 4/4]
```

## Auto-Fix Behavior

When you attempt to commit with an invalid format:

1. **Hook analyzes your message and files**
2. **Suggests the correct format** based on:
   - Keywords in your message
   - Files you're changing
   - Project conventions
3. **Saves suggestion** to `.git/COMMIT_MSG_SUGGESTION`
4. **Shows exact command** to fix: `git commit --amend -m "suggested message"`

### Example Auto-Fix

```bash
$ git commit -m "fixed the bug in combat"

🔍 Validating commit message format...
🔧 Auto-suggesting commit format...
❌ Invalid commit message format!

Your message: fixed the bug in combat

🔧 AUTO-SUGGESTED FORMAT:
  fix(domain): fixed the bug in combat

To use this suggestion, run:
  git commit --amend -m "fix(domain): fixed the bug in combat"

💡 Suggestion saved to .git/COMMIT_MSG_SUGGESTION
```

## Tips

1. **Let the hook help you**: Write your message naturally, the hook will suggest the right format
2. **Be specific in scope**: Use `(domain)`, `(app)`, `(infra)` for clarity
3. **Keep descriptions concise**: Under 50 characters for the subject line
4. **Use present tense**: "add feature" not "added feature"
5. **No period at end**: "fix bug" not "fix bug."

## Why This Matters

- **Automated changelog generation**: Tools can create release notes automatically
- **Easier navigation**: `git log --grep="^feat"` shows all features
- **Clear communication**: Team instantly understands change impact
- **Semantic versioning**: Automatically determine version bumps
- **Better PR reviews**: Reviewers know what to focus on

## Enforced By

- **Pre-commit hook**: Validates format
- **Pre-push hook**: Warns about non-conventional commits
- **CI/CD**: Validates all commits in PR
- **Auto-suggestion**: Helps you fix it instantly

Remember: The hooks are here to help, not hinder. They'll auto-suggest the right format based on your changes!