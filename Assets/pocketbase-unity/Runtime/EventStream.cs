using System;
using System.Collections.Generic;

namespace PocketBaseSdk
{
    public class EventStream<T>
    {
        private readonly List<T> _history = new();
        private event Action<T> OnEvent;

        public void Invoke(T eventData)
        {
            _history.Add(eventData);
            OnEvent?.Invoke(eventData);
        }

        public void Subscribe(Action<T> handler, bool replayHistory = true)
        {
            if (replayHistory)
            {
                foreach (var historicalEvent in _history)
                {
                    handler(historicalEvent);
                }
            }

            OnEvent += handler;
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        public void ClearSubscribers()
        {
            OnEvent = null;
        }

        public void Clear()
        {
            ClearHistory();
            ClearSubscribers();
        }
    }
}