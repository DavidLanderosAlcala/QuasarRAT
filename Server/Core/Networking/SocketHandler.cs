using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace xServer.Core.Networking
{
    public interface SocketHandler
    {
        EndPoint RemoteEndPoint { get; }
        void SetKeepAliveEx(uint keepAliveInterval, uint keepAliveTime);
        IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
        int EndReceive(IAsyncResult asyncResult);
        int Send(byte[] buffer);
        void Close();
    }
}
