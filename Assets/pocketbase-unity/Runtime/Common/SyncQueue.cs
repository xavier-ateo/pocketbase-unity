using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace PocketBaseSdk
{
    /// <summary>
    /// SyncQueue is a very rudimentary queue of async operations that will be processed sequential/synchronous.
    /// </summary>
    public class SyncQueue
    {
        public delegate Task AsyncOperation();

        private readonly List<AsyncOperation> _operations = new();
        private readonly Action _onComplete;

        public SyncQueue(Action onComplete = null)
        {
            _onComplete = onComplete;
        }

        /// <summary>
        /// Enqueue appends an async operation to the queue and executes it if it is the only one.
        /// </summary>
        public void Enqueue(AsyncOperation operation)
        {
            _operations.Add(operation);

            if (_operations.Count == 1)
            {
                // start processing
                Dequeue();
            }
        }

        /// <summary>
        /// Dequeue starts the queue processing.
        /// Each processed operation is removed from the queue once it completes.
        /// </summary>
        private async void Dequeue()
        {
            while (_operations.Any())
            {
                try
                {
                    await _operations.First()();
                    _operations.RemoveAt(0);
                }
                catch (Exception e)
                {
                    // Handle any exceptions from the async operation
                    _operations.RemoveAt(0);
                    Debug.LogException(e);
                }
            }

            // All operations completed, call the completion callback if provided
            if (_onComplete != null)
            {
                _onComplete();
            }
        }
    }
}