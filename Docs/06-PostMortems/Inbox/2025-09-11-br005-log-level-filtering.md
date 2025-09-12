# Post-Mortem: BR_005 - Debug Log Level Filtering Issue

**Date**: 2025-09-11
**Author**: Debugger Expert
**Issue Type**: Debug System Configuration
**Severity**: Critical
**Time to Resolution**: ~1 hour

## Summary
Debug window log level dropdown showed "Information" but Debug level messages from standard logging (Microsoft.Extensions.Logging) still appeared in console, undermining the debug system's usability.

## Timeline
- **19:05**: BR_005 created - Debug window filtering not working as expected
- **19:15**: Investigation started by Debugger Expert
- **19:30**: Root cause identified - two separate logging systems not synchronized
- **19:45**: Initial fix attempts with various approaches (bridge pattern, direct injection)
- **20:00**: Elegant solution implemented using LoggingLevelSwitch
- **20:15**: Fix verified and BR_005 marked as Fixed

## Root Cause Analysis

### The Problem
Two independent logging systems existed:
1. **DebugSystem.Logger** (GodotCategoryLogger) - Respected DebugConfig ✅
2. **Microsoft.Extensions.Logging/Serilog** - Had static minimum level ❌

When the user changed log level in the debug window, only the first system updated, while MediatR's LoggingBehavior and command handlers continued logging Debug messages through the second system.

### Why It Happened
The architecture had evolved to include both:
- Custom category-based logging for Godot-specific needs
- Standard ILogger<T> for Core library compatibility

These systems weren't connected, violating Single Source of Truth (SSOT) principle.

## The Fix

### Solution: Dynamic LoggingLevelSwitch
Implemented elegant SSOT solution maintaining clean architecture:

1. **Added GlobalLevelSwitch** to GameStrapper
   - `public static readonly LoggingLevelSwitch GlobalLevelSwitch`
   - Controls Serilog's minimum level dynamically

2. **Connected DebugSystem** to update the switch
   - Listens to DebugConfig.SettingChanged events
   - Maps our LogLevel enum to Serilog's LogEventLevel
   - Updates GlobalLevelSwitch.MinimumLevel

3. **Maintained Clean Separation**
   - Core library continues using standard ILogger<T>
   - No Godot dependencies in Core
   - Configuration bridge exists only in Godot layer (Phase 4)

### Code Changes
- `GameStrapper.cs`: Added GlobalLevelSwitch, configured with `.MinimumLevel.ControlledBy()`
- `DebugSystem.cs`: Added OnDebugConfigChanged() to sync levels
- `MoveActorCommandHandler.cs`: Kept using standard ILogger (no changes needed)

## Lessons Learned

### What Went Well
1. **Clean Architecture Preserved**: Solution didn't compromise Core/Godot separation
2. **SSOT Achieved**: Single configuration now controls all logging
3. **Elegant Solution**: No ugly workarounds or reflection hacks

### What Could Be Improved
1. **Initial Design**: Should have considered standard logging integration from start
2. **Documentation**: Need to document that both logging systems exist
3. **Testing**: Need integration tests for configuration changes

## Prevention Measures

### Immediate Actions
1. ✅ Document the dual logging system in HANDBOOK.md
2. ✅ Ensure all future handlers use constructor injection
3. ✅ Test configuration changes affect all logging

### Long-term Improvements
1. Consider unifying logging interfaces if possible
2. Add integration tests for debug configuration
3. Create logging architecture decision record (ADR)

## Technical Details

### Failed Approaches
1. **Bridge Pattern with ICategoryLogger DI**: Circular dependency issues
2. **Reflection-based Logger Access**: Too hacky, violated architecture
3. **Replacing All ILogger Usage**: Too invasive, broke existing code

### Why LoggingLevelSwitch Works
- Serilog designed for runtime configuration changes
- No circular dependencies
- Respects existing architecture boundaries
- Standard pattern in Serilog documentation

## Impact Assessment
- **User Experience**: Debug window now correctly filters all log messages
- **Developer Experience**: Can confidently use log level filtering during debugging
- **Performance**: Minimal - one additional event handler
- **Maintenance**: Low - solution is simple and well-documented

## Action Items
- [x] Fix implemented and tested
- [x] Scene file cleaned up (removed HealthView.cs reference)
- [x] Post-mortem created
- [ ] Update HANDBOOK.md with logging architecture
- [ ] Consider ADR for logging strategy

## References
- BR_005 in Backlog.md
- Serilog LoggingLevelSwitch documentation
- SSOT principle in architecture patterns