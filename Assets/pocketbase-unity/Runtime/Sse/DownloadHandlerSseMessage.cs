using System;
using System.Linq;
using UnityEngine;

namespace PocketBaseSdk
{
    public class DownloadHandlerSseMessage : DownloadHandlerSseBase
    {
        SseMessage _sseMessage = new();
        private readonly Action<SseMessage> _onMessage;

        public DownloadHandlerSseMessage(Action<SseMessage> onMessage) : base(new byte[1024])
        {
            _onMessage = onMessage;
        }

        protected override void OnNewLineReceived(string line)
        {
            // Empty line means end of the message
            if (string.IsNullOrEmpty(line))
            {
                _onMessage?.Invoke(_sseMessage);
                _sseMessage = new();
                return;
            }

            // Comment, ignore
            if (line.StartsWith(":"))
                return;

            // Split each line into field and value at the first occurrence of ':'
            string[] parts = line.Split(':', 2);
            string field = parts.ElementAtOrDefault(0)?.Trim();
            string value = parts.ElementAtOrDefault(1)?.Trim();

            switch (field)
            {
                case "id":
                    _sseMessage.Id = value;
                    break;

                case "event":
                    _sseMessage.Event = value;
                    break;

                case "retry":
                    int.TryParse(value, out _sseMessage.Retry);
                    break;

                case "data":
                    _sseMessage.Data = value;
                    break;
            }
        }
    }
}