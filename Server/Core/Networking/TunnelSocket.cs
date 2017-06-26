using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace xServer.Core.Networking
{
    public class TunnelSocket : SocketHandler
    {
        EndPoint _remoteEndPoint;
        NetworkTunnel _parentTunnel;

        EndPoint SocketHandler.RemoteEndPoint
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
            throw new NotImplementedException();
        }

        void SocketHandler.Close()
        {
            throw new NotImplementedException();
        }

        int SocketHandler.EndReceive(IAsyncResult asyncResult)
        {
            throw new NotImplementedException();
        }

        int SocketHandler.Send(byte[] buffer)
        {
            throw new NotImplementedException();
        }

        void SocketHandler.SetKeepAliveEx(uint keepAliveInterval, uint keepAliveTime)
        {
            /* Dummy method */
        }
    }
}
