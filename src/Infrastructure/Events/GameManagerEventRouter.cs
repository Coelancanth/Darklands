using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Darklands.Core.Domain.Combat;
using System;
using Serilog;

namespace Darklands.Core.Infrastructure.Events;

/// <summary>
/// Static event router that bridges MediatR domain events to GameManager.
/// This approach bypasses DI lifecycle issues by using static registration.
/// </summary>
public sealed class GameManagerEventRouter :
    INotificationHandler<ActorDiedEvent>,
    INotificationHandler<ActorDamagedEvent>
{
    private readonly ILogger _logger;
    private static Action<ActorDiedEvent>? _staticOnActorDied;
    private static Action<ActorDamagedEvent>? _staticOnActorDamaged;

    public GameManagerEventRouter(ILogger logger)
    {
        _logger = logger;
        _logger.Debug("🌉 [GameManagerEventRouter] Static event router created");
    }

    /// <summary>
    /// Registers GameManager's event handlers statically.
    /// Called by GameManager during initialization.
    /// </summary>
    public static void RegisterHandlers(
        Action<ActorDiedEvent> onActorDied,
        Action<ActorDamagedEvent> onActorDamaged,
        ILogger logger)
    {
        logger.Information("🔗 [GameManagerEventRouter] Registering GameManager handlers STATICALLY");
        _staticOnActorDied = onActorDied;
        _staticOnActorDamaged = onActorDamaged;
    }

    /// <summary>
    /// Handles ActorDiedEvent by forwarding to GameManager via static handler.
    /// </summary>
    public Task Handle(ActorDiedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Debug("💀 [GameManagerEventRouter] Routing ActorDiedEvent for {ActorId}", notification.ActorId);

        if (_staticOnActorDied != null)
        {
            _staticOnActorDied.Invoke(notification);
        }
        else
        {
            _logger.Error("❌ [GameManagerEventRouter] No static death handler registered!");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles ActorDamagedEvent by forwarding to GameManager via static handler.
    /// </summary>
    public Task Handle(ActorDamagedEvent notification, CancellationToken cancellationToken)
    {
        _logger.Debug("💔 [GameManagerEventRouter] Routing ActorDamagedEvent for {ActorId}", notification.ActorId);

        if (_staticOnActorDamaged != null)
        {
            _staticOnActorDamaged.Invoke(notification);
        }
        else
        {
            _logger.Error("❌ [GameManagerEventRouter] No static damage handler registered!");
        }

        return Task.CompletedTask;
    }
}
