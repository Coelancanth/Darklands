# 🚀 EventBus Quick Start

**Status**: ✅ Ready to test in Godot Editor
**Location**: `TestScenes/EventBusTestScene.tscn`

---

## ⚡ 30-Second Test

1. **Open Godot**: `godot4 project.godot`
2. **Open scene**: `TestScenes/EventBusTestScene.tscn` (in FileSystem dock)
3. **Run scene**: Press **F6**
4. **Check status**: Green status label = "✅ EventBus initialized"
5. **Click button**: "Publish TestEvent"
6. **See label update**: "Event #1: Button pressed at..."
7. **Check console**: Detailed event flow logs

---

## 🎯 What You Should See

### Status Label (Green = Success):
```
✅ EventBus initialized
✅ Button connected
Ready to test!
```

### When Button Clicked (Output Panel):
```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔵 Button pressed!
📤 Publishing TestEvent...
✅ PublishAsync called with: Button pressed at 12345
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[GodotEventBus] Publishing TestEvent to 1 subscribers
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ EVENT RECEIVED #1: Button pressed at 12345
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

### Message Label Updates:
```
Event #1: Button pressed at 12345
Event #2: Button pressed at 13456
Event #3: Button pressed at 14567
```

---

## 🐛 If Button Does Nothing

### Red Status Label = DI Not Initialized:
```
❌ ERROR: EventBus not initialized
Check Output panel
```

**Cause**: Main scene didn't run before test scene
**Solution**: Main is configured as autoload in project.godot (already done)

### Check Output Panel For:
```
❌ EventBus is NULL - DI container not initialized!
```

**Debug Steps**:
1. Check if you see Main initialization logs:
   ```
   🎮 Darklands - Initializing...
   ✅ DI Container initialized successfully
   ✅ EventBus ready
   ```

2. If NOT seeing Main logs → Main autoload not running
   - Close Godot
   - Reopen project
   - Try again

3. If still failing → Run Main.tscn directly first:
   - Open `Main.tscn`
   - Press F5
   - Then run EventBusTestScene

---

## 📝 Troubleshooting Checklist

- [ ] **Main autoload configured**: Check project.godot has `Main="*res://Main.tscn"`
- [ ] **Build succeeded**: Run `dotnet build Darklands.csproj`
- [ ] **Status label green**: EventBus initialized successfully
- [ ] **Output logs show**: Main initialization messages
- [ ] **Button connected**: See "✅ Button connected" in logs

---

## 📊 Architecture Validated

When test passes, you've verified:

✅ **DI Container** - GameStrapper.Initialize() works in Godot
✅ **EventBus Resolution** - ServiceLocator bridges Godot → DI
✅ **Event Flow** - Publish → GodotEventBus → Subscriber
✅ **CallDeferred** - Main thread marshalling works
✅ **Lifecycle** - Subscribe in _Ready, unsubscribe in _ExitTree
✅ **UI Integration** - Domain events update Godot UI

---

`✶ Insight ─────────────────────────────────────`
**Enhanced Debugging**: The new test scene provides:
1. **Visual Status** - Green/Red label shows DI state immediately
2. **Detailed Logging** - Border markers make event flow obvious
3. **Null Checks** - Explicit EventBus null checks prevent silent failures
4. **ServiceLocator Test** - Direct verification of DI resolution

This diagnostic approach helps identify WHERE initialization fails.
`─────────────────────────────────────────────────`

---

## 🎮 Next Steps After Success

1. **Commit the test**:
   ```bash
   git add testscenes/
   git commit -m "test: move EventBus test to testscenes/"
   ```

2. **Mark VS_004 complete** in backlog

3. **Start VS_001** (Health System) - now unblocked!

---

**Ready to test!** Open `testscenes/EventBusTestScene.tscn` and press F6. 🚀