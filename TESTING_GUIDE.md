# 🧪 EventBus Testing Guide

## 📦 What Was Created

### Core Files
- [Main.cs](Main.cs) - Entry point with DI initialization
- [Main.tscn](Main.tscn) - Main scene (autoload)
- [Tests/EventBusTestScene.tscn](Tests/EventBusTestScene.tscn) - Test scene with UI
- [Tests/EventBusTestListener.cs](Tests/EventBusTestListener.cs) - Event subscriber node

### Architecture Files (Already Completed)
- ✅ [src/Darklands.Core/Domain/Events/TestEvent.cs](src/Darklands.Core/Domain/Events/TestEvent.cs)
- ✅ [src/Darklands.Core/Infrastructure/Events/IGodotEventBus.cs](src/Darklands.Core/Infrastructure/Events/IGodotEventBus.cs)
- ✅ [src/Darklands.Core/Infrastructure/Events/UIEventForwarder.cs](src/Darklands.Core/Infrastructure/Events/UIEventForwarder.cs)
- ✅ [Infrastructure/Events/GodotEventBus.cs](Infrastructure/Events/GodotEventBus.cs)
- ✅ [Components/EventAwareNode.cs](Components/EventAwareNode.cs)

---

## 🚀 How to Test in Godot Editor

### Step 1: Open Project in Godot 4
```bash
# If Godot isn't open already:
cd c:\Users\Coel\Documents\Godot\darklands
godot4 .
```

Or double-click `project.godot` in Windows Explorer.

### Step 2: Configure Main Scene as Autoload (One-time setup)

1. In Godot Editor: **Project → Project Settings**
2. Go to **Autoload** tab
3. Click **Add** (folder icon)
4. Select `res://Main.tscn`
5. Node Name: `Main`
6. Click **Add**
7. Click **Close**

This ensures DI container initializes BEFORE any scene loads.

### Step 3: Open Test Scene

1. In **FileSystem** dock (bottom-left), navigate to `Tests/`
2. Double-click `EventBusTestScene.tscn`
3. Scene should open showing:
   - Title: "EventBus Test Scene"
   - Label: "Click button to publish event"
   - Button: "Publish TestEvent"
   - Instructions below

### Step 4: Run Test Scene

1. Press **F6** (Run Current Scene) or click ▶️ button with scene icon
2. **Watch the Output panel** (bottom) - You should see:

```
🎮 Darklands - Initializing...
📦 Services registered:
   - Logging (Serilog → Console + File)
   - MediatR (IMediator only, handlers via open generics)
   - IGodotEventBus → GodotEventBus
   - UIEventForwarder<T> (open generic registration)
✅ DI Container initialized successfully
✅ EventBus ready
✅ Logging configured
✅ MediatR registered

🎯 Ready to load scenes!
[GodotEventBus] GodotEventBus initialized
[EventAwareNode] EventBusTestListener subscribed to events
[EventBusTestListener] Subscribed to TestEvent
[EventBusTestListener] Button connected
```

### Step 5: Test Event Flow

1. **Click "Publish TestEvent" button**
2. Watch Output panel - should see:

```
[EventBusTestListener] 🔵 Button pressed - publishing TestEvent
[GodotEventBus] Publishing TestEvent to 1 subscribers
[EventBusTestListener] ✅ Received TestEvent #1: Button pressed at 12345
```

3. **Label updates** to show: `Event #1: Button pressed at 12345`

