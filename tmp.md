### Overall
Strong, pragmatic architecture for Godot + Clean Architecture. It clearly separates domain logic from presentation, defines lifecycle/bootstrapping, and addresses async/error/perf concerns. A few critical correctness issues and some refinements will make it production‑ready.

### What’s excellent
- **Separation of concerns**: Commands/handlers/events keep Core pure; nodes are thin adapters.
- **Lifecycle discipline**: Creation/cleanup flows, `_ExitTree` cleanup, and bootstrap guard are well thought out.
- **Event usage guidance**: Clear “when to use EventBus vs Godot signals” with performance guardrails.
- **Async safety**: Good treatment of `async void` pitfalls and mitigation patterns.
- **Operational clarity**: Logging guidance, success metrics, and checklists are valuable for team alignment.

### Critical issues to fix
- **WeakReference is neutralized by strong delegate target (risk of memory leak)**
  - You store a strong reference to the handler delegate, which in .NET holds a strong reference to the target instance, defeating the `WeakReference`. This can leak nodes if `UnsubscribeAll` doesn’t fire.
  - Where this happens:
```186:196:Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md
private class WeakSubscription
{
    public WeakReference<object> Subscriber { get; }
    public Delegate Handler { get; }

    public WeakSubscription(object subscriber, Delegate handler)
    {
        Subscriber = new WeakReference<object>(subscriber);
        Handler = handler;
    }
}
```
```198:211:Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md
public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
    where TEvent : INotification
{
    var eventType = typeof(TEvent);
    lock (_lock)
    {
        if (!_subscriptions.TryGetValue(eventType, out var list))
        {
            list = new List<WeakSubscription>();
            _subscriptions[eventType] = list;
        }
        list.Add(new WeakSubscription(subscriber, handler));
    }
}
```
```259:266:Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md
foreach (var sub in snapshot)
{
    if (sub.Subscriber.TryGetTarget(out var target) && target is Node node && node.IsInsideTree())
    {
        // Thread-safe UI update for Godot: defer to main thread
        Callable.From(() => ((Action<TEvent>)sub.Handler).Invoke(notification)).CallDeferred();
    }
}
```
  - Fix: store `MethodInfo` (or an open static delegate) instead of a bound delegate, and invoke using the weak target at publish time. Example (proposed):
```csharp
private sealed class WeakSubscription
{
    public WeakReference<object> Target { get; }
    public MethodInfo Method { get; }

    public WeakSubscription(object subscriber, MethodInfo method)
    {
        Target = new WeakReference<object>(subscriber);
        Method = method;
    }
}

public void Subscribe<TEvent>(object subscriber, Action<TEvent> handler)
    where TEvent : INotification
{
    var eventType = typeof(TEvent);
    lock (_lock)
    {
        if (!_subscriptions.TryGetValue(eventType, out var list))
            _subscriptions[eventType] = list = new List<WeakSubscription>();
        list.Add(new WeakSubscription(subscriber, handler.Method));
    }
}

public Task PublishAsync<TEvent>(TEvent notification) where TEvent : INotification
{
    // snapshot as today...
    foreach (var ws in snapshot)
    {
        if (ws.Target.TryGetTarget(out var target) && target is Node node && node.IsInsideTree())
        {
            var evt = notification;
            Callable.From(() => ws.Method.Invoke(target, new object[] { evt })).CallDeferred();
        }
    }
    return Task.CompletedTask;
}
```
  - Consider injecting an `ILogger<GodotEventBus>` and wrapping the invoke in try/catch to log handler exceptions that occur on the deferred call.

- **Example code correctness mismatches (will confuse implementers)**
  - `ActorFactory.CreateActor` is `Result<ActorId>` but uses `await` and Godot API like `GetTree()` that a non‑Node service won’t have.
```534:565:Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md
public Result<ActorId> CreateActor(ActorDefinition definition, Vector2 position)
{
    // ...
    // 7. Publish creation event (optional)
    await _mediator.Publish(new ActorCreatedEvent(actorId, definition.Type));

    return Result.Success(actorId);
}
```
  - Recommendation:
    - Either make this a presentation‑side `Node` service that owns a `SceneTree` reference, or split into two: `ActorDomainFactory` (Core) and `ActorViewFactory` (Presentation) that binds `ActorId` to nodes and adds them to the tree.
    - Fix signatures (`async Task<Result<ActorId>>`) if you await, or remove awaiting and publish via fire‑and‑forget with logging.

