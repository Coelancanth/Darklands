using System;

namespace Darklands.Presentation
{
    /// <summary>
    /// Base class for all Presenters following the MVP pattern.
    /// Provides a strongly-typed reference to the View interface and standard lifecycle hooks.
    /// Supports late-binding of views to enable DI container registration of presenters.
    /// Derived from BlockLife reference implementation with Darklands-specific adaptations.
    /// </summary>
    /// <typeparam name="TViewInterface">The interface of the view this presenter manages.</typeparam>
    public abstract class PresenterBase<TViewInterface> : IDisposable where TViewInterface : class
    {
        private TViewInterface? _view;

        /// <summary>
        /// The view interface this presenter controls.
        /// Protected to allow derived presenters direct access to their specific view.
        /// Will be null until AttachView is called.
        /// </summary>
        protected TViewInterface View => _view ?? throw new InvalidOperationException($"View has not been attached to {GetType().Name}. Call AttachView first.");

        /// <summary>
        /// Creates a new presenter without a view.
        /// The view will be attached later via AttachView.
        /// </summary>
        protected PresenterBase()
        {
        }

        /// <summary>
        /// Creates a new presenter with the specified view interface.
        /// Legacy constructor for backward compatibility.
        /// </summary>
        /// <param name="view">The view implementation this presenter will control</param>
        /// <exception cref="ArgumentNullException">Thrown when view is null</exception>
        protected PresenterBase(TViewInterface view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>
        /// Attaches a view to this presenter after construction.
        /// Used when presenters are created by DI container and views are created by Godot.
        /// </summary>
        /// <param name="view">The view to attach</param>
        public virtual void AttachView(TViewInterface view)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            if (_view != null)
                throw new InvalidOperationException($"View already attached to {GetType().Name}");

            _view = view;
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
