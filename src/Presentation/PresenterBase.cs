using System;

namespace Darklands.Application.Presentation
{
    /// <summary>
    /// Base class for all Presenters following the MVP pattern.
    /// Provides a strongly-typed reference to the View interface and standard lifecycle hooks.
    /// Derived from BlockLife reference implementation with Darklands-specific adaptations.
    /// </summary>
    /// <typeparam name="TViewInterface">The interface of the view this presenter manages.</typeparam>
    public abstract class PresenterBase<TViewInterface> : IDisposable where TViewInterface : class
    {
        /// <summary>
        /// The view interface this presenter controls.
        /// Protected to allow derived presenters direct access to their specific view.
        /// </summary>
        protected TViewInterface View { get; }

        /// <summary>
        /// Creates a new presenter with the specified view interface.
        /// </summary>
        /// <param name="view">The view implementation this presenter will control</param>
        /// <exception cref="ArgumentNullException">Thrown when view is null</exception>
        protected PresenterBase(TViewInterface view)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>
        /// Called after the presenter is created and wired to the view.
        /// Use this for:
        /// - Subscribing to domain events and notifications
        /// - Setting up initial view state
        /// - Registering for MediatR notifications
        /// - Initializing view with current data
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Called when the presenter is being disposed or the view is exiting.
        /// Use this for:
        /// - Unsubscribing from events and notifications  
        /// - Releasing resources and preventing memory leaks
        /// - Cleaning up any background operations
        /// </summary>
        public virtual void Dispose() { }
    }
}
