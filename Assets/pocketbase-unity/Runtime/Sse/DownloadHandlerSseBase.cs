using System.Text;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    public abstract class DownloadHandlerSseBase : DownloadHandlerScript
    {
        private readonly StringBuilder _currentLine = new();

        protected DownloadHandlerSseBase(byte[] buffer) : base(buffer)
        {
        }

        protected abstract void OnNewLineReceived(string line);

        protected override bool ReceiveData(byte[] newData, int dataLength)
        {
            for (int i = 0; i < dataLength; i++)
            {
                char c = (char)newData[i];

                if (c == '\n')
                {
                    OnNewLineReceived(_currentLine.ToString());
                    _currentLine.Clear();
                }
                else
                    _currentLine.Append(c);
            }

            return true;
        }

        protected override void CompleteContent()
        {
            if (_currentLine.Length > 0)
                OnNewLineReceived(_currentLine.ToString());
        }
    }
}