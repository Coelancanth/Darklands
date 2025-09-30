# 🚀 EventBus Quick Start

**Status**: ✅ Ready to test in Godot Editor

---

## ⚡ 30-Second Test

1. **Open Godot**: `godot4 project.godot`
2. **Open scene**: `Tests/EventBusTestScene.tscn` (in FileSystem dock)
3. **Run scene**: Press **F6**
4. **Click button**: "Publish TestEvent"
5. **See label update**: "Event #1: Button pressed at..."
6. **Check console**: Should show complete event flow

**Expected Output**:
```
✅ DI Container initialized successfully
✅ EventBus ready
[EventBusTestListener] Subscribed to TestEvent
[EventBusTestListener] 🔵 Button pressed - publishing TestEvent
[EventBusTestListener] ✅ Received TestEvent #1: Button pressed at 12345
```

---

## 📚 Documentation

- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Comprehensive testing instructions
- **[Tests/EVENT_BUS_MANUAL_TEST.md](Tests/EVENT_BUS_MANUAL_TEST.md)** - Original implementation notes

---

## 🏗️ Architecture at a Glance

```
User clicks button
    ↓
EventBusTestListener.OnButtonPressed()
    ↓
EventBus.PublishAsync(new TestEvent(...))
    ↓
GodotEventBus forwards to all subscribers
    ↓
EventBusTestListener.OnTestEvent(evt) [via CallDeferred]
    ↓
Label.Text = "Event #1: ..."
```

**Key Files**:
- [Main.cs](Main.cs) - DI setup (EventBus, Logging, MediatR)
- [Components/EventAwareNode.cs](Components/EventAwareNode.cs) - Base class with lifecycle
- [Infrastructure/Events/GodotEventBus.cs](Infrastructure/Events/GodotEventBus.cs) - Thread-safe implementation

---

## ✅ Verification Checklist

If everything works, you should see:

- [x] **25/25 tests pass** - `dotnet test`
- [ ] **Button updates label** - Manual test in Godot
- [ ] **Logs show event flow** - Output panel
- [ ] **Cleanup on scene close** - "unsubscribed from all events"
- [ ] **No errors/warnings** - Clean console

---

## 🐛 Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| "Failed to resolve IGodotEventBus" | Main scene should auto-load (already configured in project.godot) |
| Button doesn't work | Check Output: Should see "Button connected" |
| Events not received | Verify base._Ready() called in EventBusTestListener |
| Scene won't run | Build project: `dotnet build Darklands.csproj` |

---

## 🎯 What This Validates

✅ **Architecture** (ADR-002):
- Core has zero Godot dependencies
- IGodotEventBus abstraction enables testability
- ServiceLocator bridges Godot → DI container

✅ **Thread Safety**:
- ConcurrentDictionary for subscriptions
- CallDeferred marshals to main thread
- No race conditions

✅ **Memory Safety**:
- EventAwareNode enforces explicit lifecycle
- UnsubscribeAll in _ExitTree prevents leaks
- Strong references are debuggable

✅ **Your MediatR Concern**:
- Type resolution tests verify correct generic instances
- No double-registration (fixed in Main.cs)
- Open generics eliminate boilerplate

---

**Ready to test?** Open Godot and press F6 on EventBusTestScene.tscn! 🎮