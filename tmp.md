Godot Engine v4.4.1.stable.mono.official.49a5bc7b6 - https://godotengine.org
OpenGL API 3.3.0 Core Profile Context 23.40.02.240110 - Compatibility - Using Device: ATI Technologies Inc. - AMD Radeon(TM) Graphics

GameManager autoload starting initialization...
Initializing DI container...
[17:48:08] [INF] [System] DI container initialized successfully - unified logging active
[ServiceLocator] Initialized successfully with scope manager
[17:48:08] [INF] [System] ServiceLocator initialized with GodotScopeManager
[17:48:08] [DEB] [System] [UIEventBus] Initialized - Ready to bridge domain events to UI
[17:48:08] [DEB] [System] [UIEventBus] Subscriber {SubscriberType} registered for {EventType} (Total: {Count})
[17:48:08] [DEB] [System] [UIEventBus] Subscriber {SubscriberType} registered for {EventType} (Total: {Count})
[17:48:08] [INF] [System] Successfully subscribed to domain events via UI Event Bus
[17:48:08] [INF] [System] Modern event architecture active - static router fully replaced
[17:48:08] [INF] [System] GameManager successfully subscribed to domain events
[17:48:08] [INF] [System] GameManager initialization completed successfully
[ServiceLocator] Autoload ready at /root/ServiceLocator
[UIDispatcher] Initialized at /root/UIDispatcher
[17:48:08] [INF] [System] DebugSystem initialized successfully
[17:48:08] [INF] [Developer] Information level message - should show if level is Information or lower
[17:48:08] [WAR] [Developer] Warning level message - should always show
[ServiceExtensions] Service IGridPresenter not found in scope for Grid, falling back to GameStrapper
  ERROR: [ServiceExtensions] Error resolving IGridPresenter for Grid: Service IGridPresenter is not registered in GameStrapper for node Grid
[ServiceExtensions] Optional service IGridPresenter not available for Grid: Failed to resolve service IGridPresenter for node Grid. Primary error: Service IGridPresenter is not registered in GameStrapper for node Grid. Fallback error: Service IGridPresenter is not registered in GameStrapper for node Grid
  ERROR: [GridView] Failed to resolve IGridPresenter from service locator
[17:48:08] [INF] [System] Setting up MVP architecture...
[17:48:08] [INF] [System] Views found successfully - Grid: "Grid", Actor: "Actors" (health consolidated into ActorView)
[17:48:08] [INF] [System] GameManager will subscribe to domain events via UI Event Bus - modern architecture replaces static router
[17:48:08] [INF] [System] Presenters created and connected - GridPresenter and ActorPresenter (with consolidated health functionality) initialized with cross-presenter coordination
[17:48:08] [INF] [System] Creating {Width}x{Height} grid with lines
[17:48:08] [INF] [System] ActorPresenter initialized, setting up initial actors
[17:48:08] [INF] [System] Successfully created test player {Name} (Player) at position Actor_3e93a7ec with (15, 10) health
[17:48:08] [INF] [Gameplay] Actor_3e93a7ec created at (15,10)
[17:48:08] [INF] [System] Creating dummy combat target at position (5, 5) with 50 health
[17:48:08] [INF] [System] Successfully created dummy target {Name} (Combat Dummy) at position Actor_ad35cc64 with (5, 5) health
[17:48:08] [INF] [Gameplay] Actor_ad35cc64 created at (5,5)
[17:48:08] [INF] [System] MVP architecture setup completed - application ready for interaction
[17:48:09] [INF] [Gameplay] Actor_3e93a7ec moved from (15,10) to (20,9)
[17:48:17] [INF] [Gameplay] Actor_3e93a7ec moved from (20,9) to (4,5)
[17:48:18] [INF] [Combat] {TargetId} health: {HPBefore} → {HPAfter} ({ActualDamage} damage taken, {HPAfter}/{MaxHP} remaining)
[17:48:18] [INF] [System] [GameManager] Received damage notification for actor Actor_ad35cc64: {OldHealth} → {NewHealth}
[17:48:18] [INF] [System] [GameManager] Updating health bar via ActorPresenter (consolidated from HealthPresenter)
[17:48:18] [INF] [Combat] Attack completed: {AttackerId} next turn in +{ActionCost} TU
[17:48:18] [INF] [Combat] {AttackerId} [{Action}] → {TargetId} at (Sword Slash,Actor_ad35cc64): Actor_3e93a7ec damage ({Outcome})
[17:48:18] [INF] [System] Handling health change for Actor_ad35cc64 from Health(50/50) to Health(35/50)
