using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using xServer.Core.Extensions;

namespace xServer.Core.Networking
{
    class ClassicSocket : SocketHandler
    {
        Socket _socket;

        public ClassicSocket(Socket s)
        {
            _socket = s;
        }

        EndPoint SocketHandler.RemoteEndPoint
        {
            get { return _socket.RemoteEndPoint; }
        }

        IAsyncResult SocketHandler.BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            return _socket.BeginReceive(buffer, offset, size, socketFlags, callback, state);
        }

        void SocketHandler.Close()
        {
            _socket.Close();
        }

        int SocketHandler.EndReceive(IAsyncResult asyncResult)
        {
            return _socket.EndReceive(asyncResult);
        }

        int SocketHandler.Send(byte[] buffer)
        {
            return _socket.Send(buffer);
        }

        void SocketHandler.SetKeepAliveEx(uint keepAliveInterval, uint keepAliveTime)
        {
            _socket.SetKeepAliveEx(keepAliveInterval, keepAliveTime);
        }
    }
}
