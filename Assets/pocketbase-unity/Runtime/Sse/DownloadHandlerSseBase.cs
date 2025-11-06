/*
 * Thanks to https://github.com/prodigga for the base code.
 * Available here https://gist.github.com/prodigga/861d72075f9f8abde5fc7b9744a1f4eb.
 */

using System.Text;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    public abstract class DownloadHandlerSseBase : DownloadHandlerScript
    {
        private readonly StringBuilder _currentLine = new();
        private readonly Decoder _utf8Decoder = Encoding.UTF8.GetDecoder();

        protected DownloadHandlerSseBase(byte[] buffer) : base(buffer)
        {
        }

        protected abstract void OnNewLineReceived(string line);

        protected override bool ReceiveData(byte[] newData, int dataLength)
        {
            // Calculate max chars needed for this chunk (UTF-8: 1 byte = 1 char in worst case)
            int maxCharCount = Encoding.UTF8.GetMaxCharCount(dataLength);
            char[] charBuffer = new char[maxCharCount];
            
            // Use stateful decoder to handle multi-byte UTF-8 sequences split across chunks
            int charCount = _utf8Decoder.GetChars(newData, 0, dataLength, charBuffer, 0);
            
            for (int i = 0; i < charCount; i++)
            {
                char c = charBuffer[i];
                
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