4. **Click multiple times** → Event count increments (Event #2, #3, etc.)

### Step 6: Test Cleanup

1. **Close the running scene** (press ESC or close window)
2. Watch Output panel for cleanup:

```
[GodotEventBus] Subscriber EventBusTestListener unsubscribed from 1 event types
[EventAwareNode] EventBusTestListener unsubscribed from all events
```

---

## ✅ Success Criteria Checklist

- [ ] **DI initializes**: See "✅ DI Container initialized successfully"
- [ ] **EventBus resolves**: See "EventAwareNode ... subscribed to events"
- [ ] **Button works**: Label updates on click
- [ ] **Event flow complete**: See "Button pressed → Publishing → Received"
- [ ] **CallDeferred works**: No threading errors (events delivered on main thread)
- [ ] **Cleanup works**: See "unsubscribed from all events" on scene close
- [ ] **No errors/warnings**: Output panel clean (except Fluent Assertions license warning)

---

## 🐛 Troubleshooting

### "Failed to resolve IGodotEventBus"
**Problem**: DI container not initialized
**Solution**: Ensure Main scene is configured as Autoload (Step 2)

### "Failed to find TestButton in scene"
**Problem**: Node path incorrect
**Solution**: Open EventBusTestScene.tscn, verify structure matches:
```
EventBusTestScene (Control)
├─ CenterContainer
│  └─ VBoxContainer
│     ├─ MessageLabel (Label)
│     └─ TestButton (Button)
└─ TestListener (Node with EventBusTestListener.cs)
```

### Button clicks don't trigger events
**Problem**: Button not connected or EventBus null
**Check logs**: Should see "Button connected" on scene load
**Solution**: Verify TestListener is in scene and base._Ready() is called

### Events published twice
**Problem**: Double-registration bug (MediatR scan + open generics)
**Solution**: Main.cs already fixed - only scans `typeof(IMediator).Assembly`

### CallDeferred errors
**Problem**: Godot 4 API change
**Solution**: GodotEventBus uses `Callable.From(() => ...).CallDeferred()` (already implemented)

---

## 📊 What This Tests

### ✅ Automated Tests (25/25 passing)
- IGodotEventBus interface contract
- UIEventForwarder type resolution (your MediatR concern!)
- MediatR → UIEventForwarder → GodotEventBus integration
- Open generic registration correctness

### 🧪 Manual Tests (This Guide)
- **DI Container**: GameStrapper.Initialize() works in Godot
- **EventBus Lifecycle**: Subscribe in _Ready, unsubscribe in _ExitTree
- **Thread Marshalling**: CallDeferred delivers events on main thread
- **Event Flow**: Complete path from publish to handler invocation
- **Memory Safety**: UnsubscribeAll prevents leaks
- **UI Integration**: Godot UI updates from domain events

---

## 🎯 Next Steps After Testing

### If Tests Pass ✅
1. Commit test scene files:
   ```bash
   git add Main.cs Main.tscn Tests/EventBusTestScene.tscn Tests/EventBusTestListener.cs TESTING_GUIDE.md
   git commit -m "test: EventBus manual test scene [VS_004]"
   ```

2. Update backlog:
   - Mark VS_004 as **Done**
   - Move to VS_001 (Health System) - now unblocked!

3. Merge to main:
   ```bash
   git checkout main
   git merge feat/VS_004-event-bus
   ```

### If Tests Fail ❌
1. Check Output panel for specific error
2. Refer to Troubleshooting section above
3. If issue persists, share logs for debugging

---

`✶ Insight ─────────────────────────────────────`
**Manual Testing Value**: While automated tests (25/25) verify logic, manual testing validates:
1. **Godot Integration** - ServiceLocator bridge works in real Godot runtime
2. **CallDeferred Behavior** - Events actually marshal to main thread (can't mock this)
3. **Lifecycle Management** - _Ready() and _ExitTree() hooks fire correctly
4. **Visual Feedback** - UI updates confirm complete event flow
`─────────────────────────────────────────────────`

---

## 📸 Expected Output Screenshot

When test passes, you should see:
```
┌─────────────────────────────────┐
│  EventBus Test Scene            │
│                                 │
│  Event #3: Button pressed at... │
│                                 │
│  [Publish TestEvent]            │
│                                 │
│  Watch Output panel for logs... │
└─────────────────────────────────┘
```

**Output Panel**:
```
[EventBusTestListener] 🔵 Button pressed - publishing TestEvent
[GodotEventBus] Publishing TestEvent to 1 subscribers
[EventBusTestListener] ✅ Received TestEvent #3: Button pressed at 12345
```

---

**Ready to test!** Open Godot Editor and follow the steps above. 🚀