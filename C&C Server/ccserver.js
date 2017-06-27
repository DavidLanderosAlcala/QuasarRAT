
var net = require("net");

/*
 * Module TunnelPacket
 * It serializes and deserializes TunnelPackets
 *
 * The tunnelPackets are built with the following format:
 *     [ 4 bytes for the Origin IP Address]
 *     [ 2 bytes for the Origin Port (big-endian)]
 *     [ 1 byte for the packet type ]
 *     [ 2 bytes for the pyload size (big-endian)]
 *     [ payload (variable length) ]
 */
var TunnelPacket = (function(){

	function parse(buffer)
	{

	}

	function serialize(packetObj)
	{

	}

})();

/* 
 * Class: Tunnel
 * This class forwards incoming connections to the adminSocket,
 * wrapping incomming packets with the TunnelPacket Format.
 */
class Tunnel {

    constructor() {
        this.clients = [];
    }

	init(adminSocket, clientsPort) {

        this.adminSocket = adminSocket;
        this.clientsPort = clientsPort;

        net.createServer(function(clientSocket){

        	clientSocket.on("data", function(data){
        		onDataFromClient(clientSocket, data);
        	});

        	clientSocket.on("close", function(){
        		onClientDisconnected(clientSocket);
        	});

        	clientSocket.on("error", function(){});
        	onClientConnected(clientSocket);

        }).listen(clientsPort);

        adminPort.on("data", function(data){
        	onDataFromAdmin(data)
        });
	}

	onClientConnected(socket) {

	}

	onClientDisconnected(socket) {

	}

	onDataFromClient(socket, data) {

	}

	onDataFromAdmin(data) {

	}
}

/*
 * Module: CCServer
 * This module creates 2 listeners to receive connections from
 *   1- Administrators
 *   2- Clients
 * Usage:
 *   var adminPort   = 12345;
 *   var clientsPort = 12346;
 *   CCServer.init(adminPort, clientsPort);
 */
var CCServer = (function() {

    function init(adminPort, clientsPort, adminPassword) {
        net.createServer(function(socket) {
        	StartHandShaking(socket, function(authorized) {
                if(authorized) {
                    Tunnel tun = new Tunnel();
                    tun.init(socket, clientsPort);
                }
                else {
                	socket.end();
                }
        	});
        }).listen(adminPort);
    }

    function StartHandShaking(socket, callback) {

    }

    /* Exported functions
     */
   return { init : init }
	
})();