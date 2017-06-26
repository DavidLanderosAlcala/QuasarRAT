using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using xServer.Core.Extensions;

namespace xServer.Core.Networking
{
    public interface SocketHandler { }
    public class ClassicSocket : SocketHandler { }
    public class TunnelSocket : SocketHandler { }

    public class NetworkTunnel
    {
        /// <summary>
        /// This is the main socket and should be connected to the c&c server
        /// </summary>
        Socket _handle;
        byte[] _readBuffer;

        /// <summary>
        /// The keep-alive time in ms.
        /// </summary>
        public uint KEEP_ALIVE_TIME { get { return 25000; } } // 25s

        /// <summary>
        /// The keep-alive interval in ms.
        /// </summary>
        public uint KEEP_ALIVE_INTERVAL { get { return 25000; } } // 25s

        /// <summary>
        /// Occurs when a new socket is connected
        /// </summary>
        public event SocketConnectedEventHandler SocketConnected;

        /// <summary>
        /// Represents the method that will handle new TunnelSocket connection event
        /// </summary>
        /// <param name="s">The TunnelSocket wich represents the new connection.</param>
        public delegate void SocketConnectedEventHandler(TunnelSocket s);

        /// <summary>
        /// NetworkTunnel fail event and event-handler
        /// </summary>
        public event NetworkTunnelFailedEventHandler NetworkTunnelFailed;
        public delegate void NetworkTunnelFailedEventHandler(Exception ex);

        public NetworkTunnel()
        {
            _readBuffer = new byte[1024];
        }

        /// <summary>
        /// Fires an event that informs subscribers that a new socket has been connected.
        /// </summary>
        /// <param name="s">The TunnelSocket wich represents the new connection.</param>
        public void OnSocketConnected(TunnelSocket s)
        {
            var handler = SocketConnected;
            if(handler != null)
            {
                handler(s);
            }
        }

        /// <summary>
        /// Fires an event that informs subscribers that the network tunnel has failed.
        /// </summary>
        public void OnNetowkTunnelFailed(Exception ex)
        {
            var handler = NetworkTunnelFailed;
            if (handler != null)
            {
                handler(ex);
            }
        }

        /// <summary>
        /// Initializes the network tunnel connecting the main socket to the tunnel server
        /// </summary>
        public void Connect(IPAddress ip, ushort port)
        {
            try
            {
                Disconnect();

                _handle = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _handle.SetKeepAliveEx(KEEP_ALIVE_INTERVAL, KEEP_ALIVE_TIME);
                _handle.Connect(ip, port);

                if (_handle.Connected)
                {
                    _handle.BeginReceive(_readBuffer, 0, _readBuffer.Length, SocketFlags.None, AsyncReceive, null);
                }
            }
            catch (Exception ex)
            {
                OnNetowkTunnelFailed(ex);
            }
        }


        /// <summary>
        /// Reads incomming data and determines the type of each packet:
        /// TCP_SYN, TCP_PSH or TCP_FIN
        /// also performs the required actions to each type of packet
        /// </summary>
        /// <param name="result"></param>
        public void AsyncReceive(IAsyncResult result)
        {
            /* To be implemented */
        }

        public void Disconnect()
        {
            try
            {
                _handle.Close();
            }
            catch(Exception)
            {

            }
        }

    }
}
