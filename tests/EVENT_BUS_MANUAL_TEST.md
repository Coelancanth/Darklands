# EventBus Manual Test Procedure (VS_004 Phase 3)

**Status**: Implementation complete, awaiting manual Godot scene validation

## ✅ What's Complete

### Phase 1 & 2 (100%)
- [TestEvent.cs](../src/Darklands.Core/Domain/Events/TestEvent.cs) - Domain event
- [IGodotEventBus.cs](../src/Darklands.Core/Infrastructure/Events/IGodotEventBus.cs) - Core interface
- [UIEventForwarder.cs](../src/Darklands.Core/Infrastructure/Events/UIEventForwarder.cs) - MediatR bridge
- [GodotEventBus.cs](../Infrastructure/Events/GodotEventBus.cs) - Godot implementation
- **18/18 unit tests passing** - Including your requested MediatR type resolution tests

### Phase 3 (Implementation Complete)
- [EventAwareNode.cs](../Components/EventAwareNode.cs) - Base class with lifecycle management
- [EventBusTestListener.cs](EventBusTestListener.cs) - Example test node

## ⏳ What Remains (Manual Godot Scene Setup)

### 1. DI Registration (Main Scene)

Create `Main.cs` or add to existing entry point:

```csharp
using Godot;
using Darklands.Core.Application.Infrastructure;
using Darklands.Core.Infrastructure.Events;
using Darklands.Infrastructure.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public partial class Main : Node
{
    public override void _Ready()
    {
        // Initialize DI container
        var result = GameStrapper.Initialize(services =>
        {
            // Add logging (VS_003)
            services.AddLogging(/* Serilog config */);

            // Add MediatR (scan for IMediator core, NOT handlers)
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(IMediator).Assembly));

            // Add GodotEventBus
            services.AddSingleton<IGodotEventBus, GodotEventBus>();

            // Add UIEventForwarder with open generics
            // CRITICAL: Do NOT scan assembly for handlers (causes double-registration)
            services.AddTransient(typeof(INotificationHandler<>), typeof(UIEventForwarder<>));
        });

        if (result.IsFailure)
        {
            GD.PrintErr($"Failed to initialize DI: {result.Error}");
            GetTree().Quit();
        }

        GD.Print("✅ EventBus initialized successfully");
    }
}
```

### 2. Test Scene Creation

In Godot Editor:

1. Create new scene: `Tests/EventBusTestScene.tscn`
2. Root node: `Node` (or Control for UI)
3. Add children:
   ```
   EventBusTestScene (Node)
   ├─ TestListener (Script: EventBusTestListener.cs)
   ├─ Button (Godot Button)
   └─ MessageLabel (Godot Label)
   ```
4. Connect TestListener exports:
   - MessageLabel → MessageLabel node
   - TestButton → Button node

### 3. Manual Test Steps

1. **Run scene** in Godot
2. **Check console** for:
   ```
   ✅ EventBus initialized successfully
   [EventAwareNode] EventBusTestListener subscribed to events
   [EventBusTestListener] Subscribed to TestEvent
   ```

3. **Click button** → Check for:
   ```
   [EventBusTestListener] Button pressed - publishing TestEvent via MediatR
   [GodotEventBus] Publishing TestEvent to 1 subscribers
   [EventBusTestListener] Received TestEvent #1: Button pressed at 12345
   ```
   - **Label updates** with "Event #1: Button pressed..."

4. **Click multiple times** → Event count increments

5. **Close scene** → Check for:
   ```
   [GodotEventBus] Subscriber EventBusTestListener unsubscribed from 1 event types
   [EventAwareNode] EventBusTestListener unsubscribed from all events
   ```

### 4. Validation Checklist

- [ ] DI container initializes without errors
- [ ] EventBus resolves correctly in _Ready()
- [ ] Button click publishes event
- [ ] Label updates on each click
- [ ] Event count increments correctly
- [ ] Logs show complete flow: Publish → UIEventForwarder → GodotEventBus → Handler
- [ ] Scene close triggers UnsubscribeAll
- [ ] No errors or warnings in console
- [ ] CallDeferred executes on main thread (no threading errors)

## 🐛 Troubleshooting

### "Failed to resolve IGodotEventBus"
- **Cause**: GameStrapper not initialized before scene loads
- **Fix**: Ensure Main scene calls `GameStrapper.Initialize()` in _Ready()

### Events published twice
- **Cause**: MediatR registered UIEventForwarder via assembly scan AND open generics
- **Fix**: Only use open generic registration (see Main.cs example above)

### CallDeferred errors
- **Cause**: Godot 4 C# CallDeferred API changes
- **Fix**: See GodotEventBus.cs implementation using `Callable.From(() => ...).CallDeferred()`

### Memory leaks / stale subscriptions
- **Cause**: EventAwareNode._ExitTree() not called or base not called
- **Fix**: Always call `base._ExitTree()` in child classes

## 📊 Test Coverage Summary

**Unit Tests**: 18/18 passing ✅
- GodotEventBusTests (4 tests) - Interface contract
- UIEventForwarderTests (6 tests) - Including MediatR type resolution
- EventBusIntegrationTests (2 tests) - Full MediatR → EventBus flow

**Manual Tests**: Awaiting Godot scene validation ⏳
- EventBus lifecycle (subscribe/unsubscribe)
- CallDeferred thread marshalling
- Error isolation (one bad subscriber doesn't break others)
- Memory leak prevention (cleanup verification)

## 🎯 Success Criteria (from Backlog)

- ✅ Build succeeds: `dotnet build`
- ✅ Tests pass: `./scripts/core/build.ps1 test --filter "Category=Phase2"` (18/18)
- ⏳ TestEventBusScene manual test passes (button click → label updates)
- ✅ No Godot types in Core project (compile-time enforced)
- ⏳ Logs show complete event flow (manual test)
- ⏳ CallDeferred prevents threading errors (manual test)
- ⏳ EventAwareNode prevents leaks (manual test - check logs)

**Next Action**: Create Main.cs + EventBusTestScene.tscn → Run manual tests → Commit if green