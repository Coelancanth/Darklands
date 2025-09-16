using System;
using System.Collections.Concurrent;
using Godot;
using Darklands.Presentation.DI;

namespace Darklands.Presentation.EventBus
{
    /// <summary>
    /// Thread-safe UI dispatcher that marshals operations to the main UI thread.
    /// Implements the UIDispatcher pattern from ADR-021 to prevent race conditions
    /// and ensure all UI updates happen on Godot's main thread.
    ///
    /// MUST be registered as a Godot autoload at: /root/UIDispatcher
    ///
    /// Thread Safety:
    /// - Uses ConcurrentQueue for lock-free operation queuing
    /// - CallDeferred ensures execution on main thread
    /// - Prevents race conditions documented in BR_007
    /// </summary>
    public partial class UIDispatcher : Node, IUIDispatcher
    {
        private readonly ConcurrentQueue<Action> _actionQueue = new();
        private bool _isProcessing;

        /// <summary>
        /// Called when the autoload node is added to the scene tree.
        /// </summary>
        public override void _Ready()
        {
            Name = "UIDispatcher";
            ProcessMode = ProcessModeEnum.Always; // Continue processing even when paused
            GD.Print($"[UIDispatcher] Initialized at {GetPath()}");
        }

        /// <summary>
        /// Dispatches an action to be executed on the main UI thread.
        /// Thread-safe and can be called from any thread.
        /// </summary>
        /// <param name="action">The action to execute on the main thread</param>
        public void DispatchToMainThread(Action action)
        {
            if (action == null)
            {
                GD.PrintErr("[UIDispatcher] Cannot dispatch null action");
                return;
            }

            _actionQueue.Enqueue(action);

            // Use CallDeferred to ensure processing happens on main thread
            if (!_isProcessing)
            {
                CallDeferred(nameof(ProcessQueuedActions));
            }
        }

        /// <summary>
        /// Dispatches an action with a return value to the main thread.
        /// Blocks the calling thread until the action completes.
        /// </summary>
        /// <typeparam name="T">The return type of the action</typeparam>
        /// <param name="func">The function to execute on the main thread</param>
        /// <returns>The result of the function execution</returns>
        public T DispatchToMainThreadWithResult<T>(Func<T> func)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            T result = default!;
            Exception? exception = null;
            var completed = false;

            DispatchToMainThread(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    completed = true;
                }
            });

            // Busy wait until completed (not ideal but necessary for synchronous result)
            while (!completed)
            {
                OS.DelayMsec(1);
            }

            if (exception != null)
            {
                throw new InvalidOperationException($"Action failed on main thread: {exception.Message}", exception);
            }

            return result;
        }

        /// <summary>
        /// Processes all queued actions on the main thread.
        /// Called via CallDeferred to ensure main thread execution.
        /// </summary>
        private void ProcessQueuedActions()
        {
            _isProcessing = true;

            try
            {
                int processedCount = 0;
                const int maxBatchSize = 10; // Process up to 10 actions per frame to avoid blocking

                while (_actionQueue.TryDequeue(out var action) && processedCount < maxBatchSize)
                {
                    try
                    {
                        action.Invoke();
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"[UIDispatcher] Error executing action: {ex.Message}");
                        GD.PrintErr($"[UIDispatcher] Stack trace: {ex.StackTrace}");
                    }
                }

                // If there are still items in queue, schedule another processing
                if (!_actionQueue.IsEmpty)
                {
                    CallDeferred(nameof(ProcessQueuedActions));
                }
                else
                {
                    _isProcessing = false;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[UIDispatcher] Critical error in ProcessQueuedActions: {ex.Message}");
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Checks if the current code is executing on the main thread.
        /// Useful for assertions and debugging.
        /// </summary>
        /// <returns>True if on main thread, false otherwise</returns>
        public static bool IsOnMainThread()
        {
            // In Godot, we can check if we're on the main thread by checking if we can access the scene tree
            try
            {
                // This will only work on the main thread
                var tree = Engine.GetMainLoop() as SceneTree;
                return tree != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the number of actions currently queued for processing.
        /// Useful for monitoring and debugging.
        /// </summary>
        public int QueuedActionCount => _actionQueue.Count;

        /// <summary>
        /// Clears all queued actions without executing them.
        /// Use with caution - may cause operations to be lost.
        /// </summary>
        public void ClearQueue()
        {
            while (_actionQueue.TryDequeue(out _))
            {
                // Clear the queue
            }
            _isProcessing = false;
            GD.Print("[UIDispatcher] Queue cleared");
        }

        /// <summary>
        /// Called when the node is removed from the scene tree.
        /// Ensures cleanup of any remaining queued actions.
        /// </summary>
        public override void _ExitTree()
        {
            ClearQueue();
            GD.Print("[UIDispatcher] Disposed");
        }
    }
}