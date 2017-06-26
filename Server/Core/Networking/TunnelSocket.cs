﻿using System;
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

        IAsyncResult SocketHandler.BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            _readBuffer = buffer;
            _readCallback = callback;
            return null;
        }

        void SocketHandler.Close()
        {
            throw new NotImplementedException();
        }

        int SocketHandler.EndReceive(IAsyncResult asyncResult)
        {
            return ((TunnelSocketIAsyncResult)asyncResult).PacketLength;
        }

        int SocketHandler.Send(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        void SocketHandler.SetKeepAliveEx(uint keepAliveInterval, uint keepAliveTime)
        {
            /* Dummy method */
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
