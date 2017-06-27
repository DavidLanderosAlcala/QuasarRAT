using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace xServer.Core.Networking
{
    public class TunnelSocket : SocketHandler
    {
        EndPoint _remoteEndPoint;
        NetworkTunnel _parentTunnel;
        byte[] _readBuffer;
        AsyncCallback _readCallback;

        public EndPoint RemoteEndPoint
        {
            get { return _remoteEndPoint; }
        }

        public TunnelSocket(NetworkTunnel parent, EndPoint remoteEndPoint)
        {
            _parentTunnel = parent;
            _remoteEndPoint = remoteEndPoint;
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            _readBuffer = buffer;
            _readCallback = callback;
            return null;
        }

        public void Close()
        {
            _parentTunnel.CloseSocket(this);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return ((TunnelSocketIAsyncResult)asyncResult).PacketLength;
        }

        public int Send(byte[] buffer)
        {
            _parentTunnel.Send(this, buffer);
            return buffer.Length;
        }

        public void SetKeepAliveEx(uint keepAliveInterval, uint keepAliveTime)
        {
            /* To be implemented
             * maybe we can send TCP_PSH packets (0 length) through the tunnel
             */
        }

        public class TunnelSocketIAsyncResult : IAsyncResult
        {
            private int _packetLength;
            public int PacketLength { get { return _packetLength; } }

            public TunnelSocketIAsyncResult(int packetLength)
            {
                _packetLength = packetLength;
            }

            object IAsyncResult.AsyncState
            {
                get { throw new NotImplementedException(); }
            }

            WaitHandle IAsyncResult.AsyncWaitHandle
            {
                get { throw new NotImplementedException(); }
            }

            bool IAsyncResult.CompletedSynchronously
            {
                get { throw new NotImplementedException(); }
            }

            bool IAsyncResult.IsCompleted
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