- **`IComponentRegistry` interface vs example implementation are inconsistent**
```446:455:Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md
public interface IComponentRegistry
{
    Result<T> GetComponent<T>(ActorId actorId) where T : IComponent;
    Result AddComponent<T>(ActorId actorId, T component) where T : IComponent;
    Result RemoveComponent<T>(ActorId actorId) where T : IComponent;
    IEnumerable<T> GetAllComponents<T>() where T : IComponent;
    Result RemoveAll(ActorId actorId);
}
```
```456:499:Docs/03-Reference/ADR/ADR-002-godot-integration-architecture.md
public sealed class ComponentRegistry : IComponentRegistry
{
    // Implements GetComponent, AddComponent, RemoveAll
    // Missing: RemoveComponent<T>, GetAllComponents<T>
}
```
  - Also later examples incorrectly `await` registry calls that are synchronous. Standardize signatures and finish the example so it compiles conceptually.

### High‑value refinements
- **EventBus API**
  - Use non‑async `Publish` or return `Task.CompletedTask`; current `async` with no awaits adds overhead.
  - Prune empty lists by removing dictionary keys after pruning in `Publish` to keep the map tidy.
  - Consider `ValueTask` for MediatR adapters if you keep async.

- **Bootstrap**
  - The `SystemReady` guard is good. Add a short backoff or max retry log to avoid infinite deferral if misconfigured.
  - Consider an explicit “DIReady” autoload/singleton only for bootstrapping (you’ve cautioned against autoload in general; this narrow use can be acceptable).

- **Service access consistency**
  - You use both `ServiceLocator` and `ServiceLocatorBridge`. Prefer one façade (the bridge returning `Result<T>`) everywhere for symmetry and testability.

- **Performance**
  - For hot paths, prefer immutable snapshots (e.g., copy to `Array`) to reduce list reallocation churn under lock.
  - If bus traffic grows, consider per‑event struct storage or `ConcurrentDictionary<Type, ImmutableArray<...>>` to reduce lock time; only optimize if profiling shows need.

- **Safety**
  - Wrap deferred handler invocation with try/catch and log errors via injected logger; today a failing handler will surface on the main thread without structured logging.
  - In `EventAwareNode`, immediately null out references in `_ExitTree` after `UnsubscribeAll` (you do some of this already).

- **Docs clarity**
  - Call out that ADR snippets are illustrative and may omit imports/async details, to avoid copy‑paste errors.
  - Decide if factories are Core or Presentation responsibilities and state it explicitly to avoid mixed examples.
  - Keep naming consistent (`GameStrapper` vs “Bootstrapper”).

### Minor nits
- Consider `ILoggerFactory` in nodes when you need non‑generic loggers; otherwise your use of `ILogger<T>` assigned to `ILogger` is fine since `ILogger<T>` implements `ILogger`.
- Prefer `Node.CallDeferred` or `SceneTree.ProcessFrame +=` patterns if you ever need ordering relative to the scene tree; `Callable.From(...).CallDeferred()` is OK but generic.
- Add a brief note that record events must remain engine‑agnostic (no Godot types leaking into domain events).

### Key questions to decide now
- Is `ActorFactory` expected to be Godot‑aware (in Presentation/Infrastructure) or pure Core? If the latter, introduce `ActorViewFactory` to keep Godot APIs out of Core services.
- Do you want EventBus to swallow/log handler exceptions or let them bubble to Godot’s console? I recommend logging and isolating failures per handler.
- Will nodes always `UnsubscribeAll`? If not, implement the weak‑subscription fix above to avoid leaks.

- Very short wrap-up
  - Solid architecture and guidance; ready to implement.
  - Fix the weak subscription leak, align example signatures/layering, and unify service access.
  - Add small robustness/logging improvements to the bus and bootstrap.