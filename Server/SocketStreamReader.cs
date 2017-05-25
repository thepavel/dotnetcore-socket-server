﻿using System;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class SocketStreamReader
    {
        public static int ValueSize => 9;
        public static byte[] NewLineSequence => Encoding.ASCII.GetBytes(Environment.NewLine);
        public static int NewLineSize => NewLineSequence.Length;
        public static int ChunkSize => ValueSize + NewLineSize;

        private static readonly string _terminateSequence = "terminate" + Environment.NewLine;

        private readonly ISocketConnectionProxy _socketConnection;

        public SocketStreamReader(Socket socket) : this(new SocketConnectionProxy(socket))
        {
        }
        public SocketStreamReader(ISocketConnectionProxy socketConnection)
        {
            _socketConnection = socketConnection;
        }

        public void Read(Action<int> valueReadCallback, Action terminationCallback = null)
        {
            var buffer = new byte[ChunkSize];
            int bytesRead;
            while ((bytesRead = TryReadChunk(buffer)) == ChunkSize)
            {
                if (!TryConvertToInt32(buffer, out int value))
                {
                    if (IsTerminateSequence(buffer))
                    {
                        terminationCallback?.Invoke();
                        break;
                    }
                    break;
                }
                valueReadCallback?.Invoke(value);
            }
        }

        private int TryReadChunk(byte[] buffer)
        {
            int bytesRead;
            int bufferOffset = 0;
            while (bufferOffset < buffer.Length)
            {
                bytesRead = _socketConnection.Receive(buffer, bufferOffset, buffer.Length - bufferOffset);
                if (bytesRead == 0)
                {
                    break;
                }
                bufferOffset += bytesRead;
            }
            return bufferOffset;
        }

        private bool TryConvertToInt32(byte[] buffer, out int value)
        {
            value = 0;

            for (var i = 0; i < NewLineSize; i++)
            {
                if (buffer[ValueSize + i] != NewLineSequence[i])
                {
                    return false;
                }
            }

            byte b;
            int place;
            for (var i = 0; i < ValueSize; i++)
            {
                b = buffer[i];
                if (b < 48 || b > 57)
                {
                    return false;
                }
                place = (int)Math.Pow(10, ValueSize - i - 1);
                value += ((b - 48) * place);
            }
            return true;
        }

        private bool IsTerminateSequence(byte[] buffer)
        {
            if (buffer[0] == 84 || buffer[0] == 116)
            {
                return Encoding.ASCII.GetString(buffer)
                    .Equals(_terminateSequence, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}
