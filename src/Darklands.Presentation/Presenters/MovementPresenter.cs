using System;
using System.Threading.Tasks;
using Darklands.Application.Events;
using Darklands.Application.Common;
using Darklands.Domain.Grid;
using Darklands.Presentation.Views;
using MediatR;

namespace Darklands.Presentation.Presenters
{
    /// <summary>
    /// Presenter for coordinating movement animations between domain events and view layer.
    /// Bridges ActorMovedEvent notifications to ActorView animation capabilities.
    /// Follows MVP pattern with event-driven coordination and UIEventBus integration.
    /// Part of TD_060: Movement Animation Foundation implementation.
    /// </summary>
    public sealed class MovementPresenter : PresenterBase<IActorView>
    {
        private readonly IUIEventBus _eventBus;
        private readonly ICategoryLogger _logger;

        /// <summary>
        /// Creates a new MovementPresenter with the specified dependencies.
        /// The view will be attached later via AttachView method.
        /// </summary>
        /// <param name="eventBus">UI event bus for subscribing to domain events and publishing completion notifications</param>
        /// <param name="logger">Logger for movement coordination messages</param>
        public MovementPresenter(
            IUIEventBus eventBus,
            ICategoryLogger logger)
            : base()
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes the presenter and subscribes to domain events.
        /// Called after the presenter is created and view is attached.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
            SubscribeToEvents();
            _logger.Log(LogLevel.Information, LogCategory.System, "MovementPresenter initialized and subscribed to events");
        }

        /// <summary>
        /// Disposes the presenter and unsubscribes from events.
        /// Called when the presenter is being disposed or the view is exiting.
        /// </summary>
        public override void Dispose()
        {
            _eventBus.UnsubscribeAll(this);
            _logger.Log(LogLevel.Information, LogCategory.System, "MovementPresenter disposed and unsubscribed from events");
            base.Dispose();
        }

        /// <summary>
        /// Subscribes to relevant domain events for movement coordination.
        /// Currently handles ActorMovedEvent for animation coordination.
        /// </summary>
        private void SubscribeToEvents()
        {
            try
            {
                // Subscribe to actor movement events to trigger animations
                _eventBus.Subscribe<ActorMovedEvent>(this, OnActorMoved);

                _logger.Log(LogLevel.Debug, LogCategory.System, "MovementPresenter subscribed to ActorMovedEvent");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.System, "Failed to subscribe to movement events: {Error}", ex.Message);
            }
        }

        /// <summary>
        /// Handles ActorMovedEvent by coordinating view animation and publishing completion notification.
        /// Converts domain event data to view animation parameters and manages the animation lifecycle.
        /// </summary>
        /// <param name="actorMovedEvent">Domain event containing movement details</param>
        private async void OnActorMoved(ActorMovedEvent actorMovedEvent)
        {
            try
            {
                _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                    "[MovementPresenter.OnActorMoved] RECEIVED EVENT for actor {ActorId} with {PathLength} steps, speed={Speed}",
                    actorMovedEvent.ActorId, actorMovedEvent.Path?.Count ?? 0, actorMovedEvent.Speed);

                // Coordinate view animation using the new AnimateMovementAsync method
                if (actorMovedEvent.Path != null && actorMovedEvent.Path.Count > 0)
                {
                    _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                        "[MovementPresenter.OnActorMoved] Calling View.AnimateMovementAsync for {PathLength} positions",
                        actorMovedEvent.Path.Count);

                    // Log the path details
                    for (int i = 0; i < actorMovedEvent.Path.Count; i++)
                    {
                        var pos = actorMovedEvent.Path[i];
                        _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                            "[MovementPresenter.OnActorMoved] Path[{Index}]: ({X}, {Y})",
                            i, pos.X, pos.Y);
                    }

                    await View.AnimateMovementAsync(
                        actorMovedEvent.ActorId,
                        actorMovedEvent.Path,
                        actorMovedEvent.Speed);

                    // Publish completion notification via UIEventBus
                    await _eventBus.PublishAsync(new MovementAnimationCompletedEvent(
                        actorMovedEvent.ActorId,
                        actorMovedEvent.Path[^1])); // Last position in path

                    _logger.Log(LogLevel.Information, LogCategory.Gameplay,
                        "[MovementPresenter.OnActorMoved] Animation completed and event published for actor {ActorId}",
                        actorMovedEvent.ActorId);
                }
                else
                {
                    _logger.Log(LogLevel.Warning, LogCategory.Gameplay,
                        "[MovementPresenter.OnActorMoved] ActorMovedEvent received with EMPTY path for actor {ActorId}",
                        actorMovedEvent.ActorId);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, LogCategory.Gameplay,
                    "[MovementPresenter.OnActorMoved] ERROR handling ActorMovedEvent for actor {ActorId}: {Error}",
                    actorMovedEvent.ActorId, ex.Message);
            }
        }
    }

    /// <summary>
    /// Domain event indicating an actor has moved along a path.
    /// Published by movement commands when actors complete movement actions.
    /// Contains path information for animation coordination.
    /// </summary>
    public record ActorMovedEvent(
        ActorId ActorId,
        System.Collections.Generic.List<Position> Path,
        float Speed = 3.0f) : INotification;

    /// <summary>
    /// UI event indicating movement animation has completed.
    /// Published by MovementPresenter when view animation finishes.
    /// Used to coordinate next turn or UI state updates.
    /// </summary>
    public record MovementAnimationCompletedEvent(
        ActorId ActorId,
        Position FinalPosition) : INotification;
}
