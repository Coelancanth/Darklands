#!/bin/bash
# BlockLife Build Script
# Simple, no over-complexity approach

set -e

# Colors for output
CYAN='\033[0;36m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
GRAY='\033[0;90m'
NC='\033[0m' # No Color

write_step() {
    echo -e "\n${CYAN}→ $1${NC}"
}

execute_command() {
    echo -e "  ${GRAY}$1${NC}"
    eval $1
}

command=${1:-build}

case $command in
    clean)
        write_step "Cleaning build artifacts"
        execute_command "dotnet clean BlockLife.sln"
        [ -d ".godot/mono/temp/bin" ] && rm -rf .godot/mono/temp/bin
        echo -e "${GREEN}✓ Clean complete${NC}"
        ;;
        
    build)
        write_step "Building BlockLife"
        execute_command "dotnet build BlockLife.sln --configuration Debug"
        echo -e "${GREEN}✓ Build successful${NC}"
        ;;
        
    test)
        write_step "Building and running tests (safe default)"
        echo -e "  ${YELLOW}Building first to catch Godot compilation issues...${NC}"
        execute_command "dotnet build BlockLife.sln --configuration Debug"
        echo -e "${GREEN}✓ Build successful${NC}"
        write_step "Running tests"
        execute_command "dotnet test BlockLife.sln --configuration Debug --verbosity normal"
        echo -e "${GREEN}✓ Build and test complete - safe to commit${NC}"
        ;;
        
    test-only)
        write_step "Running tests only (development iteration)"
        echo -e "  ${YELLOW}⚠️  Note: This doesn't validate Godot compilation${NC}"
        execute_command "dotnet test BlockLife.sln --configuration Debug --verbosity normal"
        echo -e "${GREEN}✓ All tests passed${NC}"
        echo -e "  ${YELLOW}Remember to run 'test' (not 'test-only') before committing!${NC}"
        ;;
        
    run)
        write_step "Running BlockLife"
        echo -e "  ${YELLOW}Note: This requires Godot to be installed${NC}"
        if command -v godot &> /dev/null; then
            execute_command "godot"
        else
            echo -e "${RED}✗ Godot not found in PATH${NC}"
            echo -e "  ${YELLOW}Please install Godot 4.4 or add it to your PATH${NC}"
        fi
        ;;
        
    all)
        $0 clean
        $0 build
        $0 test
        echo -e "\n${GREEN}✓ All steps completed successfully${NC}"
        ;;
        
    *)
        echo "Usage: $0 {build|test|test-only|clean|run|all}"
        echo ""
        echo "Commands:"
        echo "  build      - Build the solution"
        echo "  test       - Build + run tests (safe default for commits)"
        echo "  test-only  - Run tests only (dev iteration, not for commits)"
        echo "  clean      - Clean build artifacts"
        echo "  run        - Run the game (requires Godot)"
        echo "  all        - Clean, build, and test"
        exit 1
        ;;
esac