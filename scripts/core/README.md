# Core Build Scripts

Essential build, clean, and run operations for BlockLife.

## Available Scripts

### Windows (PowerShell)
```powershell
# Core build operations
.\core\build.ps1 build      # Build the solution  
.\core\build.ps1 test       # Build + run tests (safe default)
.\core\build.ps1 test-only  # Run tests only (dev iteration)
.\core\build.ps1 clean      # Clean build artifacts
.\core\build.ps1 run        # Launch the game (requires Godot)
.\core\build.ps1 all        # Clean, build, and test
```

### Linux/Mac (Bash)
```bash
# Core build operations  
./core/build.sh build       # Build the solution
./core/build.sh test        # Build + run tests (safe default)
./core/build.sh test-only   # Run tests only (dev iteration)
./core/build.sh clean       # Clean build artifacts
./core/build.sh run         # Launch the game (requires Godot)
./core/build.sh all         # Clean, build, and test
```

## Usage Guidelines

### For Development
- Use `../test/quick.ps1` for rapid feedback (architecture tests, 1.3s)
- Use `test-only` for tests without rebuild
- Use `test` before committing (full validation)
- Use `../test/full.ps1` for complete test coverage

### Test Script Integration (TD_071)
The core build scripts focus on **build + all tests**. For **selective testing**, use:
- `../test/quick.ps1` - Architecture only (1.3s)
- `../test/full.ps1` - Staged execution (3-5s)
- `../test/incremental.ps1` - Changed code only (coming, TD_077)

### For CI/CD
- GitHub Actions uses these scripts for consistent builds
- Pre-commit hooks can optionally use `../test/quick.ps1`
- Full test suite still runs via `build.ps1 test`

## Design Principles

1. **Simple and Fast** - Direct dotnet commands, minimal overhead
2. **Safe Defaults** - `test` command builds first to catch Godot issues
3. **Cross-Platform** - Identical functionality on Windows/Linux/Mac
4. **CI Integration** - Used by both local development and GitHub Actions